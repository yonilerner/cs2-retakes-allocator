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

    private static readonly JsonSerializerOptions SerializationOptions = new JsonSerializerOptions
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

    public static ConfigData Load(string modulePath, bool saveDefaults = true)
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
            _configData = GetDefaultData();
            if (saveDefaults)
            {
                SaveConfigData(_configData);
            }
        }

        if (_configData is null)
        {
            throw new Exception("Failed to load configs.");
        }
        
        _configData.Validate();

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

    private static ConfigData GetDefaultData()
    {
        return new ConfigData {
            PlayerSelectableWeapons = WeaponHelpers.GetAllWeapons(),
            AllowedWeaponSelectionTypes = Enum.GetValues<WeaponSelectionType>().ToList(),
            RoundTypePercentages = new()
            {
                {RoundType.Pistol, 15},
                {RoundType.HalfBuy, 25},
                {RoundType.FullBuy, 60},
            },
            MigrateOnStartup = false
        };
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
    public required List<CsItem> PlayerSelectableWeapons {get; set; }
    public required List<WeaponSelectionType> AllowedWeaponSelectionTypes {get; set; }
    public required Dictionary<RoundType, int> RoundTypePercentages {get; set; }
    public required bool MigrateOnStartup {get; set; }
    public void Validate()
    {
        if (RoundTypePercentages.Values.Sum() != 100)
        {
            throw new Exception("'RoundTypePercentages' values must add up to 100");
        }
    }

    public double GetRoundTypePercentage(RoundType roundType)
    {
        return RoundTypePercentages[roundType] / 100;
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
