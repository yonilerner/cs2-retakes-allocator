using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace RetakesAllocator;

public class Helpers
{
    public static bool PlayerIsValid(CCSPlayerController? player)
    {
        return player != null && player.IsValid && player.AuthorizedSteamID is not null;
    }

    public static ICollection<string> CommandInfoToArgList(CommandInfo commandInfo, bool includeFirst = false)
    {
        var result = new List<string>();

        for (var i = includeFirst ? 0 : 1; i < commandInfo.ArgCount; i++)
        {
            result.Add(commandInfo.GetArg(i));
        }

        return result;
    }

    public static ulong GetSteamId(CCSPlayerController? player)
    {
        if (!PlayerIsValid(player))
        {
            return 0;
        }

        return player?.AuthorizedSteamID?.SteamId64 ?? 0;
    }
}
