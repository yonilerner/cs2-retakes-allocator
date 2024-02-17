using System.Runtime.Serialization;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.EntityFrameworkCore;

namespace RetakesAllocatorCore;

public static class Utils
{
    /**
     * Randomly get an item from the collection
     */
    public static T? Choice<T>(ICollection<T> items)
    {
        // Log.Write($"Item count: {items.Count}");
        if (items.Count == 0)
        {
            return default;
        }

        var random = new Random().Next(items.Count);
        // Log.Write($"Random: {random}");
        var item = items.ElementAt(random);
        // Log.Write($"Item: {item}");
        return item;
    }

    public static void Shuffle<T>(IList<T> list)
    {
        var random = new Random();
        var n = list.Count;
        while (n > 1)
        {
            n--;
            var k = random.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    public static CsTeam ParseTeam(string teamInput)
    {
        return teamInput.ToLower() switch
        {
            "t" => CsTeam.Terrorist,
            "terrorist" => CsTeam.Terrorist,
            "ct" => CsTeam.CounterTerrorist,
            "counterterrorist" => CsTeam.CounterTerrorist,
            _ => CsTeam.None,
        };
    }

    public static T? ToEnum<T>(string str)
    {
        var enumType = typeof(T);
        try
        {
            foreach (var name in Enum.GetNames(enumType))
            {
                // Log.Write($"Enum name {name}");
                var enumMemberAttribute =
                    ((EnumMemberAttribute[]) enumType.GetField(name)!.GetCustomAttributes(typeof(EnumMemberAttribute),
                        true)).SingleOrDefault();
                // Log.Write($"Custom attribute: {enumMemberAttribute?.Value}");
                if (enumMemberAttribute?.Value == str)
                {
                    return (T) Enum.Parse(enumType, name);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"Exception parsing enum {e.Message}");
        }

        return default;
    }

    public static void SetupMySql(string connectionString, DbContextOptionsBuilder optionsBuilder)
    {
        var version = ServerVersion.AutoDetect(connectionString);
        optionsBuilder.UseMySql(connectionString, version);
    }

    public static void SetupSqlite(string connectionString, DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(connectionString);
    }
}