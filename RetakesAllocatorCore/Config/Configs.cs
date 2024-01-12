using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace RetakesAllocatorCore.Config;

public static class Configs
{
    private static readonly string ConfigDirectoryName = "config";
    private static readonly string ConfigFileName = "config.json";

    private static string? _configFilePath;
    private static ConfigData? _configData;

    private static readonly JsonSerializerOptions SerializationOptions = new()
    {
        Converters =
        {
            new JsonStringEnumConverter()
        },
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static ConfigData GetConfigData()
    {
        if (_configData is null)
        {
            throw new Exception("Config not yet loaded.");
        }

        return _configData;
    }

    public static ConfigData Load(string modulePath, bool saveAfterLoad = false)
    {
        var configFileDirectory = Path.Combine(modulePath, ConfigDirectoryName);
        Directory.CreateDirectory(configFileDirectory);

        _configFilePath = Path.Combine(configFileDirectory, ConfigFileName);
        if (File.Exists(_configFilePath))
        {
            _configData =
                JsonSerializer.Deserialize<ConfigData>(File.ReadAllText(_configFilePath), SerializationOptions);
        }
        else
        {
            _configData = new ConfigData();
        }

        if (_configData is null)
        {
            throw new Exception("Failed to load configs.");
        }

        if (saveAfterLoad)
        {
            SaveConfigData(_configData);
        }

        _configData.Validate();

        return _configData;
    }

    public static ConfigData OverrideConfigDataForTests(
        ConfigData configData
    )
    {
        _configData = configData;
        return _configData;
    }

    private static void SaveConfigData(ConfigData configData)
    {
        if (_configFilePath is null)
        {
            throw new Exception("Config not yet loaded.");
        }

        File.WriteAllText(_configFilePath, JsonSerializer.Serialize(configData, SerializationOptions));
    }
}

public enum WeaponSelectionType
{
    PlayerChoice,
    Random,
    Default,
}

public record ConfigData
{
    public List<CsItem> UsableWeapons { get; set; } = WeaponHelpers.GetAllWeapons();

    public List<WeaponSelectionType> AllowedWeaponSelectionTypes { get; set; } =
        Enum.GetValues<WeaponSelectionType>().ToList();

    public Dictionary<RoundType, int> RoundTypePercentages { get; set; } = new()
    {
        {RoundType.Pistol, 15},
        {RoundType.HalfBuy, 25},
        {RoundType.FullBuy, 60},
    };

    public bool MigrateOnStartup { get; set; } = true;
    public bool AllowAllocationAfterFreezeTime { get; set; } = false;
    public bool EnableRoundTypeAnnouncement { get; set; } = true;

    public void Validate()
    {
        if (RoundTypePercentages.Values.Sum() != 100)
        {
            throw new Exception("'RoundTypePercentages' values must add up to 100");
        }
    }

    public double GetRoundTypePercentage(RoundType roundType)
    {
        return Math.Round(RoundTypePercentages[roundType] / 100.0, 2);
    }

    public bool CanPlayersSelectWeapons()
    {
        return AllowedWeaponSelectionTypes.Contains(WeaponSelectionType.PlayerChoice);
    }

    public bool CanAssignRandomWeapons()
    {
        return AllowedWeaponSelectionTypes.Contains(WeaponSelectionType.Random);
    }

    public bool CanAssignDefaultWeapons()
    {
        return AllowedWeaponSelectionTypes.Contains(WeaponSelectionType.Default);
    }
}
