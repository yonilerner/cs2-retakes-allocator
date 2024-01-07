using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;

namespace RetakesAllocatorCore;


public class OnRoundPostStartHelper
{
    public static void Handle<T>(
        RoundType? nextRoundType,
        ICollection<T> tPlayers,
        ICollection<T> ctPlayers,
        Func<T?, bool> isPlayerValid,
        Func<T?, ulong> getSteamId,
        Func<T, CsTeam> getTeam,
        Action<T> giveDefuseKit,
        Action<T, ICollection<CsItem>> allocateItemsForPlayer,
        out RoundType currentRoundType
    )
    {
        var roundType = nextRoundType ?? RoundTypeHelpers.GetRandomRoundType();
        currentRoundType = roundType;
        
        Log.Write($"Round type: {roundType}");
        Log.Write($"#T Players: {tPlayers.Count}");
        Log.Write($"#CT Players: {ctPlayers.Count}");

        var allPlayers = ctPlayers.Concat(tPlayers).ToList();
        
        
        var playerIds = allPlayers
            .Where(isPlayerValid)
            .Select(getSteamId)
            .Where(id => id != 0)
            .ToList();
        var userSettingsByPlayerId = Queries.GetUsersSettings(playerIds);

        var defusingPlayer = Utils.Choice(ctPlayers);

        foreach (var player in allPlayers)
        {
            var team = getTeam(player);
            var playerSteamId = getSteamId(player);
            userSettingsByPlayerId.TryGetValue(playerSteamId, out var userSettings);
            var items = new List<CsItem>
            {
                RoundTypeHelpers.GetArmorForRoundType(roundType),
                team == CsTeam.Terrorist ? CsItem.DefaultKnifeT : CsItem.DefaultKnifeCT,
            };
            var pref = userSettings?.GetWeaponPreference(team, roundType) ?? CsItem.Knife;
            Log.Write($"Weapon pref!: {pref} {roundType} {team}");

            var userWeapons = userSettings?.GetWeaponsForTeamAndRound(team, roundType);
            if (userWeapons is not null)
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

            allocateItemsForPlayer(player, items);
        }
    }
}
