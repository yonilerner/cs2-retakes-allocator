using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore.Config;

namespace RetakesAllocatorCore;

public static class PluginInfo
{
    public const string Version = "1.2.17";
    
    public static readonly string LogPrefix = $"[RetakesAllocator {Version}] ";
    public static string MessagePrefix
    {
        get
        {
            var name = Configs.IsLoaded() ? Configs.GetConfigData().ChatMessagePluginName : "Retakes";
            return $"[{ChatColors.Green}{name}{ChatColors.White}] ";
        }
    }
}
