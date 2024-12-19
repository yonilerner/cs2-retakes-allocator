using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace RetakesAllocator;

public class CustomGameData
{
    private static Dictionary<string, Dictionary<OSPlatform, string>> _customGameData = new()
    {
        // Thank you to @Whaliin https://github.com/CS2Plugins/WeaponRestrict/blob/main/WeaponRestrict.json
        {
            "GiveNamedItem2",
            new()
            {
                {
                    OSPlatform.Windows,
                    @"\x48\x83\xEC\x38\x48\xC7\x44\x24\x28\x00\x00\x00\x00\x45\x33\xC9\x45\x33\xC0\xC6\x44\x24\x20\x00\xE8\x2A\x2A\x2A\x2A\x48\x85"
                },
                {
                    OSPlatform.Linux,
                    @"\x55\x48\x89\xE5\x41\x57\x41\x56\x4D\x89\xC6\x41\x55\x49\x89\xF5\x41\x54\x49\x89\xD4"
                },
            }
        }
    };

    private readonly MemoryFunctionVoid<IntPtr, string, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr> GiveNamedItem2;

    public readonly
        MemoryFunctionWithReturn<CCSPlayer_ItemServices, CEconItemView, AcquireMethod, NativeObject, AcquireResult>
        CCSPlayer_ItemServices_CanAcquireFunc = new(GameData.GetSignature("CCSPlayer_ItemServices_CanAcquire"));

    public readonly MemoryFunctionWithReturn<int, string, CCSWeaponBaseVData> GetCSWeaponDataFromKeyFunc = new(GameData.GetSignature("GetCSWeaponDataFromKey"));

    public CustomGameData()
    {
        GiveNamedItem2 = new(GetCustomGameDataKey("GiveNamedItem2"));
    }

    private string GetCustomGameDataKey(string key)
    {
        if (!_customGameData.TryGetValue(key, out var customGameData))
        {
            throw new Exception($"Invalid key {key}");
        }

        OSPlatform platform;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            platform = OSPlatform.Linux;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            platform = OSPlatform.Windows;
        }
        else
        {
            throw new Exception("Unsupported platform");
        }

        return customGameData.TryGetValue(platform, out var customData)
            ? customData
            : throw new Exception($"Missing custom data for {key} on {platform}");
    }

    public void PlayerGiveNamedItem(CCSPlayerController player, string item)
    {
        if (!player.PlayerPawn.IsValid) return;
        if (player.PlayerPawn.Value == null) return;
        if (!player.PlayerPawn.Value.IsValid) return;
        if (player.PlayerPawn.Value.ItemServices == null) return;

        // Log.Debug("Using custom function for GiveNamedItem2");
        GiveNamedItem2.Invoke(player.PlayerPawn.Value.ItemServices.Handle, item, 0, 0, 0, 0, 0, 0);
    }
}

// Possible results for CSPlayer::CanAcquire
public enum AcquireResult
{
    Allowed = 0,
    InvalidItem,
    AlreadyOwned,
    AlreadyPurchased,
    ReachedGrenadeTypeLimit,
    ReachedGrenadeTotalLimit,
    NotAllowedByTeam,
    NotAllowedByMap,
    NotAllowedByMode,
    NotAllowedForPurchase,
    NotAllowedByProhibition,
};

// Possible results for CSPlayer::CanAcquire
public enum AcquireMethod
{
    PickUp = 0,
    Buy,
};
