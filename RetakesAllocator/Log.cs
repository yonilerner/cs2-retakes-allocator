namespace RetakesAllocator;

public static class Log
{
    public static void Write(string message)
    {
        Console.WriteLine($"[{nameof(RetakesAllocator)}] {message}");
    }
}