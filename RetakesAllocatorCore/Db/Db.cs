using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.EntityFrameworkCore;
using RetakesAllocatorCore.Config;

namespace RetakesAllocatorCore.Db;

public class Db : DbContext
{
    public DbSet<UserSetting> UserSettings { get; set; }

    private static Db? Instance { get; set; }

    public static Db GetInstance()
    {
        return Instance ??= new Db();
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

        // TODO This whole thing needs to be fixed per
        // https://jasonwatmore.com/post/2020/01/03/aspnet-core-ef-core-migrations-for-multiple-databases-sqlite-and-sql-server
        var configData = Configs.IsLoaded() ? Configs.GetConfigData() : new ConfigData();
        var databaseConnectionString = configData.DatabaseConnectionString;
        switch (configData.DatabaseProvider)
        {
            case DatabaseProvider.Sqlite:
                optionsBuilder.UseSqlite(databaseConnectionString);
                break;
            case DatabaseProvider.MySql:
                var version = ServerVersion.AutoDetect(databaseConnectionString);
                optionsBuilder.UseMySql(databaseConnectionString, version);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserSetting>()
            .Property(e => e.WeaponPreferences)
            .IsRequired(false);
        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        UserSetting.Configure(configurationBuilder);
        configurationBuilder
            .Properties<CsItem>()
            .HaveConversion<CsItemConverter>();
    }
}
