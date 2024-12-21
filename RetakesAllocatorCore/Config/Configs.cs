using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace RetakesAllocatorCore.Config;

public static class Configs
{
    public static class Shared
    {
        public static string? Module { get; set; }
    }
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

    public static bool IsLoaded()
    {
        return _configData is not null;
    }

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
        configData.Validate();
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

    public static string? StringifyConfig(string? configName)
    {
        var configData = GetConfigData();
        if (configName is null)
        {
            return JsonSerializer.Serialize(configData, SerializationOptions);
        }
        var property = configData.GetType().GetProperty(configName);
        if (property is null)
        {
            return null;
        }
        return JsonSerializer.Serialize(property.GetValue(configData), SerializationOptions);
    }
}

public enum WeaponSelectionType
{
    PlayerChoice,
    Random,
    Default,
}

public enum DatabaseProvider
{
    Sqlite,
    MySql,
}

public enum RoundTypeSelectionOption
{
    Random,
    RandomFixedCounts,
    ManualOrdering,
}

public record RoundTypeManualOrderingItem(RoundType Type, int Count);

public record ConfigData
{
    public List<CsItem> UsableWeapons { get; set; } = WeaponHelpers.AllWeapons;

    public List<WeaponSelectionType> AllowedWeaponSelectionTypes { get; set; } =
        Enum.GetValues<WeaponSelectionType>().ToList();

    public Dictionary<CsTeam, Dictionary<WeaponAllocationType, CsItem>> DefaultWeapons { get; set; } =
        WeaponHelpers.DefaultWeaponsByTeamAndAllocationType;

    public Dictionary<
        string,
        Dictionary<
            CsTeam,
            Dictionary<CsItem, int>
        >
    > MaxNades { get; set; } = new()
    {
        {
            NadeHelpers.GlobalSettingName, new()
            {
                {
                    CsTeam.Terrorist, new()
                    {
                        {CsItem.Flashbang, 2},
                        {CsItem.Smoke, 1},
                        {CsItem.Molotov, 1},
                        {CsItem.HE, 1},
                    }
                },
                {
                    CsTeam.CounterTerrorist, new()
                    {
                        {CsItem.Flashbang, 2},
                        {CsItem.Smoke, 1},
                        {CsItem.Incendiary, 2},
                        {CsItem.HE, 1},
                    }
                },
            }
        }
    };

    public Dictionary<
        string,
        Dictionary<
            CsTeam,
            Dictionary<RoundType, MaxTeamNadesSetting>
        >
    > MaxTeamNades { get; set; } = new()
    {
        {
            NadeHelpers.GlobalSettingName, new()
            {
                {
                    CsTeam.Terrorist, new()
                    {
                        {RoundType.Pistol, MaxTeamNadesSetting.AverageOnePerPlayer},
                        {RoundType.HalfBuy, MaxTeamNadesSetting.AverageOnePointFivePerPlayer},
                        {RoundType.FullBuy, MaxTeamNadesSetting.AverageOnePointFivePerPlayer},
                    }
                },
                {
                    CsTeam.CounterTerrorist, new()
                    {
                        {RoundType.Pistol, MaxTeamNadesSetting.AverageOnePerPlayer},
                        {RoundType.HalfBuy, MaxTeamNadesSetting.AverageOnePointFivePerPlayer},
                        {RoundType.FullBuy, MaxTeamNadesSetting.AverageOnePointFivePerPlayer},
                    }
                },
            }
        }
    };

    public RoundTypeSelectionOption RoundTypeSelection { get; set; } = RoundTypeSelectionOption.Random;

    public Dictionary<RoundType, int> RoundTypePercentages { get; set; } = new()
    {
        {RoundType.Pistol, 15},
        {RoundType.HalfBuy, 25},
        {RoundType.FullBuy, 60},
    };

    public Dictionary<RoundType, int> RoundTypeRandomFixedCounts { get; set; } = new()
    {
        {RoundType.Pistol, 5},
        {RoundType.HalfBuy, 10},
        {RoundType.FullBuy, 15},
    };

    public List<RoundTypeManualOrderingItem> RoundTypeManualOrdering { get; set; } = new()
    {
        new RoundTypeManualOrderingItem(RoundType.Pistol, 5),
        new RoundTypeManualOrderingItem(RoundType.HalfBuy, 10),
        new RoundTypeManualOrderingItem(RoundType.FullBuy, 15),
    };

