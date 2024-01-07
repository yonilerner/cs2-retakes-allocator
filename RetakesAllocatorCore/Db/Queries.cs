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

    public static void SetWeaponPreferenceForUser(ulong userId, CsTeam team, RoundType roundType, CsItem? item)
    {
        var isNew = false;
        var userSettings = Db.GetInstance().UserSettings.FirstOrDefault(u => u.UserId == userId);
        if (userSettings is null)
        {
            userSettings = new UserSetting {UserId = userId};
            Db.GetInstance().UserSettings.Add(userSettings);
            isNew = true;
        }

        userSettings.SetWeaponPreference(team, roundType, item);
        Db.GetInstance().Entry(userSettings).State = isNew ? EntityState.Added : EntityState.Modified;

        Db.GetInstance().SaveChanges();
        Db.GetInstance().Entry(userSettings).State = EntityState.Detached;
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
