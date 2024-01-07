using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RetakesAllocatorCore.Config;

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

    public void SetWeaponPreference(CsTeam team, RoundType roundType, CsItem? weapon)
    {
        Log.Write($"Setting preference for {UserId} {team} {roundType} {weapon}");
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

    public ICollection<CsItem> GetWeaponsForTeamAndRound(CsTeam team, RoundType roundType)
    {
        if (!Configs.GetConfigData().CanPlayersSelectWeapons())
        {
            if (Configs.GetConfigData().CanAssignRandomWeapons())
            {
                return WeaponHelpers.GetRandomWeaponsForRoundType(roundType, team);
            }

            if (Configs.GetConfigData().CanAssignDefaultWeapons())
            {
                return WeaponHelpers.GetDefaultWeaponsForRoundType(roundType, team);
            }
        }

        List<CsItem> weapons = new()
        {
            GetWeaponPreference(team, roundType) ?? WeaponHelpers.GetRandomWeaponForRoundType(roundType, team)
        };
        // Log.Write($"First weapon!!!: {firstWeapon}");

        if (roundType != RoundType.Pistol)
        {
            weapons.AddRange(GetWeaponsForTeamAndRound(team, RoundType.Pistol));
            // Log.Write($"Not pistol {roundType}: {string.Join(",", weapons)}");
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
