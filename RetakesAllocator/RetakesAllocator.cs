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
        var team = (CsTeam) player!.TeamNum;

        var result = OnWeaponCommandHelper.Handle(
            Helpers.CommandInfoToArgList(commandInfo),
            playerId,
            team,
            false,
            out var selectedWeapon
        );
        if (result != null)
        {
            commandInfo.ReplyToCommand(result);
        }

        if (selectedWeapon != null)
        {
            AllocateItemsForPlayer(player, new List<CsItem> {selectedWeapon.Value});
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
        var team = (CsTeam) player!.TeamNum;

        var result = OnWeaponCommandHelper.Handle(
            Helpers.CommandInfoToArgList(commandInfo),
            playerId,
            team,
            true,
            out _
        );
        if (result != null)
        {
            commandInfo.ReplyToCommand(result);
        }
    }

    [ConsoleCommand("css_nextround", "Sets the next round type.")]
    [CommandHelper(minArgs: 1, usage: "[P/H/F]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
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
    public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid;
        var oldTeam = (CsTeam) @event.Oldteam;
        var playerTeam = (CsTeam) @event.Team;

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
        var roundType = _nextRoundType ?? RoundTypeHelpers.GetRandomRoundType();
        _nextRoundType = null;

        Log.Write($"Round type: {roundType}");
        Log.Write($"#T Players: {_tPlayers.Count}");
        Log.Write($"#CT Players: {_ctPlayers.Count}");

        var allPlayers = _ctPlayers.Concat(_tPlayers).ToList();

        var playerIds = allPlayers
            .Where(Helpers.PlayerIsValid)
            .Select(Helpers.GetSteamId)
            .Where(id => id != 0)
            .ToList();
        var userSettingsByPlayerId = Queries.GetUsersSettings(playerIds);

        var defusingPlayer = Utils.Choice(_ctPlayers);

        foreach (var player in allPlayers)
        {
            var team = (CsTeam) player.TeamNum;
            var playerSteamId = Helpers.GetSteamId(player);
            userSettingsByPlayerId.TryGetValue(playerSteamId, out var userSettings);
            var items = new List<CsItem>
            {
                RoundTypeHelpers.GetArmorForRoundType(roundType),
                team == CsTeam.Terrorist ? CsItem.DefaultKnifeT : CsItem.DefaultKnifeCT,
            };
            var pref = userSettings?.GetWeaponPreference(team, roundType) ?? CsItem.Knife;
            Log.Write($"Weapon pref!: {pref} {roundType} {team}");

            var userWeapons = userSettings?.GetWeaponsForTeamAndRound(team, roundType);
            if (userWeapons != null)
            {
                items.AddRange(userWeapons);
            }
            else if (Configs.GetConfigData().CanAssignRandomWeapons())
            {
                items.AddRange(WeaponHelpers.GetRandomWeaponsForRoundType(roundType, team));
            }
            else if (Configs.GetConfigData().CanAssignDefaultWeapons())
            {
                items.AddRange(WeaponHelpers.GetDefaultWeaponsForRoundType(roundType, team));
            }

            if (team == CsTeam.CounterTerrorist)
            {
                // On non-pistol rounds, everyone gets defuse kit and util
                if (roundType != RoundType.Pistol)
                {
                    GiveDefuseKit(player);
                    items.AddRange(RoundTypeHelpers.GetRandomUtilForRoundType(roundType, team));
                }
                else
                {
                    // On pistol rounds, you get util *or* a defuse kit
                    if (defusingPlayer?.UserId == player.UserId)
                    {
                        GiveDefuseKit(player);
                    }
                    else
                    {
                        items.AddRange(RoundTypeHelpers.GetRandomUtilForRoundType(roundType, team));
                    }
                }
            }
            else
            {
                items.AddRange(RoundTypeHelpers.GetRandomUtilForRoundType(roundType, team));
            }

            AllocateItemsForPlayer(player, items);
        }

        return HookResult.Continue;
    }

    #endregion

    #region Helpers

    private void AllocateItemsForPlayer(CCSPlayerController player, IList<CsItem> items)
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

            if ((CsTeam) player.TeamNum == CsTeam.Terrorist)
            {
                AddTimer(0.1f, () => { NativeAPI.IssueClientCommand((int) player.UserId!, "slot5"); });
            }
        });
    }

    private void GiveDefuseKit(CCSPlayerController player)
    {
        AddTimer(0.1f, () =>
        {
            if (player.PlayerPawn.Value?.ItemServices?.Handle == null || !Helpers.PlayerIsValid(player))
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
