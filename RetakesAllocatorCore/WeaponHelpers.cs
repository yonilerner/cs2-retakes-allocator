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
        CsItem.Deagle,
        CsItem.P250,
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

    private static readonly ICollection<CsItem> _pistolsForT =
        _sharedPistols.Concat(_tPistols).ToHashSet();

    private static readonly ICollection<CsItem> _pistolsForCt =
        _sharedPistols.Concat(_ctPistols).ToHashSet();

    private static readonly ICollection<CsItem> _sharedMidRange = new HashSet<CsItem>
    {
        // SMG
        CsItem.P90,
        CsItem.UMP45,
        CsItem.MP7,
        CsItem.Bizon,
        CsItem.MP5,

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

    private static readonly ICollection<CsItem> _midRangeForCt = _sharedMidRange.Concat(_ctMidRange).ToHashSet();
    private static readonly ICollection<CsItem> _midRangeForT = _sharedMidRange.Concat(_tMidRange).ToHashSet();

    private static readonly int _maxSmgItemValue = (int) CsItem.UMP;

    private static readonly ICollection<CsItem> _smgsForT =
        _sharedMidRange.Concat(_tMidRange).Where(i => (int) i <= _maxSmgItemValue).ToHashSet();

    private static readonly ICollection<CsItem> _smgsForCt =
        _sharedMidRange.Concat(_ctMidRange).Where(i => (int) i <= _maxSmgItemValue).ToHashSet();

    private static readonly ICollection<CsItem> _tRifles = new HashSet<CsItem>
    {
        CsItem.AK47,
        CsItem.Galil,
        CsItem.Krieg,
    };

    private static readonly ICollection<CsItem> _ctRifles = new HashSet<CsItem>
    {
        CsItem.M4A1S,
        CsItem.M4A4,
        CsItem.Famas,
        CsItem.AUG,
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

    private static readonly ICollection<CsItem> _snipersForT = _sharedSnipers.Concat(_tSnipers).ToHashSet();
    private static readonly ICollection<CsItem> _snipersForCt = _sharedSnipers.Concat(_ctSnipers).ToHashSet();

    private static readonly ICollection<CsItem> _allSnipers =
        _snipersForT.Concat(_snipersForCt).ToHashSet();

    private static readonly ICollection<CsItem> _heavys = new HashSet<CsItem>
    {
        CsItem.M249,
        CsItem.Negev,
    };

    private static readonly ICollection<CsItem> _fullBuyForT =
        _tRifles.Concat(_snipersForT).Concat(_heavys).ToHashSet();

    private static readonly ICollection<CsItem> _fullBuyForCt =
        _ctRifles.Concat(_snipersForCt).Concat(_heavys).ToHashSet();

    private static readonly ICollection<CsItem> _allWeapons = Enum.GetValues<CsItem>()
        .Where(item => (int) item >= 200 && (int) item < 500)
        .ToHashSet();

    private static readonly ICollection<CsItem> _fullBuyRound =
        _allSnipers.Concat(_heavys).Concat(_tRifles).Concat(_ctRifles).ToHashSet();

    private static readonly ICollection<CsItem> _halfBuyRound =
        _midRangeForT.Concat(_midRangeForCt).ToHashSet();

    private static readonly ICollection<CsItem> _pistolRound =
        _pistolsForT.Concat(_pistolsForCt).ToHashSet();

    private static readonly Dictionary<
        CsTeam,
        Dictionary<RoundType, ICollection<CsItem>>
    > _validWeaponsByTeamAndRoundType = new()
    {
        {
            CsTeam.Terrorist, new()
            {
                {RoundType.Pistol, _pistolsForT},
                {RoundType.HalfBuy, _midRangeForT},
                {RoundType.FullBuy, _fullBuyForT},
            }
        },
        {
            CsTeam.CounterTerrorist, new()
            {
                {RoundType.Pistol, _pistolsForCt},
                {RoundType.HalfBuy, _midRangeForCt},
                {RoundType.FullBuy, _fullBuyForCt},
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
        {"m4a1", CsItem.M4A1S},
        {"m4a1-s", CsItem.M4A1S},
    };

    public static List<CsItem> GetAllWeapons()
    {
        return _allWeapons.ToList();
    }

    public static bool IsWeapon(CsItem item)
    {
        return _allWeapons.Contains(item);
    }

    public static ICollection<CsItem> GetPossibleWeaponsForRoundType(RoundType roundType, CsTeam team)
    {
        return _validWeaponsByTeamAndRoundType[team][roundType].Where(IsUsableWeapon).ToList();
    }

    public static bool IsValidWeaponForRound(RoundType roundType, CsTeam team, CsItem weapon)
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

    public static bool IsSniper(CsTeam team, CsItem weapon)
    {
        return team switch
        {
            CsTeam.Terrorist => _snipersForT.Contains(weapon),
            CsTeam.CounterTerrorist => _snipersForCt.Contains(weapon),
            _ => false,
        };
    }

    public static bool IsUsableWeapon(CsItem weapon)
    {
        return Configs.GetConfigData().UsableWeapons.Contains(weapon);
    }

    public static RoundType? GetRoundTypeForWeapon(CsItem weapon)
    {
        if (_fullBuyRound.Contains(weapon))
        {
            return RoundType.FullBuy;
        }

        if (_halfBuyRound.Contains(weapon))
        {
            return RoundType.HalfBuy;
        }

        if (_pistolRound.Contains(weapon))
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
        needle = needle.ToLower();
        if (_weaponNameSearchOverrides.TryGetValue(needle, out var nameOverride))
        {
            return new List<CsItem> {nameOverride};
        }

        return Enum.GetNames<CsItem>()
            .Where(name => name.ToLower().Contains(needle))
            .Select(Enum.Parse<CsItem>)
            .ToList();
    }

    private static CsItem GetDefaultWeaponForRoundType(RoundType roundType, CsTeam team)
    {
        return _defaultWeaponsByTeamAndRoundType[team][roundType];
    }

    private static CsItem GetRandomWeaponForRoundType(RoundType roundType, CsTeam team)
    {
        if (team != CsTeam.Terrorist && team != CsTeam.CounterTerrorist)
        {
            return CsItem.Deagle;
        }

        var collectionToCheck = roundType switch
        {
            RoundType.Pistol => team == CsTeam.Terrorist ? _pistolsForT : _pistolsForCt,
            RoundType.HalfBuy => team == CsTeam.Terrorist ? _smgsForT : _smgsForCt,
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

    public static bool IsWeaponAllocationAllowed(bool isFreezePeriod)
    {
        return Configs.GetConfigData().AllowAllocationAfterFreezeTime || isFreezePeriod;
    }
}
