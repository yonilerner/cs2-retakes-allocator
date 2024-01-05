using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RetakesAllocator.db;

using WeaponPreferencesType = Dictionary<
    CsTeam,
    Dictionary<RoundType, CsItem>
>;

public class UserSetting
{
    [Key]
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int UserId { get; set; }

    [Column(TypeName = "TEXT"), MaxLength(10000)]
    public WeaponPreferencesType WeaponPreferences { get; set; } = new();

    public void SetWeaponPreference(CsTeam team, RoundType roundType, CsItem? weapon)
    {
        if (!WeaponPreferences.TryGetValue(team, out var roundPreferences))
        {
            roundPreferences = new();
            WeaponPreferences.Add(team, roundPreferences);
        }

        if (weapon is not null)
        {
            roundPreferences.Add(roundType, (CsItem) weapon);
        }
        else
        {
            roundPreferences.Remove(roundType);
        }
    }

    public ICollection<CsItem> GetWeaponsForTeamAndRound(CsTeam team, RoundType roundType)
    {
        List<CsItem> weapons = new();
        if (WeaponPreferences.TryGetValue(team, out var roundPreferences))
        {
            if (roundPreferences.TryGetValue(roundType, out var weapon))
            {
                weapons.Add(weapon);
            }
        }

        if (weapons.Count == 0)
        {
            weapons.Add(WeaponHelpers.GetRandomWeaponForRoundType(roundType, team));
        }

        if (roundType != RoundType.Pistol)
        {
            weapons.AddRange(GetWeaponsForTeamAndRound(team, RoundType.Pistol));
        }

        return weapons;
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
        if (value == null)
        {
            return "";
        }

        try
        {
            return JsonSerializer.Serialize(value);
        }
        catch
        {
            return "";
        }
    }

    public static WeaponPreferencesType WeaponPreferenceDeserialize(string value)
    {
        try
        {
            var parseResult = JsonSerializer.Deserialize<WeaponPreferencesType>(value);
            return parseResult ?? new WeaponPreferencesType();
        }
        catch
        {
            return new WeaponPreferencesType();
        }
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
