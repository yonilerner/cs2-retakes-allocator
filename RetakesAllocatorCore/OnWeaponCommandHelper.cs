using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore.db;

namespace RetakesAllocatorCore;

public class OnWeaponCommandHelper
{
    public static string? Handle(ICollection<string> args, ulong userId, CsTeam team, RoundType roundType)
    {
        var weaponInput = args.ElementAt(0).Trim();

        var teamInput = args.ElementAtOrDefault(1)?.Trim().ToLower();
        if (teamInput is not null)
        {
            var parsedTeamInput = Utils.ParseTeam(teamInput);
            if (parsedTeamInput == CsTeam.None)
            {
                return $"Invalid team provided: {teamInput}";
            }

            team = parsedTeamInput;
        }
        
        var roundTypeInput = args.ElementAtOrDefault(2)?.Trim();
        if (roundTypeInput is not null)
        {
            var parsedRoundType = RoundTypeHelpers.ParseRoundType(roundTypeInput);
            if (parsedRoundType is null)
            {
                return $"Invalid round type provided: {roundTypeInput}";
            }

            roundType = parsedRoundType.Value;
        }

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
                return $"Weapon '{weaponInput}' not found.";
            }

            // if (foundWeapons.Count != 1)
            // {
            //     commandInfo.ReplyToCommand($"Weapon '{weaponInput}' matches multiple weapons: {foundWeapons}");
            //     return;
            // }

            var firstWeapon = foundWeapons.First();

            if (!WeaponHelpers.IsValidWeapon(roundType, team, firstWeapon))
            {
                return $"Weapon '{firstWeapon}' is not valid for {roundType} rounds on {team}";
            }

            weapon = firstWeapon;
        }

        Queries.SetWeaponPreferenceForUser(userId, team, roundType, weapon);
        return $"Weapon '{weapon}' is now your {roundType} preference for {team}.";
    }
}
