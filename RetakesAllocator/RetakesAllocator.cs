using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.EntityFrameworkCore;
using RetakesAllocatorCore.db;
using RetakesAllocatorCore;

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
        SQLitePCL.Batteries.Init();

        Db.Instance ??= new Db();
        Db.GetInstance().Database.Migrate();

        if (hotReload)
        {
            HandleHotReload();
        }
    }

    private void HandleHotReload()
    {
        _tPlayers.Clear();
        _ctPlayers.Clear();
        Server.ExecuteCommand($"map {Server.MapName}");
    }

    public override void Unload(bool hotReload)
    {
        Log.Write($"Unloaded");

        Db.Instance?.Dispose();
        Db.Instance = null;
    }

    #endregion

    #region Commands

    [ConsoleCommand("css_weapon")]
    [CommandHelper(minArgs: 2, usage: "P|H|F weapon", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnWeaponCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!Utils.PlayerIsValid(player))
        {
            return;
        }

        var playerId = player?.AuthorizedSteamID?.SteamId64 ?? 0;
        var team = (CsTeam) player!.TeamNum;

        var roundTypeInput = commandInfo.GetArg(1).Trim();
        var roundType = Utils.ParseRoundType(roundTypeInput);
        if (roundType is null)
        {
            commandInfo.ReplyToCommand($"Invalid round type provided: {roundTypeInput}");
            return;
        }

        var weaponInput = commandInfo.GetArg(2).Trim();
        CsItem? weapon;
        if (WeaponHelpers.IsRemoveWeaponSentinel(weaponInput))
        {
            weapon = null;
        }
        else
        {
            var foundWeapons = WeaponHelpers.FindItemByName(weaponInput);
            if (foundWeapons.Count == 0)
            {
                commandInfo.ReplyToCommand($"Weapon '{weaponInput}' not found.");
                return;
            }

            // if (foundWeapons.Count != 1)
            // {
            //     commandInfo.ReplyToCommand($"Weapon '{weaponInput}' matches multiple weapons: {foundWeapons}");
            //     return;
            // }

            var firstWeapon = foundWeapons.First();

            if (!WeaponHelpers.IsValidWeapon((RoundType) roundType, team, firstWeapon))
            {
                commandInfo.ReplyToCommand(
                    $"Weapon '{firstWeapon}' is not valid for round={roundType} and team={team}");
                return;
            }

            weapon = firstWeapon;
        }

        var userSettings = Db.GetInstance().UserSettings.FirstOrDefault(u => u.UserId == playerId) ??
                           new UserSetting {UserId = playerId};
        Db.GetInstance().Attach(userSettings);
        userSettings.SetWeaponPreference(team, (RoundType) roundType, weapon);
        Db.GetInstance().SaveChanges();
        commandInfo.ReplyToCommand($"Weapon '{weapon}' is now your preference.");
    }

    [ConsoleCommand("css_nextround", "Sets the next round type.")]
    [CommandHelper(minArgs: 1, usage: "[P/H/F]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnNextRoundCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var type = commandInfo.GetArg(1).ToUpper();
        if (type == "P")
        {
            _nextRoundType = RoundType.Pistol;
            commandInfo.ReplyToCommand($"Next round will be a pistol round.");
            return;
        }

        if (type == "H")
        {
            _nextRoundType = RoundType.HalfBuy;
            commandInfo.ReplyToCommand($"Next round will be a halfbuy round.");
            return;
        }

        if (type == "F")
        {
            _nextRoundType = RoundType.FullBuy;
            commandInfo.ReplyToCommand($"Next round will be a fullbuy round.");
            return;
        }

        commandInfo.ReplyToCommand($"[Allocator] You must specify a round type [P/H/F]");
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

        Log.Write("Players:");
        Log.Write($"T: {_tPlayers.Count}");
        Log.Write($"CT: {_ctPlayers.Count}");

        var playerIds = _ctPlayers.Concat(_tPlayers)
            .Where(p => p.IsValid && p.UserId is not null && p.AuthorizedSteamID is not null)
            .Select(x => x.AuthorizedSteamID!.SteamId64)
            .ToList();
        Log.Write($"playerIds: {string.Join(",", playerIds)}");
        var userSettingsList = Db.GetInstance()
            .UserSettings
            .AsNoTracking()
            // .Where(u => playerIds.Contains(u.UserId))
            .ToList();
        Log.Write($"Players: {userSettingsList.Count}");
        var userSettingsByPlayerId = userSettingsList
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => g.First());
        Log.Write($"Players by ID: {userSettingsByPlayerId.Count}");
        Log.Write($"K: {string.Join(",", userSettingsByPlayerId.Keys)}");

        foreach (var player in _tPlayers)
        {
            var playerSteamId = player.AuthorizedSteamID?.SteamId64 ?? 0;
            userSettingsByPlayerId.TryGetValue(playerSteamId, out var userSettings);
            Log.Write($"Found user settings {userSettings}");
            if (userSettings != null)
            {
                Log.Write($"Found preferences {string.Join(",", userSettings.WeaponPreferences.Keys)}");
            }
            var items = new List<CsItem>
            {
                RoundTypeHelpers.GetArmorForRoundType(roundType),
                CsItem.Knife,
            };
            items.AddRange(RoundTypeHelpers.GetRandomUtilForRoundType(roundType, CsTeam.Terrorist)
            );
            items.AddRange(
                userSettings?.GetWeaponsForTeamAndRound(CsTeam.Terrorist, roundType) ??
                WeaponHelpers.GetRandomWeaponsForRoundType(roundType, CsTeam.Terrorist)
            );

            AllocateItemsForPlayer(player, items);
        }

        var defusingPlayer = Utils.Choice(_ctPlayers);
        foreach (var player in _ctPlayers)
        {
            var playerSteamId = player.AuthorizedSteamID?.SteamId64 ?? 0;
            userSettingsByPlayerId.TryGetValue(playerSteamId, out var userSettings);
            var items = new List<CsItem>
            {
                RoundTypeHelpers.GetArmorForRoundType(roundType),
                CsItem.Knife,
            };
            items.AddRange(
                userSettings?.GetWeaponsForTeamAndRound(CsTeam.CounterTerrorist, roundType) ??
                WeaponHelpers.GetRandomWeaponsForRoundType(roundType, CsTeam.CounterTerrorist)
            );

            // On non-pistol rounds, everyone gets defuse kit and util
            if (roundType != RoundType.Pistol)
            {
                GiveDefuseKit(player);
                items.AddRange(RoundTypeHelpers.GetRandomUtilForRoundType(roundType, CsTeam.CounterTerrorist));
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
                    items.AddRange(RoundTypeHelpers.GetRandomUtilForRoundType(roundType, CsTeam.CounterTerrorist));
                }
            }

            AllocateItemsForPlayer(player, items);
        }

        return HookResult.Continue;
    }

    #endregion

    #region Helpers

    private void AllocateItemsForPlayer(CCSPlayerController player, IList<CsItem> items)
    {
        AddTimer(0.1f, () =>
        {
            if (!Utils.PlayerIsValid(player))
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
            if (player.PlayerPawn.Value?.ItemServices?.Handle == null || !Utils.PlayerIsValid(player))
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
