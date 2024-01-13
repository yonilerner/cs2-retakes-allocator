using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RetakesAllocatorCore.Db;

using WeaponPreferencesType = Dictionary<
    CsTeam,
    Dictionary<RoundType, CsItem>
>;

public class UserSetting
{
    [Key]
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong UserId { get; set; }

    [Column(TypeName = "TEXT"), MaxLength(10000)]
    public WeaponPreferencesType WeaponPreferences { get; set; } = new();

    [Column(TypeName = "TEXT"), MaxLength(100)]
    public CsItem? SniperPreference { get; set; } = null;

    public void SetWeaponPreference(CsTeam team, RoundType roundType, CsItem? weapon)
    {
        // Log.Write($"Setting preference for {UserId} {team} {roundType} {weapon}");
        if (!WeaponPreferences.TryGetValue(team, out var roundPreferences))
        {
            roundPreferences = new();
            WeaponPreferences.Add(team, roundPreferences);
        }

        if (weapon is not null)
        {
            roundPreferences[roundType] = (CsItem) weapon;
        }
        else
        {
            roundPreferences.Remove(roundType);
        }
    }

    public CsItem? GetWeaponPreference(CsTeam team, RoundType roundType)
    {
        if (WeaponPreferences.TryGetValue(team, out var roundPreferences))
        {
            if (roundPreferences.TryGetValue(roundType, out var weapon))
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
        var parseResult = JsonSerializer.Deserialize<WeaponPreferencesType>(value);
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
