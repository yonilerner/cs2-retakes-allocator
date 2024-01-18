using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;

namespace RetakesAllocator;

public static class Helpers
{
    public static bool PlayerIsValid(CCSPlayerController? player)
    {
        return player is not null && player.IsValid && player.AuthorizedSteamID is not null;
    }

    public static void WriteNewlineDelimited(string message, Action<string> writer)
    {
        foreach (var line in message.Split("\n"))
        {
            writer($"{PluginInfo.MessagePrefix}{line}");
        }
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

    public static CsTeam GetTeam(CCSPlayerController player)
    {
        return player.Team;
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

    public static CsItem? GetPlayerWeaponItem(CCSPlayerController player, Func<CsItem, bool> pred)
    {
        if (!PlayerIsValid(player) || player.PlayerPawn.Value?.WeaponServices is null)
        {
            return null;
        }

        foreach (var weapon in player.PlayerPawn.Value.WeaponServices.MyWeapons)
        {
            if (weapon is not {IsValid: true, Value.IsValid: true})
            {
                continue;
            }

            CsItem? item = Utils.ToEnum<CsItem>(weapon.Value.DesignerName);
            if (item is not null && pred(item.Value))
            {
                return item;
            }
        }

        return null;
    }

    public static CHandle<CBasePlayerWeapon>? GetPlayerWeapon(CCSPlayerController player,
        Func<CBasePlayerWeapon, CsItem, bool> pred)
    {
        if (!PlayerIsValid(player) || player.PlayerPawn.Value?.WeaponServices is null)
        {
            return null;
        }

        foreach (var weapon in player.PlayerPawn.Value.WeaponServices.MyWeapons)
        {
            if (weapon is not {IsValid: true, Value.IsValid: true})
            {
                continue;
            }

            CsItem? item = Utils.ToEnum<CsItem>(weapon.Value.DesignerName);
            if (item is not null && pred(weapon.Value, item.Value))
            {
                return weapon;
            }
        }

        return null;
    }

    public static bool RemoveWeapons(CCSPlayerController player, Func<CsItem, bool>? where = null)
    {
        if (!PlayerIsValid(player) || player.PlayerPawn.Value?.WeaponServices is null)
        {
            return false;
        }

        var removed = false;

        foreach (var weapon in player.PlayerPawn.Value.WeaponServices.MyWeapons)
        {
            // Log.Write($"want to remove wep {weapon.Value?.DesignerName} {weapon.IsValid}");
            if (weapon is not {IsValid: true, Value.IsValid: true})
            {
                continue;
            }

            CsItem? item = Utils.ToEnum<CsItem>(weapon.Value.DesignerName);
            // Log.Write($"item to remove: {item}");

            if (
                where is not null &&
                (item is null || !where(item.Value))
            )
            {
                continue;
            }

            // Log.Write($"Removing weapon {weapon.Value.DesignerName} {weapon.IsValid}");

            player.PlayerPawn.Value.RemovePlayerItem(weapon.Value);
            weapon.Value.Remove();

            removed = true;
        }

        return removed;
    }

    private static CCSGameRules GetGameRules()
    {
        var gameRulesEntities = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
        var gameRules = gameRulesEntities.First().GameRules;

        if (gameRules is null)
        {
            const string message = "Game rules were null.";
            Log.Write(message);
            throw new Exception(message);
        }

        return gameRules;
    }

    public static bool IsWarmup()
    {
        return GetGameRules().WarmupPeriod;
    }

    public static bool IsWeaponAllocationAllowed()
    {
        return WeaponHelpers.IsWeaponAllocationAllowed(GetGameRules().FreezePeriod);
    }

    public static double GetVectorDistance(Vector v1, Vector v2)
    {
        var dx = v1.X - v2.X;
        var dy = v1.Y - v2.Y;

        return Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));
    }

    public static int GetNumPlayersOnTeam()
    {
        return Utilities.GetPlayers()
            .Where(player => player.IsValid)
            .Where(player => player.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist).ToList()
            .Count;
    }
}
