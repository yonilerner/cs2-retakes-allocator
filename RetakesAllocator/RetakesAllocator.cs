using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
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
    public override string ModuleVersion => "0.0.1";

    private readonly IList<CCSPlayerController> _tPlayers = new List<CCSPlayerController>();
    private readonly IList<CCSPlayerController> _ctPlayers = new List<CCSPlayerController>();

    private RoundType? _nextRoundType;
    private RoundType? _currentRoundType;

    #region Setup

    public override void Load(bool hotReload)
    {
        Log.Write("Loaded");
        Batteries.Init();

        Configs.Load(ModuleDirectory);

        if (Configs.GetConfigData().MigrateOnStartup)
        {
            Queries.Migrate();
        }

        if (hotReload)
        {
            HandleHotReload();
        }

        RegisterListener<Listeners.OnEntitySpawned>(i => { });
    }

    private void ResetState()
    {
        _tPlayers.Clear();
        _ctPlayers.Clear();
    }

    private void HandleHotReload()
    {
        ResetState();
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

    // TODO Make per-user
    private CsItem? _currentWeapon = null;
    private CsItem? _currentPistol = null;

    [GameEventHandler(HookMode.Pre)]
    public HookResult OnPreItemPurchase(EventItemPurchase @event, GameEventInfo info)
    {
        _currentWeapon = Helpers.GetUserWeaponItem(@event.Userid,
            i => WeaponHelpers.GetRoundTypeForWeapon(i) == _currentRoundType);
        _currentPistol = Helpers.GetUserWeaponItem(@event.Userid,
            i => WeaponHelpers.GetRoundTypeForWeapon(i) == RoundType.Pistol);

        Log.Write($"current: w={_currentWeapon} p={_currentPistol}");

        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Post)]
    public HookResult OnPostItemPurchase(EventItemPurchase @event, GameEventInfo info)
    {
        var item = Utils.ToEnum<CsItem>(@event.Weapon);
        var team = (CsTeam)@event.Userid.TeamNum;
        var playerId = Helpers.GetSteamId(@event.Userid);
        var weaponRoundType = WeaponHelpers.GetRoundTypeForWeapon(item);
        
        Log.Write($"item {item} team {team} player {playerId}");
        Log.Write($"curRound {_currentRoundType} weapon Round {weaponRoundType}");
        
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
            var removedWeapon = Helpers.GetUserWeapon(@event.Userid, i => i == item);
            Helpers.RemoveWeapons(@event.Userid, i => i == item);
            Thread.Sleep(150);
            Log.Write($"Removed {item}");
            if (_currentRoundType is not null && WeaponHelpers.GetAllWeapons().Contains(item))
            {
                var replacementItem = WeaponHelpers.GetWeaponForRoundType(_currentRoundType.Value, team,
                    Queries.GetUserSettings(playerId));
                Log.Write($"Replacement item: {replacementItem}");
                if (replacementItem is not null)
                {
                    AllocateItemsForPlayer(@event.Userid, new List<CsItem>
                    {
                        replacementItem.Value
                    });
                }
            }
            removedWeapon?.Value?.Remove();
        }


        Log.Write($"post item: {item}");

        _currentPistol = null;
        _currentWeapon = null;
        return HookResult.Continue;
    }

    // [GameEventHandler(HookMode.Pre)]
    // public HookResult OnPreWeaponhudSelection(EventWeaponhudSelection @event, GameEventInfo eventInfo)
    // {
    //     Log.Write($"Pre HUD Selection: {@event.Entindex} {@event.Mode}.");
    //     return HookResult.Continue;
    // }
    //
    // [GameEventHandler(HookMode.Post)]
    // public HookResult OnPostWeaponhudSelection(EventWeaponhudSelection @event, GameEventInfo eventInfo)
    // {
    //     Log.Write($"Pre HUD Selection: {@event.Entindex} {@event.Mode}.");
    //     return HookResult.Continue;
    // }

    [GameEventHandler]
    public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid;
        var oldTeam = (CsTeam)@event.Oldteam;
        var playerTeam = (CsTeam)@event.Team;

        switch (oldTeam)
        {
            case CsTeam.Terrorist:
                _tPlayers.Remove(player);
                break;
            case CsTeam.CounterTerrorist:
                _ctPlayers.Remove(player);
                break;
        }

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
        var player = _tPlayers.First();
        OnRoundPostStartHelper.Handle(
            _nextRoundType,
            _tPlayers,
            _ctPlayers,
            Helpers.PlayerIsValid,
            Helpers.GetSteamId,
            Helpers.GetTeam,
            GiveDefuseKit,
            AllocateItemsForPlayer,
            (p, money) =>
            {
                if (Helpers.PlayerIsValid(p) && p.InGameMoneyServices is not null)
                {
                    p.InGameMoneyServices.Account = money;
                }
            },
            out var currentRoundType
        );
        _currentRoundType = currentRoundType;
        _nextRoundType = null;
        return HookResult.Continue;
    }

    #endregion

    #region Helpers

    private void AllocateItemsForPlayer(CCSPlayerController player, ICollection<CsItem> items)
    {
        // Helpers.RemoveArmor(player);
        // Helpers.RemoveWeapons(player);
        Log.Write($"Allocating items: {string.Join(",", items)}");
        AddTimer(0.1f, () =>
        {
            if (!Helpers.PlayerIsValid(player))
            {
                Log.Write($"Player is not valid when allocating item");
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
                Log.Write($"Player is not valid when giving defuse kit");
                return;
            }

            var itemServices = new CCSPlayer_ItemServices(player.PlayerPawn.Value.ItemServices.Handle);
            itemServices.HasDefuser = true;
        });
    }

    #endregion
}
