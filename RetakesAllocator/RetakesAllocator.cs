using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;
using SQLitePCL;

namespace RetakesAllocator;

[MinimumApiVersion(142)]
public class RetakesAllocator : BasePlugin
{
    public override string ModuleName => "Retakes Allocator Plugin";
    public override string ModuleVersion => "1.0.0-beta";

    private readonly IList<CCSPlayerController> _tPlayers = new List<CCSPlayerController>();
    private readonly IList<CCSPlayerController> _ctPlayers = new List<CCSPlayerController>();

    private RoundType? _nextRoundType;
    private RoundType? _currentRoundType;

    #region Setup

    public override void Load(bool hotReload)
    {
        Log.Write("Loaded");
        ResetState();
        Batteries.Init();
        
        RegisterListener<Listeners.OnMapStart>(mapName =>
        {
            ResetState();
        });

        if (Configs.GetConfigData().MigrateOnStartup)
        {
            Queries.Migrate();
        }
        

        if (hotReload)
        {
            HandleHotReload();
        }
    }

    private void ResetState()
    {
        Configs.Load(ModuleDirectory);
        _tPlayers.Clear();
        _ctPlayers.Clear();
        _nextRoundType = null;
        _currentRoundType = null;
    }

    private void HandleHotReload()
    {
        Server.ExecuteCommand($"map {Server.MapName}");
    }

    public override void Unload(bool hotReload)
    {
        Log.Write($"Unloaded");
        ResetState();
        Queries.Disconnect();
    }

    #endregion

    #region Commands

