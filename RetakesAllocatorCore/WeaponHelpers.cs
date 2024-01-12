using System.Collections;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;

namespace RetakesAllocatorCore;

public static class WeaponHelpers
{
    private static readonly ICollection<CsItem> _sharedPistols = new HashSet<CsItem>
    {
        CsItem.P250,
        CsItem.Deagle,
        CsItem.CZ,
        CsItem.Dualies,
        CsItem.R8,
    };

    private static readonly ICollection<CsItem> _tPistols = new HashSet<CsItem>
    {
        CsItem.Glock,
        CsItem.Tec9,
    };

    private static readonly ICollection<CsItem> _ctPistols = new HashSet<CsItem>
    {
        CsItem.USP,
        CsItem.P2000,
        CsItem.FiveSeven,
    };

    private static readonly ICollection<CsItem> _sharedMidRange = new HashSet<CsItem>
    {
        // SMG
        CsItem.MP5,
        CsItem.UMP45,
        CsItem.P90,
        CsItem.MP7,
        CsItem.Bizon,

        // Shotgun
        CsItem.XM1014,
        CsItem.Nova,
    };

    private static readonly ICollection<CsItem> _tMidRange = new HashSet<CsItem>
    {
        CsItem.Mac10,
        CsItem.SawedOff,
    };

    private static readonly ICollection<CsItem> _ctMidRange = new HashSet<CsItem>
    {
        CsItem.MP9,
        CsItem.MAG7,
    };

    private static readonly int _maxSmgItemValue = (int) CsItem.UMP;

    private static readonly ICollection<CsItem> _tRifles = new HashSet<CsItem>
    {
        CsItem.AK47,
        CsItem.Galil,
        CsItem.Krieg,
    };

    private static readonly ICollection<CsItem> _ctRifles = new HashSet<CsItem>
    {
        CsItem.Famas,
        CsItem.AUG,
        CsItem.M4A1S,
        CsItem.M4A4,
    };

    private static readonly ICollection<CsItem> _sharedSnipers = new HashSet<CsItem>
    {
        CsItem.AWP,
        CsItem.Scout,
    };

    private static readonly ICollection<CsItem> _tSnipers = new HashSet<CsItem>
    {
        CsItem.AutoSniperT,
    };

    private static readonly ICollection<CsItem> _ctSnipers = new HashSet<CsItem>
    {
        CsItem.AutoSniperCT,
    };

    private static readonly ICollection<CsItem> _allSnipers =
        _sharedSnipers.Concat(_ctSnipers).Concat(_tSnipers).ToHashSet();

    private static readonly ICollection<CsItem> _heavys = new HashSet<CsItem>
    {
        CsItem.M249,
        CsItem.Negev,
    };

    private static readonly ICollection<CsItem> _allWeapons = Enum.GetValues<CsItem>()
        .Where(item => (int) item >= 200 && (int) item < 500)
        .ToHashSet();

    private static readonly Dictionary<
        CsTeam,
        Dictionary<RoundType, ICollection<CsItem>>
    > _validWeaponsByTeamAndRoundType = new()
    {
        {
            CsTeam.Terrorist, new()
            {
                {RoundType.Pistol, new HashSet<CsItem>(_sharedPistols.Concat(_tPistols))},
                {RoundType.HalfBuy, new HashSet<CsItem>(_sharedMidRange.Concat(_tMidRange))},
                {RoundType.FullBuy, _tRifles.Concat(_sharedSnipers).Concat(_tSnipers).Concat(_heavys).ToHashSet()},
            }
        },
        {
            CsTeam.CounterTerrorist, new()
            {
                {RoundType.Pistol, new HashSet<CsItem>(_sharedPistols.Concat(_ctPistols))},
                {RoundType.HalfBuy, new HashSet<CsItem>(_sharedMidRange.Concat(_ctMidRange))},
                {RoundType.FullBuy, _ctRifles.Concat(_sharedSnipers).Concat(_ctSnipers).Concat(_heavys).ToHashSet()},
            }
        }
    };

    private static readonly Dictionary<
        CsTeam,
        Dictionary<RoundType, CsItem>
    > _defaultWeaponsByTeamAndRoundType = new()
    {
        {
            CsTeam.Terrorist, new()
            {
                {RoundType.Pistol, CsItem.Glock},
                {RoundType.HalfBuy, CsItem.Mac10},
                {RoundType.FullBuy, CsItem.AK47},
            }
        },
        {
            CsTeam.CounterTerrorist, new()
            {
                {RoundType.Pistol, CsItem.USPS},
                {RoundType.HalfBuy, CsItem.MP9},
                {RoundType.FullBuy, CsItem.M4A4},
            }
        }
    };

