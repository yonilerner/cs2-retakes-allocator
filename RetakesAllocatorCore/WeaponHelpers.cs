using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;

namespace RetakesAllocatorCore;

public enum WeaponAllocationType
{
    FullBuyPrimary,
    HalfBuyPrimary,
    Secondary,
    PistolRound,
    Sniper,
}

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

    private static readonly ICollection<CsItem> _allFullBuy =
        _allSnipers.Concat(_heavys).Concat(_tRifles).Concat(_ctRifles).ToHashSet();

    private static readonly ICollection<CsItem> _allHalfBuy =
        _midRangeForT.Concat(_midRangeForCt).ToHashSet();

    private static readonly ICollection<CsItem> _allPistols =
        _pistolsForT.Concat(_pistolsForCt).ToHashSet();

    private static readonly Dictionary<
        CsTeam,
        Dictionary<WeaponAllocationType, ICollection<CsItem>>
    > _validWeaponsByTeamAndAllocationType = new()
    {
        {
            CsTeam.Terrorist, new()
            {
                {WeaponAllocationType.PistolRound, _pistolsForT},
                {WeaponAllocationType.Secondary, _pistolsForT},
                {WeaponAllocationType.HalfBuyPrimary, _midRangeForT},
                {WeaponAllocationType.FullBuyPrimary, _fullBuyForT},
                {WeaponAllocationType.Sniper, _snipersForT},
            }
        },
        {
            CsTeam.CounterTerrorist, new()
            {
                {WeaponAllocationType.PistolRound, _pistolsForCt},
                {WeaponAllocationType.Secondary, _pistolsForCt},
                {WeaponAllocationType.HalfBuyPrimary, _midRangeForCt},
                {WeaponAllocationType.FullBuyPrimary, _fullBuyForCt},
                {WeaponAllocationType.Sniper, _snipersForCt},
            }
        }
    };

    private static readonly Dictionary<
        CsTeam,
        Dictionary<WeaponAllocationType, CsItem>
    > _defaultWeaponsByTeamAndAllocationType = new()
    {
        {
            CsTeam.Terrorist, new()
            {
                {WeaponAllocationType.FullBuyPrimary, CsItem.AK47},
                {WeaponAllocationType.HalfBuyPrimary, CsItem.Mac10},
                {WeaponAllocationType.Secondary, CsItem.Deagle},
                {WeaponAllocationType.PistolRound, CsItem.Glock},
                {WeaponAllocationType.Sniper, CsItem.AWP},
            }
        },
        {
            CsTeam.CounterTerrorist, new()
            {
                {WeaponAllocationType.FullBuyPrimary, CsItem.M4A1S},
                {WeaponAllocationType.HalfBuyPrimary, CsItem.MP9},
                {WeaponAllocationType.Secondary, CsItem.Deagle},
                {WeaponAllocationType.PistolRound, CsItem.USPS},
                {WeaponAllocationType.Sniper, CsItem.AWP},
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

    public static ICollection<CsItem> GetPossibleWeaponsForAllocationType(WeaponAllocationType allocationType,
        CsTeam team)
    {
        return _validWeaponsByTeamAndAllocationType[team][allocationType].Where(IsUsableWeapon).ToList();
    }

    // TODO Im not convinced this is reasonable
    public static bool IsAllocationTypeValidForRound(WeaponAllocationType allocationType, RoundType roundType)
    {
        return roundType switch
        {
            RoundType.Pistol => allocationType is WeaponAllocationType.PistolRound or WeaponAllocationType.Secondary,
            RoundType.HalfBuy => allocationType == WeaponAllocationType.HalfBuyPrimary,
            RoundType.FullBuy => allocationType is WeaponAllocationType.FullBuyPrimary or WeaponAllocationType.Sniper,
            _ => false
        };
    }

    public static WeaponAllocationType? WeaponAllocationTypeForWeaponAndRound(RoundType roundType, CsTeam team,
        CsItem weapon)
    {
        if (team != CsTeam.Terrorist && team != CsTeam.CounterTerrorist)
        {
            return null;
        }

        if (_allSnipers.Contains(weapon))
        {
            return WeaponAllocationType.Sniper;
        }

        var allocationsByTeam = _validWeaponsByTeamAndAllocationType[team];
        foreach (var (allocationType, items) in allocationsByTeam)
        {
            if (items.Contains(weapon) && IsAllocationTypeValidForRound(allocationType, roundType))
            {
                return allocationType;
            }
        }

        return null;
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

    // TODO In the future this will be more complex based on sniper config
    public static ICollection<T> SelectSnipers<T>(ICollection<T> players)
    {
        var player = Utils.Choice(players);
        if (player is null)
        {
            return new HashSet<T>();
        }

        return new HashSet<T> {player};
    }

    public static bool IsUsableWeapon(CsItem weapon)
    {
        return Configs.GetConfigData().UsableWeapons.Contains(weapon);
    }

    public static CsItem? CoerceSniperTeam(CsItem? sniper, CsTeam team)
    {
        if (sniper == null || !_allSnipers.Contains(sniper.Value))
        {
            return null;
        }

        if (team != CsTeam.Terrorist && team != CsTeam.CounterTerrorist)
        {
            return null;
        }

        if (sniper is CsItem.AWP or CsItem.Scout)
        {
            return sniper;
        }

        return team == CsTeam.Terrorist ? CsItem.AutoSniperT : CsItem.AutoSniperCT;
    }

    // TODO Change all usages of this
    public static RoundType? GetRoundTypeForWeapon(CsItem weapon)
    {
        if (_allFullBuy.Contains(weapon))
        {
            return RoundType.FullBuy;
        }

        if (_allHalfBuy.Contains(weapon))
        {
            return RoundType.HalfBuy;
        }

        if (_allPistols.Contains(weapon))
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

    public static WeaponAllocationType? GetWeaponAllocationTypeForWeapon(CsItem weapon, RoundType roundType)
    {
        if (_allPistols.Contains(weapon))
        {
            return roundType switch
            {
                RoundType.Pistol => WeaponAllocationType.PistolRound,
                _ => WeaponAllocationType.Secondary,
            };
        }

        if (_allHalfBuy.Contains(weapon) && roundType == RoundType.HalfBuy)
        {
            return WeaponAllocationType.HalfBuyPrimary;
        }

        if (_allFullBuy.Contains(weapon) && roundType == RoundType.FullBuy)
        {
            return WeaponAllocationType.FullBuyPrimary;
        }

        return null;
    }

    public static ICollection<CsItem> GetWeaponsForRoundType(
        RoundType roundType,
        CsTeam team,
        UserSetting? userSetting,
        bool giveSniper
    )
    {
        WeaponAllocationType? primaryWeaponAllocation =
            giveSniper
                ? WeaponAllocationType.Sniper
                : roundType switch
                {
                    RoundType.HalfBuy => WeaponAllocationType.HalfBuyPrimary,
                    RoundType.FullBuy => WeaponAllocationType.FullBuyPrimary,
                    _ => null,
                };

        var secondaryWeaponAllocation = roundType switch
        {
            RoundType.Pistol => WeaponAllocationType.PistolRound,
            _ => WeaponAllocationType.Secondary,
        };

        var weapons = new List<CsItem>();
        var secondary = GetWeaponForAllocationType(secondaryWeaponAllocation, team, userSetting);
        if (secondary is not null)
        {
            weapons.Add(secondary.Value);
        }

        if (primaryWeaponAllocation is null)
        {
            return weapons;
        }

        var primary = GetWeaponForAllocationType(primaryWeaponAllocation.Value, team, userSetting);
        if (primary is not null)
        {
            weapons.Add(primary.Value);
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

    private static CsItem GetDefaultWeaponForAllocationType(WeaponAllocationType allocationType, CsTeam team)
    {
        return _defaultWeaponsByTeamAndAllocationType[team][allocationType];
    }

    private static CsItem GetRandomWeaponForAllocationType(WeaponAllocationType allocationType, CsTeam team)
    {
        if (team != CsTeam.Terrorist && team != CsTeam.CounterTerrorist)
        {
            return CsItem.Deagle;
        }

        var collectionToCheck = allocationType switch
        {
            WeaponAllocationType.PistolRound => team == CsTeam.Terrorist ? _pistolsForT : _pistolsForCt,
            WeaponAllocationType.Secondary => team == CsTeam.Terrorist ? _pistolsForT : _pistolsForCt,
            WeaponAllocationType.HalfBuyPrimary => team == CsTeam.Terrorist ? _smgsForT : _smgsForCt,
            WeaponAllocationType.FullBuyPrimary => team == CsTeam.Terrorist ? _tRifles : _ctRifles,
            WeaponAllocationType.Sniper => team == CsTeam.Terrorist ? _snipersForT : _snipersForCt,
            _ => _sharedPistols,
        };
        return Utils.Choice(collectionToCheck.Where(IsUsableWeapon).ToList());
    }

    public static CsItem? GetWeaponForAllocationType(WeaponAllocationType allocationType, CsTeam team,
        UserSetting? userSetting)
    {
        CsItem? weapon = null;

        if (Configs.GetConfigData().CanPlayersSelectWeapons() && userSetting is not null)
        {
            var weaponPreference = userSetting.GetWeaponPreference(team, allocationType);
            if (weaponPreference is not null && IsUsableWeapon(weaponPreference.Value))
            {
                weapon = weaponPreference;
            }
        }

        if (weapon is null && Configs.GetConfigData().CanAssignRandomWeapons())
        {
            weapon = GetRandomWeaponForAllocationType(allocationType, team);
        }

        if (weapon is null && Configs.GetConfigData().CanAssignDefaultWeapons())
        {
            weapon = GetDefaultWeaponForAllocationType(allocationType, team);
        }

        return weapon;
    }

    public static bool IsWeaponAllocationAllowed(bool isFreezePeriod)
    {
        return Configs.GetConfigData().AllowAllocationAfterFreezeTime || isFreezePeriod;
    }
}
