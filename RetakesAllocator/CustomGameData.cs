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
            "CCSPlayer_ItemServices_CanAcquire",
            new()
            {
                {OSPlatform.Windows, @"\x48\x8B\xC4\x44\x89\x40\x18\x48\x89\x50\x??\x48\x89\x48"},
                {OSPlatform.Linux, @"\x55\x48\x89\xE5\x41\x57\x41\x56\x48\x8D\x45\x??\x41\x55\x41\x54\x53\x48\x89\xCB\x48\x83\xEC"},
            }
        },
        {
            "GetCSWeaponDataFromKey",
            new()
            {
                {OSPlatform.Windows, @"\x48\x89\x5C\x24\x??\x48\x89\x74\x24\x??\x57\x48\x83\xEC\x??\x48\x8B\xFA\x8B\xF1\x48\x85\xD2\x0F\x84"},
                {OSPlatform.Linux, @"\x55\x48\x89\xE5\x41\x57\x41\x56\x41\x89\xFE\x41\x55\x41\x54\x45"},
			}
        },
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
                    @"\x55\x48\x89\xE5\x41\x57\x41\x56\x41\x55\x41\x54\x53\x48\x83\xEC\x18\x48\x89\x7D\xC8\x48\x85\xF6\x74"
                },
            }
        }
    };

    private readonly MemoryFunctionVoid<IntPtr, string, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr> GiveNamedItem2;

    public readonly
        MemoryFunctionWithReturn<CCSPlayer_ItemServices, CEconItemView, AcquireMethod, NativeObject, AcquireResult>
        CCSPlayer_ItemServices_CanAcquireFunc;

    public readonly MemoryFunctionWithReturn<int, string, CCSWeaponBaseVData> GetCSWeaponDataFromKeyFunc;

    public CustomGameData()
    {
        GiveNamedItem2 = new(GetCustomGameDataKey("GiveNamedItem2"));
        CCSPlayer_ItemServices_CanAcquireFunc = new(GetCustomGameDataKey("CCSPlayer_ItemServices_CanAcquire"));
        GetCSWeaponDataFromKeyFunc = new(GetCustomGameDataKey("GetCSWeaponDataFromKey"));
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
