using static RetakesAllocatorCore.PluginInfo;

namespace RetakesAllocatorCore;

public static class Log
{
    public static void Write(string message)
    {
        Console.WriteLine($"{LogPrefix}{message}");
    }
}
