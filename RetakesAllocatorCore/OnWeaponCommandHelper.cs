using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore.db;

namespace RetakesAllocatorCore;

public class OnWeaponCommandHelper
{
    public static string? Handle(ICollection<string> args, ulong userId, CsTeam team, bool remove)
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
        
        var foundWeapons = WeaponHelpers.FindItemByName(weaponInput);
        if (foundWeapons.Count == 0)
        {
            return $"Weapon '{weaponInput}' not found.";
        }
        var weapon = foundWeapons.First();
        
        var roundType = WeaponHelpers.GetRoundTypeForWeapon(weapon);
        if (roundType is null)
        {
            return $"Invalid weapon '{weapon}'";
        }
        
        if (!WeaponHelpers.IsValidWeapon(roundType.Value, team, weapon))
        {
            return $"Weapon '{weapon}' is not valid for {roundType} rounds on {team}";
        }

        if (remove)
        {
            Queries.SetWeaponPreferenceForUser(userId, team, roundType.Value, null);
            return $"Weapon '{weapon}' is no longer your {roundType} preference for {team}.";
        }
        else
        {
            Queries.SetWeaponPreferenceForUser(userId, team, roundType.Value, weapon);
            return $"Weapon '{weapon}' is now your {roundType} preference for {team}.";
        }
    }
}
