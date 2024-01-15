using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore.Db;
using RetakesAllocatorCore.Config;

namespace RetakesAllocatorCore;

public class OnWeaponCommandHelper
{
    public static string Handle(ICollection<string> args, ulong userId, RoundType? roundType, CsTeam currentTeam,
        bool remove, out CsItem? outWeapon)
    {
        outWeapon = null;
        if (!Configs.GetConfigData().CanPlayersSelectWeapons())
        {
            return "Players cannot choose their weapons on this server.";
        }

        var weaponInput = args.ElementAt(0).Trim();

        CsTeam team;
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
        else if (currentTeam is CsTeam.None or CsTeam.Spectator)
        {
            return "You must join a team before running this command.";
        }
        else
        {
            team = currentTeam;
        }

        var foundWeapons = WeaponHelpers.FindValidWeaponsByName(weaponInput);
        if (foundWeapons.Count == 0)
        {
            return $"Weapon '{weaponInput}' not found.";
        }

        var weapon = foundWeapons.First();

        if (!WeaponHelpers.IsUsableWeapon(weapon))
        {
            return $"Weapon '{weapon}' is not allowed to be selected.";
        }

        var weaponRoundTypes = WeaponHelpers.GetRoundTypesForWeapon(weapon);
        if (weaponRoundTypes.Count == 0)
        {
            return $"Invalid weapon '{weapon}'";
        }

        var allocationType = WeaponHelpers.GetWeaponAllocationTypeForWeaponAndRound(
            roundType, team, weapon
        );
        var isPreferred = allocationType == WeaponAllocationType.Preferred;

        var allocateImmediately = (
            // Always true for pistols
            roundType is not null &&
            weaponRoundTypes.Contains(roundType.Value) &&
            // Only set the outWeapon if the user is setting the preference for their current team
            currentTeam == team &&
            // TODO Allow immediate allocation of preferred if the config permits it (eg. unlimited preferred)
            // Could be tricky for max # per team config, since this function doesnt know # of players on the team
            !isPreferred
        );

        if (allocationType is null)
        {
            return $"Weapon '{weapon}' is not valid for {team}";
        }


        if (remove)
        {
            if (isPreferred)
            {
                Queries.SetPreferredWeaponPreference(userId, null);
                return $"You will no longer receive '{weapon}'.";
            }
            else
            {
                Queries.SetWeaponPreferenceForUser(userId, team, allocationType.Value, null);
                return $"Weapon '{weapon}' is no longer your {allocationType.Value} preference for {team}.";
            }
        }

        string message;
        if (isPreferred)
        {
            Queries.SetPreferredWeaponPreference(userId, weapon);
            // If we ever add more preferred weapons, we need to change the wording of "sniper" here
            message = $"You will now get a '{weapon}' when its your turn for a sniper.";
        }
        else
        {
            Queries.SetWeaponPreferenceForUser(userId, team, allocationType.Value, weapon);
            message = $"Weapon '{weapon}' is now your {allocationType.Value} preference for {team}.";
        }

        if (allocateImmediately)
        {
            outWeapon = weapon;
        }
        else if (!isPreferred)
        {
            message += $" You will get it at the next {weaponRoundTypes.First()} round.";
        }

        return message;
    }
}
