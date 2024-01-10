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
    public override string ModuleVersion => "1.0.0";

    private RoundType? _nextRoundType;
    private RoundType? _currentRoundType;

    #region Setup

    public override void Load(bool hotReload)
    {
        Log.Write("Loaded");
        ResetState();
        Batteries.Init();

        RegisterListener<Listeners.OnMapStart>(mapName => { ResetState(); });

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
        commandInfo.ReplyToCommand($"Reloading config for version {ModuleVersion}");
        Configs.Load(ModuleDirectory);
    }

    #endregion

    #region Events

    [GameEventHandler]
    public HookResult OnPostItemPurchase(EventItemPurchase @event, GameEventInfo info)
    {
        if (Helpers.IsWarmup() || !Helpers.PlayerIsValid(@event.Userid) || !@event.Userid.PlayerPawn.IsValid)
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

        var playerPos = @event.Userid.PlayerPawn.Value?.AbsOrigin;

        var pEntity = new CEntityIdentity(EntitySystem.FirstActiveEntity);
        for (; pEntity is not null && pEntity.Handle != IntPtr.Zero; pEntity = pEntity.Next)
        {
            var p = Utilities.GetEntityFromIndex<CBasePlayerWeapon>((int)pEntity.EntityInstance.Index);
            if (!p.IsValid || !p.DesignerName.StartsWith("weapon") || playerPos is null || p.AbsOrigin is null)
            {
                continue;
            }

            var distance = Helpers.GetVectorDistance(playerPos, p.AbsOrigin);
            if (distance < 20)
            {
                AddTimer(.5f, () =>
                {
                    if (p.IsValid && !p.OwnerEntity.IsValid)
                    {
                        p.Remove();
                    }
                });
            }
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundPostStart(EventRoundPoststart @event, GameEventInfo info)
    {
        if (Helpers.IsWarmup())
        {
            return HookResult.Continue;
        }

        var allPlayers = Utilities.GetPlayers()
            .Where(Helpers.PlayerIsValid)
            .ToList();

        OnRoundPostStartHelper.Handle(
            _nextRoundType,
            allPlayers,
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
