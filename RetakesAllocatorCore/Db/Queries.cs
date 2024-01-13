using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.EntityFrameworkCore;

namespace RetakesAllocatorCore.Db;

public class Queries
{
    public static UserSetting? GetUserSettings(ulong userId)
    {
        return Db.GetInstance().UserSettings.FirstOrDefault(u => u.UserId == userId);
    }

    private static UserSetting? UpsertUserSettings(ulong userId, Action<UserSetting> mutation)
    {
        var instance = Db.GetInstance();
        var isNew = false;
        var userSettings = instance.UserSettings.FirstOrDefault(u => u.UserId == userId);
        if (userSettings is null)
        {
            userSettings = new UserSetting {UserId = userId};
            instance.UserSettings.Add(userSettings);
            isNew = true;
        }

        instance.Entry(userSettings).State = isNew ? EntityState.Added : EntityState.Modified;

        mutation(userSettings);

        instance.SaveChanges();
        instance.Entry(userSettings).State = EntityState.Detached;

        return userSettings;
    }

    public static void SetWeaponPreferenceForUser(ulong userId, CsTeam team, WeaponAllocationType weaponAllocationType,
        CsItem? item)
    {
        UpsertUserSettings(userId,
            userSetting => { userSetting.SetWeaponPreference(team, weaponAllocationType, item); });
    }

    public static void SetSniperPreference(ulong userId, CsItem? sniper)
    {
        UpsertUserSettings(userId, userSetting =>
        {
            userSetting.SetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.Sniper,
                WeaponHelpers.CoerceSniperTeam(sniper, CsTeam.Terrorist));
            userSetting.SetWeaponPreference(CsTeam.CounterTerrorist, WeaponAllocationType.Sniper,
                WeaponHelpers.CoerceSniperTeam(sniper, CsTeam.CounterTerrorist));
        });
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
