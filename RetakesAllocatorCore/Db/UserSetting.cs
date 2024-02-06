using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RetakesAllocatorCore.Db;

using WeaponPreferencesType = Dictionary<
    CsTeam,
    Dictionary<WeaponAllocationType, CsItem>
>;

public class UserSetting
{
    [Key]
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column(TypeName = "bigint")]
    public ulong UserId { get; set; }

    [Column(TypeName = "text"), MaxLength(10000)]
    public WeaponPreferencesType WeaponPreferences { get; set; } = new();

    public static void Configure(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<WeaponPreferencesType>()
            .HaveConversion<WeaponPreferencesConverter, WeaponPreferencesComparer>();
    }

    public void SetWeaponPreference(CsTeam team, WeaponAllocationType weaponAllocationType, CsItem? weapon)
    {
        // Log.Write($"Setting preference for {UserId} {team} {weaponAllocationType} {weapon}");
        if (!WeaponPreferences.TryGetValue(team, out var allocationPreference))
        {
            allocationPreference = new();
            WeaponPreferences.Add(team, allocationPreference);
        }

        if (weapon is not null)
        {
            allocationPreference[weaponAllocationType] = (CsItem) weapon;
        }
        else
        {
            allocationPreference.Remove(weaponAllocationType);
        }
    }

    public CsItem? GetWeaponPreference(CsTeam team, WeaponAllocationType weaponAllocationType)
    {
        if (WeaponPreferences.TryGetValue(team, out var allocationPreference))
        {
            if (allocationPreference.TryGetValue(weaponAllocationType, out var weapon))
            {
                return weapon;
            }
        }

        return null;
    }
}

public class CsItemConverter : ValueConverter<CsItem?, string>
{
    public CsItemConverter() : base(
        v => CsItemSerializer(v),
        s => CsItemDeserializer(s)
    )
    {
    }

    public static string CsItemSerializer(CsItem? item)
    {
        return JsonSerializer.Serialize(item);
    }

    public static CsItem? CsItemDeserializer(string? str)
    {
        if (str is null)
        {
            return null;
        }

        return JsonSerializer.Deserialize<CsItem>(str);
    }
}

public class WeaponPreferencesConverter : ValueConverter<WeaponPreferencesType, string>
{
    public WeaponPreferencesConverter() : base(
        v => WeaponPreferenceSerialize(v),
        s => WeaponPreferenceDeserialize(s)
    )
    {
    }

    public static string WeaponPreferenceSerialize(WeaponPreferencesType? value)
    {
        if (value is null)
        {
            return "";
        }

        return JsonSerializer.Serialize(value);
    }

    public static WeaponPreferencesType WeaponPreferenceDeserialize(string value)
    {
        WeaponPreferencesType? parseResult = null;
        try { 
            parseResult = JsonSerializer.Deserialize<WeaponPreferencesType>(value);
        } catch (Exception e)
        {
            Log.Error($"Failed to deserialize weapon preferences: {e.Message}");
        }
        
        return parseResult ?? new WeaponPreferencesType();
    }
}

public class WeaponPreferencesComparer : ValueComparer<WeaponPreferencesType>
{
    public WeaponPreferencesComparer() : base(
        (a, b) =>
            WeaponPreferencesConverter.WeaponPreferenceSerialize(a).Equals(
                WeaponPreferencesConverter.WeaponPreferenceSerialize(b)
            ),
        (v) => WeaponPreferencesConverter.WeaponPreferenceSerialize(v).GetHashCode()
    )
    {
    }
}
