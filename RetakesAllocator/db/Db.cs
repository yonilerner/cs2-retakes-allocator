using System.Text.Json;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RetakesAllocator.db;

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
            throw new Exception("Database was not initialized");
        }

        return Instance;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source=C:\\cs2-server\\game\\bin\\win64\\data.db");
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<WeaponPreferencesType>()
            .HaveConversion<WeaponPreferencesConverter, WeaponPreferencesComparer>();
    }
}
