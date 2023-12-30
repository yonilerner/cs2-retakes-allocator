using CounterStrikeSharp.API.Core;

namespace cs_weapon_allocator;

public class Utils
{
    public static T? Choice<T>(IList<T> items)
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
}