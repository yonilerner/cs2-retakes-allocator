using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore.Db;
using RetakesAllocatorCore.Config;

namespace RetakesAllocatorCore;

public class OnWeaponCommandHelper
{
    private static readonly object SaveQueueLock = new();
    private static Task _saveQueue = Task.CompletedTask;

    public static string Handle(ICollection<string> args, ulong userId, RoundType? roundType, CsTeam currentTeam,
        bool remove, out CsItem? outWeapon)
    {
        var (result, save) = Prepare(args, userId, roundType, currentTeam, remove);
        if (save is not null)
        {
            _ = SaveAndLogAsync(save);
        }
        outWeapon = result.Item2;
        return result.Item1;
    }

    public static async Task<Tuple<string, CsItem?>> HandleAsync(ICollection<string> args, ulong userId,
        RoundType? roundType, CsTeam currentTeam,
        bool remove)
    {
        var (result, save) = Prepare(args, userId, roundType, currentTeam, remove);
        if (save is not null)
        {
            await QueueSave(save);
        }
        return result;
    }

    private static Task QueueSave(Func<Task> save)
    {
        // Keep menu selections in order, but run every database operation on a worker.
        lock (SaveQueueLock)
        {
            _saveQueue = _saveQueue.ContinueWith(_ => save(), CancellationToken.None,
                TaskContinuationOptions.None, TaskScheduler.Default).Unwrap();
            return _saveQueue;
        }
    }

    private static async Task SaveAndLogAsync(Func<Task> save)
    {
        try
        {
            await QueueSave(save);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to save weapon preference: {ex}");
        }
    }

    private static (Tuple<string, CsItem?> Result, Func<Task>? Save) Prepare(ICollection<string> args, ulong userId,
        RoundType? roundType, CsTeam currentTeam, bool remove)
    {
        CsItem? outWeapon = null;
        Func<Task>? save = null;

        (Tuple<string, CsItem?>, Func<Task>?) Ret(string str) => (new(str, outWeapon), save);

        if (!Configs.GetConfigData().CanPlayersSelectWeapons())
        {
            return Ret(Translator.Instance["weapon_preference.cannot_choose"]);
        }

        if (args.Count == 0)
        {
            var gunsMessage = Translator.Instance[
                "weapon_preference.gun_usage",
                currentTeam,
                string.Join(", ",
                    WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.PistolRound, currentTeam)),
                string.Join(", ",
                    WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.HalfBuyPrimary,
                        currentTeam)),
                string.Join(", ",
                    WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.FullBuyPrimary, currentTeam))
            ];
            return Ret(gunsMessage);
        }

        var weaponInput = args.ElementAt(0).Trim();

        CsTeam team;
        var teamInput = args.ElementAtOrDefault(1)?.Trim().ToLower();
        if (teamInput is not null)
        {
            var parsedTeamInput = Utils.ParseTeam(teamInput);
            if (parsedTeamInput == CsTeam.None)
            {
                return Ret(Translator.Instance["weapon_preference.invalid_team", teamInput]);
            }

            team = parsedTeamInput;
        }
        else if (currentTeam is CsTeam.None or CsTeam.Spectator)
        {
            return Ret(Translator.Instance["weapon_preference.join_team"]);
        }
        else
        {
            team = currentTeam;
        }

        var foundWeapons = WeaponHelpers.FindValidWeaponsByName(weaponInput);
        if (foundWeapons.Count == 0)
        {
            return Ret(Translator.Instance["weapon_preference.not_found", weaponInput]);
        }

        var weapon = foundWeapons.First();

        if (!WeaponHelpers.IsUsableWeapon(weapon))
        {
            return Ret(Translator.Instance["weapon_preference.not_allowed", weapon]);
        }

        var weaponRoundTypes = WeaponHelpers.GetRoundTypesForWeapon(weapon);
        if (weaponRoundTypes.Count == 0)
        {
            return Ret(Translator.Instance["weapon_preference.invalid_weapon", weapon]);
        }

        var allocationType = WeaponHelpers.GetWeaponAllocationTypeForWeaponAndRound(
            roundType, team, weapon
        );
        var isPreferred = allocationType == WeaponAllocationType.Preferred;

        var allocateImmediately = (
            // Always true for pistols
            allocationType is not null &&
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
            return Ret(Translator.Instance["weapon_preference.not_valid_for_team", weapon, team]);
        }


        if (remove)
        {
            if (isPreferred)
            {
                save = () => Queries.SetPreferredWeaponPreferenceAsync(userId, null);
                return Ret(Translator.Instance["weapon_preference.unset_preference_preferred", weapon]);
            }
            else
            {
                save = () => Queries.SetWeaponPreferenceForUserAsync(userId, team, allocationType.Value, null);
                return Ret(
                    Translator.Instance["weapon_preference.unset_preference", weapon, allocationType.Value, team]);
            }
        }

        string message;
        if (isPreferred)
        {
            save = () => Queries.SetPreferredWeaponPreferenceAsync(userId, weapon);
            // If we ever add more preferred weapons, we need to change the wording of "sniper" here
            message = Translator.Instance["weapon_preference.set_preference_preferred", weapon];
        }
        else
        {
            save = () => Queries.SetWeaponPreferenceForUserAsync(userId, team, allocationType.Value, weapon);
            message = Translator.Instance["weapon_preference.set_preference", weapon, allocationType.Value, team];
        }

        if (allocateImmediately)
        {
            outWeapon = weapon;
        }
        else if (!isPreferred)
        {
            message += Translator.Instance["weapon_preference.receive_next_round", weaponRoundTypes.First()];
        }

        if (userId == 0)
        {
            message = Translator.Instance["weapon_preference.not_saved"];
        }

        return Ret(message);
    }
}
