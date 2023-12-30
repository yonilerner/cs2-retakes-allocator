namespace Cs2WeaponAllocator;

public static class Log
{
    public static void Write(string message)
    {
        Console.WriteLine($"[{nameof(Cs2WeaponAllocator)}] {message}");
    }
}
