namespace cs_weapon_allocator;

public static class Log
{
    public static void Write(string message)
    {
        Console.WriteLine($"[WeaponAllocator] {message}");
    }
}