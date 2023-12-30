namespace cs_weapon_allocator;

public class Log
{
    public static void Write(string message)
    {
        Console.WriteLine($"[WeaponAllocator] {message}");
    }
}