using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using RetakesAllocatorCore;

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
    
    public static void RemoveArmor(CCSPlayerController player)
    {
        if (!PlayerIsValid(player) || player.PlayerPawn.Value?.ItemServices is null)
        {
            return;
        }
        
        var itemServices = new CCSPlayer_ItemServices(player.PlayerPawn.Value.ItemServices.Handle);
        itemServices.HasHelmet = false;
        itemServices.HasHeavyArmor = false;
    }
    
    public static void RemoveWeapons(CCSPlayerController player)
    {
        if (!PlayerIsValid(player) || player.PlayerPawn.Value?.WeaponServices is null)
        {
            return;
        }
        
        foreach(var weapon in player.PlayerPawn.Value.WeaponServices.MyWeapons)
        {
            if (weapon is not { IsValid: true, Value.IsValid: true })
            {
                continue;
            }
            
            Log.Write($"Removing weapon {weapon.Value.DesignerName}");
        
            player.PlayerPawn.Value.RemovePlayerItem(weapon.Value);
            weapon.Value.Remove();
        }
    }
}
