using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.CounterStrikeSharpMock;
using RetakesAllocatorCore.Db;

namespace RetakesAllocatorCore.Helpers;

public class OnRoundPostStartHelper
{
    public static HookResult Handle(
        RoundType? nextRoundType,
        ICounterStrikeSharpMock counterStrikeSharp,
        out RoundType? currentRoundType
    )
    {
        var roundType = nextRoundType ?? RoundTypeHelpers.GetRandomRoundType();
        currentRoundType = roundType;

        if (counterStrikeSharp.IsWarmup())
        {
            return HookResult.Continue;
        }

        var allPlayers = counterStrikeSharp.Utilities.GetPlayers();
        var tPlayers = new List<ICCSPlayerControllerMock>();
        var ctPlayers = new List<ICCSPlayerControllerMock>();
        var playerIds = new List<ulong>();
        foreach (var player in allPlayers)
        {
            var steamId = player.SteamId;
            if (steamId != 0)
            {
                playerIds.Add(steamId);
            }

            var playerTeam = player.Team;
            if (playerTeam == CsTeam.Terrorist)
            {
                tPlayers.Add(player);
            }
            else if (playerTeam == CsTeam.CounterTerrorist)
            {
                ctPlayers.Add(player);
            }
        }

        Log.Write($"#T Players: {string.Join(",", tPlayers.Select(p => p.SteamId))}");
        Log.Write($"#CT Players: {string.Join(",", ctPlayers.Select(p => p.SteamId))}");

        var userSettingsByPlayerId = Queries.GetUsersSettings(playerIds);

        var defusingPlayer = Utils.Choice(ctPlayers);

        HashSet<ICCSPlayerControllerMock> FilterByPreferredWeaponPreference(IEnumerable<ICCSPlayerControllerMock> ps) =>
            ps.Where(p =>
                    userSettingsByPlayerId.TryGetValue(p.SteamId, out var userSetting) &&
                    userSetting.GetWeaponPreference(p.Team, WeaponAllocationType.Preferred) is not null)
                .ToHashSet();

        ICollection<ICCSPlayerControllerMock> tPreferredPlayers = new HashSet<ICCSPlayerControllerMock>();
        ICollection<ICCSPlayerControllerMock> ctPreferredPlayers = new HashSet<ICCSPlayerControllerMock>();
        if (roundType == RoundType.FullBuy)
        {
            tPreferredPlayers = WeaponHelpers.SelectPreferredPlayers(FilterByPreferredWeaponPreference(tPlayers));
            ctPreferredPlayers = WeaponHelpers.SelectPreferredPlayers(FilterByPreferredWeaponPreference(ctPlayers));
        }

        foreach (var player in allPlayers)
        {
            var team = player.Team;
            var playerSteamId = player.SteamId;
            userSettingsByPlayerId.TryGetValue(playerSteamId, out var userSetting);
            var items = new List<CsItem>
            {
                RoundTypeHelpers.GetArmorForRoundType(roundType),
                team == CsTeam.Terrorist ? CsItem.DefaultKnifeT : CsItem.DefaultKnifeCT,
            };

            var givePreferred = team switch
            {
                CsTeam.Terrorist => tPreferredPlayers.Contains(player),
                CsTeam.CounterTerrorist => ctPreferredPlayers.Contains(player),
                _ => false,
            };

            items.AddRange(
                WeaponHelpers.GetWeaponsForRoundType(
                    roundType,
                    team,
                    userSetting,
                    givePreferred
                )
            );

            if (team == CsTeam.CounterTerrorist)
            {
                // On non-pistol rounds, everyone gets defuse kit and util
                if (roundType != RoundType.Pistol)
                {
                    counterStrikeSharp.GiveDefuseKit(player);
                    items.AddRange(RoundTypeHelpers.GetRandomUtilForRoundType(roundType, team));
                }
                else
                {
                    // On pistol rounds, you get util *or* a defuse kit
                    if (defusingPlayer?.SteamId == player.SteamId)
                    {
                        counterStrikeSharp.GiveDefuseKit(player);
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

            counterStrikeSharp.AllocateItemsForPlayer(player, items, team == CsTeam.Terrorist ? "slot5" : "slot1");
        }

        if (Configs.GetConfigData().EnableRoundTypeAnnouncement)
        {
            counterStrikeSharp.Server.PrintToChatAll(
                $"{counterStrikeSharp.MessagePrefix}{Enum.GetName(roundType)} Round"
            );
        }

        return HookResult.Continue;
    }
}
