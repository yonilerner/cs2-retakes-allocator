using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore.db;

namespace RetakesAllocatorCore;

public class OnWeaponCommandHelper
{
    public static string? Handle(ICollection<string> args, ulong userId, CsTeam team)
    {
        var roundTypeInput = args.ElementAt(0).Trim();
        var roundType = RoundTypeHelpers.ParseRoundType(roundTypeInput);
        if (roundType is null)
        {
            return $"Invalid round type provided: {roundTypeInput}";
        }

        var weaponInput = args.ElementAt(1).Trim();
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

            if (!WeaponHelpers.IsValidWeapon((RoundType) roundType, team, firstWeapon))
            {
                return $"Weapon '{firstWeapon}' is not valid for round={roundType} and team={team}";
            }

            weapon = firstWeapon;
        }

        Queries.SetWeaponPreferenceForUser(userId, team, (RoundType) roundType, weapon);
        return $"Weapon '{weapon}' is now your preference.";
    }
}
