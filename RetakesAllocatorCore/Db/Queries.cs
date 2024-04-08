using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.EntityFrameworkCore;

namespace RetakesAllocatorCore.Db;

public class Queries
{
    public static async Task<UserSetting?> GetUserSettings(ulong userId)
    {
        return await Db.GetInstance().UserSettings.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
    }

    private static async Task<UserSetting?> UpsertUserSettings(ulong userId, Action<UserSetting> mutation)
    {
        if (userId == 0)
        {
            Log.Debug("Encountered userid 0, not upserting user settings");
            return null;
        }
        
        Log.Debug($"Upserting settings for {userId}");

        var instance = Db.GetInstance();
        var isNew = false;
        var userSettings = await instance.UserSettings.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        if (userSettings is null)
        {
            userSettings = new UserSetting {UserId = userId};
            await instance.UserSettings.AddAsync(userSettings);
            isNew = true;
        }

        instance.Entry(userSettings).State = isNew ? EntityState.Added : EntityState.Modified;

        mutation(userSettings);

        await instance.SaveChangesAsync();
        instance.Entry(userSettings).State = EntityState.Detached;

        return userSettings;
    }

    public static async Task SetWeaponPreferenceForUserAsync(ulong userId, CsTeam team,
        WeaponAllocationType weaponAllocationType,
        CsItem? item)
    {
        await UpsertUserSettings(userId,
            userSetting => { userSetting.SetWeaponPreference(team, weaponAllocationType, item); });
    }

    public static void SetWeaponPreferenceForUser(ulong userId, CsTeam team, WeaponAllocationType weaponAllocationType,
        CsItem? item)
    {
        Task.Run(async () => { await SetWeaponPreferenceForUserAsync(userId, team, weaponAllocationType, item); });
    }

    public static async Task ClearWeaponPreferencesForUserAsync(ulong userId)
    {
        await UpsertUserSettings(userId, userSetting => { userSetting.WeaponPreferences = new(); });
    }

    public static void ClearWeaponPreferencesForUser(ulong userId)
    {
        Task.Run(async () => { await ClearWeaponPreferencesForUserAsync(userId); });
    }

    public static async Task SetPreferredWeaponPreferenceAsync(ulong userId, CsItem? item)
    {
        await UpsertUserSettings(userId, userSetting =>
        {
            userSetting.SetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.Preferred,
                WeaponHelpers.CoercePreferredTeam(item, CsTeam.Terrorist));
            userSetting.SetWeaponPreference(CsTeam.CounterTerrorist, WeaponAllocationType.Preferred,
                WeaponHelpers.CoercePreferredTeam(item, CsTeam.CounterTerrorist));
        });
    }

    public static void SetPreferredWeaponPreference(ulong userId, CsItem? item)
    {
        Task.Run(async () => { await SetPreferredWeaponPreferenceAsync(userId, item); });
    }

    public static IDictionary<ulong, UserSetting> GetUsersSettings(ICollection<ulong> userIds)
    {
        var userSettingsList = Db.GetInstance()
            .UserSettings
            .AsNoTracking()
            .Where(u => userIds.Contains(u.UserId))
            .ToList();
        if (userSettingsList.Count == 0)
        {
            return new Dictionary<ulong, UserSetting>();
        }

        return userSettingsList
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public static void Migrate()
    {
        Db.GetInstance().Database.Migrate();
    }

    public static void Wipe()
    {
        Db.GetInstance().UserSettings.ExecuteDelete();
    }

    public static void Disconnect()
    {
        Db.Disconnect();
    }
}