    public bool MigrateOnStartup { get; set; } = true;
    public bool ResetStateOnGameRestart { get; set; } = true;
    public bool AllowAllocationAfterFreezeTime { get; set; } = true;
    public bool UseOnTickFeatures { get; set; } = true;
    public bool CapabilityWeaponPaints { get; set; } = true;
    public bool EnableRoundTypeAnnouncement { get; set; } = true;
    public bool EnableRoundTypeAnnouncementCenter { get; set; } = false;
    public bool EnableBombSiteAnnouncementCenter { get; set; } = false;
    public bool BombSiteAnnouncementCenterToCTOnly { get; set; } = false;
    public bool DisableDefaultBombPlantedCenterMessage { get; set; } = false;
    public bool ForceCloseBombSiteAnnouncementCenterOnPlant { get; set; } = true;
    public float BombSiteAnnouncementCenterDelay { get; set; } = 1.0f;
    public float BombSiteAnnouncementCenterShowTimer { get; set; } = 5.0f;
    public bool EnableBombSiteAnnouncementChat { get; set; } = false;
    public bool EnableNextRoundTypeVoting { get; set; } = false;
    public int NumberOfExtraVipChancesForPreferredWeapon { get; set; } = 1;
    public bool AllowPreferredWeaponForEveryone { get; set; } = false;

    public double ChanceForPreferredWeapon { get; set; } = 100;

    public Dictionary<CsTeam, int> MaxPreferredWeaponsPerTeam { get; set; } = new()
    {
        {CsTeam.Terrorist, 1},
        {CsTeam.CounterTerrorist, 1},
    };

    public Dictionary<CsTeam, int> MinPlayersPerTeamForPreferredWeapon { get; set; } = new()
    {
        {CsTeam.Terrorist, 1},
        {CsTeam.CounterTerrorist, 1},
    };

    public bool EnableCanAcquireHook { get; set; } = true;

    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    public string ChatMessagePluginName { get; set; } = "Retakes";
    public string? ChatMessagePluginPrefix { get; set; }

    public string InGameGunMenuCenterCommands { get; set; } =
        "gunsmenu,gunmenu,!gunmenu,!gunsmenu,!menugun,!menuguns,/gunsmenu,/gunmenu";

    public string InGameGunMenuChatCommands { get; set; } = "guns,!guns,/guns";
    public ZeusPreference ZeusPreference { get; set; } = ZeusPreference.Never;

    public DatabaseProvider DatabaseProvider { get; set; } = DatabaseProvider.Sqlite;
    public string DatabaseConnectionString { get; set; } = "Data Source=data.db; Pooling=False";
    public bool AutoUpdateSignatures { get; set; } = true;

    public IList<string> Validate()
    {
        if (RoundTypePercentages.Values.Sum() != 100)
        {
            throw new Exception("'RoundTypePercentages' values must add up to 100");
        }

        var warnings = new List<string>();
        warnings.AddRange(ValidateDefaultWeapons(CsTeam.Terrorist));
        warnings.AddRange(ValidateDefaultWeapons(CsTeam.CounterTerrorist));

        foreach (var warning in warnings)
        {
            Log.Warn($"[CONFIG WARNING] {warning}");
        }

        return warnings;
    }

    private ICollection<string> ValidateDefaultWeapons(CsTeam team)
    {
        var warnings = new List<string>();
        if (!DefaultWeapons.TryGetValue(team, out var defaultWeapons))
        {
            warnings.Add($"Missing {team} in DefaultWeapons config.");
            return warnings;
        }

        if (defaultWeapons.ContainsKey(WeaponAllocationType.Preferred))
        {
            throw new Exception(
                $"Preferred is not a valid default weapon allocation type " +
                $"for config DefaultWeapons.{team}.");
        }

        var allocationTypes = WeaponHelpers.WeaponAllocationTypes;
        allocationTypes.Remove(WeaponAllocationType.Preferred);

        foreach (var allocationType in allocationTypes)
        {
            if (!defaultWeapons.TryGetValue(allocationType, out var w))
            {
                warnings.Add($"Missing {allocationType} in DefaultWeapons.{team} config.");
                continue;
            }

            if (!WeaponHelpers.IsWeapon(w))
            {
                throw new Exception($"{w} is not a valid weapon in config DefaultWeapons.{team}.{allocationType}.");
            }

            if (!UsableWeapons.Contains(w))
            {
                warnings.Add(
                    $"{w} in the DefaultWeapons.{team}.{allocationType} config " +
                    $"is not in the UsableWeapons list.");
            }
        }

        return warnings;
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
