using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore.Config;

namespace RetakesAllocatorCore;

public class NadeHelpers
{
    public static string GlobalSettingName = "GLOBAL";

    public static Stack<CsItem> GetUtilForTeam(string? map, RoundType roundType, CsTeam team, int numPlayers)
    {
        map ??= GlobalSettingName;

        var maxTotalNades = (int) Math.Ceiling(numPlayers * (roundType == RoundType.Pistol ? 1 : 1.5));
        var molly = team == CsTeam.Terrorist ? CsItem.Molotov : CsItem.Incendiary;
        var nadeDistribution = new List<CsItem>
        {
            CsItem.Flashbang, CsItem.Flashbang, CsItem.Flashbang, CsItem.Flashbang,
            CsItem.Smoke, CsItem.Smoke, CsItem.Smoke,
            CsItem.HE, CsItem.HE, CsItem.HE,
            molly, molly
        };

        var nadeAllocations = new Dictionary<CsItem, int>
        {
            {CsItem.Flashbang, GetMaxNades(map, team, CsItem.Flashbang)},
            {CsItem.Smoke, GetMaxNades(map, team, CsItem.Smoke)},
            {CsItem.HE, GetMaxNades(map, team, CsItem.HE)},
            {molly, GetMaxNades(map, team, molly)},
        };

        var nades = new Stack<CsItem>();
        while (true)
        {
            if (nadeAllocations.Count == 0 || maxTotalNades <= 0)
            {
                break;
            }

            var nextNade = Utils.Choice(nadeDistribution);
            if (nadeAllocations[nextNade] <= 0)
            {
                nadeDistribution.RemoveAll(item => item == nextNade);
                nadeAllocations.Remove(nextNade);
                continue;
            }

            nades.Push(nextNade);
            nadeAllocations[nextNade]--;
            maxTotalNades--;
        }

        return nades;
    }

    private static int GetMaxNades(string map, CsTeam team, CsItem nade)
    {
        if (Configs.GetConfigData().MaxNades.TryGetValue(map, out var mapNades))
        {
            if (mapNades.TryGetValue(team, out var teamNades))
            {
                int nadeCount;
                if (teamNades.TryGetValue(nade, out nadeCount))
                {
                    return nadeCount;
                }

                if (nade is CsItem.Molotov or CsItem.Incendiary)
                {
                    var otherNade = nade == CsItem.Molotov ? CsItem.Incendiary : CsItem.Molotov;
                    if (teamNades.TryGetValue(otherNade, out nadeCount))
                    {
                        return nadeCount;
                    }
                }
            }
        }

        if (map == GlobalSettingName)
        {
            return 999999;
        }

        return GetMaxNades(GlobalSettingName, team, nade);
    }

    private static bool PlayerReachedMaxNades(ICollection<CsItem> nades)
    {
        var allowancePerType = new Dictionary<CsItem, int>
        {
            {CsItem.Flashbang, 2},
            {CsItem.Smoke, 1},
            {CsItem.HE, 1},
            {CsItem.Molotov, 1},
            {CsItem.Incendiary, 1},
        };
        foreach (var nade in nades)
        {
            if (!allowancePerType.ContainsKey(nade) || allowancePerType[nade] <= 0)
            {
                return true;
            }
            allowancePerType[nade]--;
        }

        return false;
    }

    public static void AllocateNadesToPlayers<T>(
        Stack<CsItem> teamNades,
        ICollection<T> teamPlayers,
        Dictionary<T, ICollection<CsItem>> nadesByPlayer
    ) where T : notnull
    {
        // Copy to avoid mutating the actual player list
        var teamPlayersShuffled = new List<T>(teamPlayers);
        // Shuffle for fairness
        Utils.Shuffle(teamPlayersShuffled);

        var playerI = 0;
        while (teamNades.Count != 0 && teamPlayersShuffled.Count != 0)
        {
            var player = teamPlayersShuffled[playerI];
            
            if (!nadesByPlayer.TryGetValue(player, out var nadesForPlayer))
            {
                nadesForPlayer = new List<CsItem>();
                nadesByPlayer.Add(player, nadesForPlayer);
            }

            // If a player has reached max nades, remove them from the list and try again at the same index,
            // which is now the next player
            if (PlayerReachedMaxNades(nadesForPlayer))
            {
                teamPlayersShuffled.RemoveAt(playerI);
                continue;
            }

            if (!teamNades.TryPop(out var nextNade))
            {
                break;
            }
            nadesForPlayer.Add(nextNade);
            
            playerI++;
            if (playerI >= teamPlayersShuffled.Count)
            {
                playerI = 0;
            }
        }
    }
}