using System.Collections.Concurrent;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.EntityFrameworkCore;

namespace RetakesAllocatorCore.Db;

public class Queries
{
    // Every operation gets its own DbContext: EF Core contexts are not thread safe, and this
    // plugin reads on the game thread (round start) while preference writes run on the thread
    // pool. Writes are serialized so two quick changes for one player cannot overwrite each other.
    private static readonly SemaphoreSlim WriteLock = new(1, 1);

    // Settings only change through this plugin, so they are read from the database once per
    // player and kept until the player disconnects or the map changes. Round start then needs
    // no database round trip. A null value records "no row yet".
    private static readonly ConcurrentDictionary<ulong, UserSetting?> Cache = new();

    public static async Task<UserSetting?> GetUserSettings(ulong userId)
    {
        if (Cache.TryGetValue(userId, out var cached))
        {
            return cached;
        }

        await using var instance = Db.Create();
        var userSettings = await instance.UserSettings.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        return Cache.GetOrAdd(userId, userSettings);
    }

    private static async Task<UserSetting?> UpsertUserSettings(ulong userId, Action<UserSetting> mutation)
    {
        if (userId == 0)
        {
            Log.Debug("Encountered userid 0, not upserting user settings");
            return null;
        }

        Log.Debug($"Upserting settings for {userId}");

        await WriteLock.WaitAsync();
        try
        {
            await using var instance = Db.Create();
            var isNew = false;
            var userSettings = await instance.UserSettings.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
            if (userSettings is null)
            {
                userSettings = new UserSetting {UserId = userId};
                isNew = true;
            }

            mutation(userSettings);

            instance.Entry(userSettings).State = isNew ? EntityState.Added : EntityState.Modified;
            await instance.SaveChangesAsync();
            instance.Entry(userSettings).State = EntityState.Detached;

            Cache[userId] = userSettings;
            return userSettings;
        }
        finally
        {
            WriteLock.Release();
        }
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
        var result = new Dictionary<ulong, UserSetting>();
        var missing = new List<ulong>();
        foreach (var userId in userIds)
        {
            if (Cache.TryGetValue(userId, out var cached))
            {
                if (cached is not null)
                {
                    result[userId] = cached;
                }
            }
            else if (!missing.Contains(userId))
            {
                missing.Add(userId);
            }
        }

        if (missing.Count == 0)
        {
            return result;
        }

        using var instance = Db.Create();
        var loaded = instance
            .UserSettings
            .AsNoTracking()
            .Where(u => missing.Contains(u.UserId))
            .ToList()
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var userId in missing)
        {
            loaded.TryGetValue(userId, out var userSetting);
            // A write that finished while this read was running wins over the older row.
            var current = Cache.GetOrAdd(userId, userSetting);
            if (current is not null)
            {
                result[userId] = current;
            }
        }

        return result;
    }

    /// <summary>Drops a player's cached settings, so the next read comes from the database.</summary>
    public static void ForgetUser(ulong userId)
    {
        Cache.TryRemove(userId, out _);
    }

    public static void ClearCache()
    {
        Cache.Clear();
    }

    public static void Migrate()
    {
        using var instance = Db.Create();
        instance.Database.Migrate();
    }

    public static void Wipe()
    {
        using var instance = Db.Create();
        instance.UserSettings.ExecuteDelete();
        Cache.Clear();
    }

    public static void Disconnect()
    {
        Cache.Clear();
    }
}
