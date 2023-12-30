namespace Cs2WeaponAllocator;

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
}
