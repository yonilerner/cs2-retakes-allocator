using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using RetakesAllocatorCore.Config;

namespace RetakesAllocatorCore;

public static class Log
{
    private static void Write(string message, LogLevel level)
    {
        var currentLevel = Configs.IsLoaded() ? Configs.GetConfigData().LogLevel : LogLevel.Error;
        if (currentLevel == LogLevel.None || level < currentLevel)
        {
            return;
        }
        
        Console.ResetColor();
        switch (level)
        {
            case LogLevel.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case LogLevel.Critical:
            case LogLevel.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                break;
            default:
                // Looks red??
                // Console.ForegroundColor = ConsoleColor.White;
                break;
        }

        Console.WriteLine($"{PluginInfo.LogPrefix}{message}");
        Console.ResetColor();
    }

    public static void Trace(string message)
    {
        Write(message, LogLevel.Trace);
    }

    public static void Debug(string message)
    {
        Write(message, LogLevel.Debug);
    }

    public static void Info(string message)
    {
        Write(message, LogLevel.Information);
    }

    public static void Warn(string message)
    {
        Write(message, LogLevel.Warning);
    }

    public static void Error(string message)
    {
        Write(message, LogLevel.Error);
    }
}
