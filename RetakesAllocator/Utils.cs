using System.Diagnostics;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesAllocator;

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

    public static bool PlayerIsValid(CCSPlayerController? player)
    {
        return player != null && player.IsValid;
    }

    public static RoundType? ParseRoundType(string roundType)
    {
        return roundType.ToUpper() switch
        {
            "F" => RoundType.FullBuy,
            "H" => RoundType.HalfBuy,
            "P" => RoundType.Pistol,
            _ => null,
        };
    }
}
