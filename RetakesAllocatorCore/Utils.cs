using CounterStrikeSharp.API.Modules.Utils;

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
}
