using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore.Db;
using RetakesAllocatorCore.Managers;

namespace RetakesAllocatorCore;

public class OnRoundPostStartHelper
{
    public static void Handle<T>(
        ICollection<T> allPlayers,
        Func<T?, ulong> getSteamId,
        Func<T, CsTeam> getTeam,
        Action<T> giveDefuseKit,
        Action<T, ICollection<CsItem>, string?> allocateItemsForPlayer,
        Func<T, bool> isVip,
        out RoundType currentRoundType
    )
    {
        var roundType = RoundTypeManager.Instance.GetNextRoundType();
        currentRoundType = roundType;

        var tPlayers = new List<T>();
        var ctPlayers = new List<T>();
        var playerIds = new List<ulong>();
        foreach (var player in allPlayers)
        {
            var steamId = getSteamId(player);
            if (steamId != 0)
            {
                playerIds.Add(steamId);
            }

            var playerTeam = getTeam(player);
            if (playerTeam == CsTeam.Terrorist)
            {
                tPlayers.Add(player);
            }
            else if (playerTeam == CsTeam.CounterTerrorist)
            {
                ctPlayers.Add(player);
            }
        }

        Log.Write($"#T Players: {string.Join(",", tPlayers.Select(getSteamId))}");
        Log.Write($"#CT Players: {string.Join(",", ctPlayers.Select(getSteamId))}");

        var userSettingsByPlayerId = Queries.GetUsersSettings(playerIds);

        var defusingPlayer = Utils.Choice(ctPlayers);

        HashSet<T> FilterByPreferredWeaponPreference(IEnumerable<T> ps) =>
            ps.Where(p =>
                    userSettingsByPlayerId.TryGetValue(getSteamId(p), out var userSetting) &&
                    userSetting.GetWeaponPreference(getTeam(p), WeaponAllocationType.Preferred) is not null)
                .ToHashSet();

        ICollection<T> tPreferredPlayers = new List<T>();
        ICollection<T> ctPreferredPlayers = new List<T>();
        if (roundType == RoundType.FullBuy)
        {
            tPreferredPlayers =
                WeaponHelpers.SelectPreferredPlayers(FilterByPreferredWeaponPreference(tPlayers), isVip);
            ctPreferredPlayers =
                WeaponHelpers.SelectPreferredPlayers(FilterByPreferredWeaponPreference(ctPlayers), isVip);
        }

        foreach (var player in allPlayers)
        {
            var team = getTeam(player);
            var playerSteamId = getSteamId(player);
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
                    giveDefuseKit(player);
                    items.AddRange(RoundTypeHelpers.GetRandomUtilForRoundType(roundType, team));
                }
                else
                {
                    // On pistol rounds, you get util *or* a defuse kit
                    if (getSteamId(defusingPlayer) == getSteamId(player))
                    {
                        giveDefuseKit(player);
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

            allocateItemsForPlayer(player, items, team == CsTeam.Terrorist ? "slot5" : "slot1");
        }
    }
}