    private static readonly Dictionary<string, CsItem> _weaponNameSearchOverrides = new()
    {
        {"m4a1", CsItem.M4A1S}
    };

    public static List<CsItem> GetAllWeapons()
    {
        return _allWeapons.ToList();
    }

    public static bool IsWeapon(CsItem item)
    {
        return _allWeapons.Contains(item);
    }

    public static bool IsValidWeapon(RoundType roundType, CsTeam team, CsItem weapon)
    {
        if (team != CsTeam.Terrorist && team != CsTeam.CounterTerrorist)
        {
            return false;
        }

        if (_allSnipers.Contains(weapon) || _heavys.Contains(weapon))
        {
            return true;
        }

        return _validWeaponsByTeamAndRoundType[team][roundType].Contains(weapon);
    }

    public static bool IsUsableWeapon(CsItem weapon)
    {
        return Configs.GetConfigData().UsableWeapons.Contains(weapon);
    }

    public static RoundType? GetRoundTypeForWeapon(CsItem weapon)
    {
        if (_allSnipers.Concat(_heavys).Concat(_ctRifles).Concat(_tRifles).Contains(weapon))
        {
            return RoundType.FullBuy;
        }

        if (_sharedMidRange.Concat(_ctMidRange).Concat(_tMidRange).Contains(weapon))
        {
            return RoundType.HalfBuy;
        }

        if (_sharedPistols.Concat(_ctPistols).Concat(_tPistols).Contains(weapon))
        {
            return RoundType.Pistol;
        }

        return null;
    }

    public static ICollection<CsItem> FindValidWeaponsByName(string needle)
    {
        return FindItemsByName(needle)
            .Where(item => _allWeapons.Contains(item))
            .ToList();
    }

    public static ICollection<CsItem> GetWeaponsForRoundType(RoundType roundType, CsTeam team, UserSetting? userSetting)
    {
        var weapons = new List<CsItem>();
        var weapon = GetWeaponForRoundType(RoundType.Pistol, team, userSetting);
        if (weapon is not null)
        {
            weapons.Add(weapon.Value);
        }

        if (roundType == RoundType.Pistol)
        {
            return weapons;
        }

        weapon = GetWeaponForRoundType(roundType, team, userSetting);
        if (weapon is not null)
        {
            weapons.Add(weapon.Value);
        }

        return weapons;
    }

    private static ICollection<CsItem> FindItemsByName(string needle)
    {
        if (_weaponNameSearchOverrides.TryGetValue(needle, out var nameOverride))
        {
            return new List<CsItem> {nameOverride};
        }

        return Enum.GetNames<CsItem>()
            .Where(name => name.ToLower().Contains(needle.ToLower()))
            .Select(Enum.Parse<CsItem>)
            .ToList();
    }

    private static CsItem GetDefaultWeaponForRoundType(RoundType roundType, CsTeam team)
    {
        return _defaultWeaponsByTeamAndRoundType[team][roundType];
    }

    private static CsItem GetRandomWeaponForRoundType(RoundType roundType, CsTeam team)
    {
        var collectionToCheck = roundType switch
        {
            RoundType.Pistol => _sharedPistols.Concat(team == CsTeam.Terrorist ? _tPistols : _ctPistols).ToHashSet(),
            RoundType.HalfBuy =>
                _sharedMidRange
                    .Concat(team == CsTeam.Terrorist ? _tMidRange : _ctMidRange)
                    .Where(item => (int) item <= _maxSmgItemValue)
                    .ToHashSet(),
            RoundType.FullBuy => team == CsTeam.Terrorist ? _tRifles : _ctRifles,
            _ => _sharedPistols,
        };
        return Utils.Choice(collectionToCheck.Where(IsUsableWeapon).ToList());
    }

    public static CsItem? GetWeaponForRoundType(RoundType roundType, CsTeam team, UserSetting? userSetting)
    {
        CsItem? weapon = null;
        if (Configs.GetConfigData().CanPlayersSelectWeapons() && userSetting is not null)
        {
            var weaponPreference = userSetting.GetWeaponPreference(team, roundType);
            if (weaponPreference is not null && IsUsableWeapon(weaponPreference.Value))
            {
                weapon = weaponPreference;
            }
        }

        if (weapon is null && Configs.GetConfigData().CanAssignRandomWeapons())
        {
            weapon = GetRandomWeaponForRoundType(roundType, team);
        }

        if (weapon is null && Configs.GetConfigData().CanAssignDefaultWeapons())
        {
            weapon = GetDefaultWeaponForRoundType(roundType, team);
        }

        return weapon;
    }
}