    [ConsoleCommand("css_gun")]
    [CommandHelper(minArgs: 1, usage: "<gun> [T|CT]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnWeaponCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!Helpers.PlayerIsValid(player))
        {
            return;
        }

        var playerId = player?.AuthorizedSteamID?.SteamId64 ?? 0;
        var team = (CsTeam)player!.TeamNum;

        var result = OnWeaponCommandHelper.Handle(
            Helpers.CommandInfoToArgList(commandInfo),
            playerId,
            team,
            false,
            out var selectedWeapon
        );
        if (result is not null)
        {
            commandInfo.ReplyToCommand(result);
        }

        if (selectedWeapon is not null)
        {
            var selectedWeaponRoundType = WeaponHelpers.GetRoundTypeForWeapon(selectedWeapon.Value);
            if (selectedWeaponRoundType == RoundType.Pistol || selectedWeaponRoundType == _currentRoundType)
            {
                Helpers.RemoveWeapons(
                    player,
                    item => WeaponHelpers.GetRoundTypeForWeapon(item) == selectedWeaponRoundType
                );
                AllocateItemsForPlayer(player, new List<CsItem> { selectedWeapon.Value });
            }
        }
    }

    [ConsoleCommand("css_removegun")]
    [CommandHelper(minArgs: 1, usage: "<gun> [T|CT]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnRemoveWeaponCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!Helpers.PlayerIsValid(player))
        {
            return;
        }

        var playerId = player?.AuthorizedSteamID?.SteamId64 ?? 0;
        var team = (CsTeam)player!.TeamNum;

        var result = OnWeaponCommandHelper.Handle(
            Helpers.CommandInfoToArgList(commandInfo),
            playerId,
            team,
            true,
            out _
        );
        if (result is not null)
        {
            commandInfo.ReplyToCommand(result);
        }
    }

    [ConsoleCommand("css_nextround", "Sets the next round type.")]
    [CommandHelper(minArgs: 1, usage: "<P/H/F>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnNextRoundCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var roundTypeInput = commandInfo.GetArg(1).ToLower();
        var roundType = RoundTypeHelpers.ParseRoundType(roundTypeInput);
        if (roundType is null)
        {
            commandInfo.ReplyToCommand($"Invalid round type: {roundTypeInput}.");
        }
        else
        {
            _nextRoundType = roundType;
            commandInfo.ReplyToCommand($"Next round will be a {roundType} round.");
        }
    }

    [ConsoleCommand("css_reload_allocator_config", "Reloads the cs2-retakes-allocator config.")]
    [RequiresPermissions("@css/root")]
    public void OnReloadAllocatorConfigCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        Configs.Load(ModuleDirectory);
    }

    #endregion

    #region Events

    [GameEventHandler]
    public HookResult OnPostItemPurchase(EventItemPurchase @event, GameEventInfo info)
    {
        if (Helpers.IsWarmup())
        {
            return HookResult.Continue;
        }
        var item = Utils.ToEnum<CsItem>(@event.Weapon);
        var team = (CsTeam)@event.Userid.TeamNum;
        var playerId = Helpers.GetSteamId(@event.Userid);
        var weaponRoundType = WeaponHelpers.GetRoundTypeForWeapon(item);

        // Log.Write($"item {item} team {team} player {playerId}");
        // Log.Write($"curRound {_currentRoundType} weapon Round {weaponRoundType}");

        if (weaponRoundType is not null &&
            (weaponRoundType == _currentRoundType || weaponRoundType == RoundType.Pistol))
        {
            Queries.SetWeaponPreferenceForUser(
                playerId,
                team,
                weaponRoundType.Value,
                item
            );
        }
        else
        {
            var removedAnyWeapons = Helpers.RemoveWeapons(@event.Userid,
                i =>
                {
                    if (!WeaponHelpers.IsWeapon(i))
                    {
                        return i == item;
                    }

                    // Some weapons identify as other weapons, so we just remove them all
                    return WeaponHelpers.GetRoundTypeForWeapon(i) == weaponRoundType;
                });
            // Log.Write($"Removed {item}? {removedAnyWeapons}");
            if (removedAnyWeapons && _currentRoundType is not null && WeaponHelpers.IsWeapon(item))
            {
                var replacementItem = WeaponHelpers.GetWeaponForRoundType(_currentRoundType.Value, team,
                    Queries.GetUserSettings(playerId));
                // Log.Write($"Replacement item: {replacementItem}");
                if (replacementItem is not null)
                {
                    AllocateItemsForPlayer(@event.Userid, new List<CsItem>
                    {
                        replacementItem.Value
                    });
                }
            }
        }

        var playerPos = @event.Userid.PlayerPawn.Value!.AbsOrigin;

        AddTimer(0.01f, () =>
        {
            var pEntity = new CEntityIdentity(EntitySystem.FirstActiveEntity);
            for (; pEntity != null && pEntity.Handle != IntPtr.Zero; pEntity = pEntity.Next)
            {
                var p = new PointerTo<CEntityInstance>(pEntity.Handle).Value;
                var p2 = new CBasePlayerWeapon(p.Handle);
                if (!p.IsValid)
                {
                    continue;
                }

                if (!p.DesignerName.StartsWith("weapon") || playerPos == null || p2.AbsOrigin is null)
                {
                    continue;
                }
                
                Log.Write($"d: {p.DesignerName}. n: {p.Entity?.Name} wgid: {p.Entity?.WorldGroupId}");

                var distance = Helpers.GetVectorDistance(playerPos, p2.AbsOrigin);
                if (distance is > 0 and < 20)
                {
                    p.Remove();
                }
                
                // var found = false;
                // foreach (var player in _ctPlayers.Concat(_tPlayers))
                // {
                //     var w = Helpers.GetPlayerWeapon(player, (wep, _) =>
                //     {
                //         return wep.Index == p.Index;
                //     });
                //     if (w is not null)
                //     {
                //         Log.Write($"Found {p.DesignerName} on {player}");
                //         found = true;
                //     }
                // }
                //
                // if (!found)
                // {
                //     Log.Write($"Didnt find {p.DesignerName}, removing");
                //     p.Remove();
                // }
            }
        });

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid;
        var playerTeam = (CsTeam)@event.Team;

        _tPlayers.Remove(player);
        _ctPlayers.Remove(player);

        switch (playerTeam)
        {
            case CsTeam.Spectator:
            case CsTeam.None:
                _tPlayers.Remove(player);
                _ctPlayers.Remove(player);
                break;
            case CsTeam.Terrorist:
                _tPlayers.Add(player);
                break;
            case CsTeam.CounterTerrorist:
                _ctPlayers.Add(player);
                break;
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        _tPlayers.Remove(@event.Userid);
        _ctPlayers.Remove(@event.Userid);
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundPostStart(EventRoundPoststart @event, GameEventInfo info)
    {
        if (Helpers.IsWarmup())
        {
            return HookResult.Continue;
        }
        Log.Write($"#T Players: {string.Join(",", _tPlayers.Select(Helpers.GetSteamId))}");
        Log.Write($"#CT Players: {string.Join(",", _ctPlayers.Select(Helpers.GetSteamId))}");

        OnRoundPostStartHelper.Handle(
            _nextRoundType,
            _tPlayers,
            _ctPlayers,
            Helpers.PlayerIsValid,
            Helpers.GetSteamId,
            Helpers.GetTeam,
            GiveDefuseKit,
            AllocateItemsForPlayer,
            out var currentRoundType
        );
        _currentRoundType = currentRoundType;
        _nextRoundType = null;

        var messagePrefix = $"[{ChatColors.Green}RetakesAllocator{ChatColors.White}] ";
        Server.PrintToChatAll(
            $"{messagePrefix}{Enum.GetName(_currentRoundType.Value)} Round"
        );

        return HookResult.Continue;
    }

    #endregion

    #region Helpers

    private void AllocateItemsForPlayer(CCSPlayerController player, ICollection<CsItem> items)
    {
        // Log.Write($"Allocating items: {string.Join(",", items)}");
        AddTimer(0.1f, () =>
        {
            if (!Helpers.PlayerIsValid(player))
            {
                // Log.Write($"Player is not valid when allocating item");
                return;
            }

            foreach (var item in items)
            {
                player.GiveNamedItem(item);
            }

            if ((CsTeam)player.TeamNum == CsTeam.Terrorist)
            {
                AddTimer(0.1f, () => { NativeAPI.IssueClientCommand((int)player.UserId!, "slot5"); });
            }
        });
    }

    private void GiveDefuseKit(CCSPlayerController player)
    {
        AddTimer(0.1f, () =>
        {
            if (player.PlayerPawn.Value?.ItemServices?.Handle is null || !Helpers.PlayerIsValid(player))
            {
                // Log.Write($"Player is not valid when giving defuse kit");
                return;
            }

            var itemServices = new CCSPlayer_ItemServices(player.PlayerPawn.Value.ItemServices.Handle);
            itemServices.HasDefuser = true;
        });
    }

    #endregion
}
