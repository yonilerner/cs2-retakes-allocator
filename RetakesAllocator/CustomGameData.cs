using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System.Text.Json;

namespace RetakesAllocator;

public class CustomGameData
{
    public static Dictionary<string, Dictionary<OSPlatform, string>> _customGameData = new();
    private readonly MemoryFunctionVoid<IntPtr, string, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr> GiveNamedItem2;
    public readonly MemoryFunctionWithReturn<CCSPlayer_ItemServices, CEconItemView, AcquireMethod, NativeObject, AcquireResult> CCSPlayer_ItemServices_CanAcquireFunc;

    public readonly MemoryFunctionWithReturn<int, string, CCSWeaponBaseVData> GetCSWeaponDataFromKeyFunc;

    public CustomGameData()
    {
        LoadCustomGameDataFromJson();
        
        GiveNamedItem2 = new(GetCustomGameDataKey("GiveNamedItem2"));
        CCSPlayer_ItemServices_CanAcquireFunc = new(GetCustomGameDataKey("CCSPlayer_ItemServices_CanAcquire"));
        GetCSWeaponDataFromKeyFunc = new(GetCustomGameDataKey("GetCSWeaponDataFromKey"));
    }
    public void LoadCustomGameDataFromJson()
    {
        string jsonFilePath = $"{Configs.Shared.Module}/../../plugins/RetakesAllocator/gamedata/RetakesAllocator_gamedata.json";
        if (!File.Exists(jsonFilePath))
        {
            Log.Debug($"JSON file does not exist at path: {jsonFilePath}. Returning without loading custom game data.");
            return;
        }
        
        try
        {
            var jsonData = File.ReadAllText(jsonFilePath);
            var jsonDocument = JsonDocument.Parse(jsonData);
            
            foreach (var element in jsonDocument.RootElement.EnumerateObject())
            {
                string key = element.Name;

                var platformData = new Dictionary<OSPlatform, string>();

                if (element.Value.TryGetProperty("signatures", out var signatures))
                {
                    if (signatures.TryGetProperty("windows", out var windows))
                    {
                        platformData[OSPlatform.Windows] = windows.GetString()!;
                    }

                    if (signatures.TryGetProperty("linux", out var linux))
                    {
                        platformData[OSPlatform.Linux] = linux.GetString()!;
                    }
                }
                _customGameData[key] = platformData;
            }
        }
        catch (Exception ex)
        {
            Log.Debug($"Error loading custom game data: {ex.Message}");
        }
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
