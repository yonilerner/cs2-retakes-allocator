using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.EntityFrameworkCore;
using RetakesAllocatorCore.Config;

namespace RetakesAllocatorCore.Db;

using WeaponPreferencesType = Dictionary<
    CsTeam,
    Dictionary<RoundType, CsItem>
>;

public class Db : DbContext
{
    public DbSet<UserSetting> UserSettings { get; set; }

    public static Db? Instance { get; set; }

    public static Db GetInstance()
    {
        if (Instance is null)
        {
            Instance = new Db();
        }

        return Instance;
    }

    public static void Disconnect()
    {
        Instance?.Dispose();
        Instance = null;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

        var databaseConnectionString = Configs.GetConfigData().DatabaseConnectionString;
        switch (Configs.GetConfigData().DatabaseProvider)
        {
            case DatabaseProvider.Sqlite:
                optionsBuilder.UseSqlite(databaseConnectionString);
                break;
            case DatabaseProvider.MySql:
                optionsBuilder.UseMySQL(databaseConnectionString);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<WeaponPreferencesType>()
            .HaveConversion<WeaponPreferencesConverter, WeaponPreferencesComparer>();
    }
}
