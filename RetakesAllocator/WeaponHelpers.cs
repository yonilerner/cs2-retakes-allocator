using System.Collections;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesAllocator;

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

    private static readonly ICollection<CsItem> _tRifles = new HashSet<CsItem>
    {
        CsItem.AK47,
        CsItem.Galil,
        CsItem.Krieg,
        CsItem.AutoSniperT,
    };

    private static readonly ICollection<CsItem> _ctRifles = new HashSet<CsItem>
    {
        CsItem.Famas,
        CsItem.AUG,
        CsItem.M4A1,
        CsItem.M4A4,
        CsItem.AutoSniperCT,
    };

    private static readonly ICollection<CsItem> _snipers = new HashSet<CsItem>
    {
        CsItem.AWP,
        CsItem.Scout,
    };

    private static readonly ICollection<CsItem> _heavys = new HashSet<CsItem>
    {
        CsItem.M249,
        CsItem.Negev,
    };


    private static readonly Dictionary<
        CsTeam,
        Dictionary<RoundType, ICollection<CsItem>>
    > _weaponsByTeamAndRoundType = new()
    {
        {
            CsTeam.Terrorist, new()
            {
                {RoundType.Pistol, new HashSet<CsItem>(_sharedPistols.Concat(_tPistols))},
                {RoundType.HalfBuy, new HashSet<CsItem>(_sharedMidRange.Concat(_tMidRange))},
                {RoundType.FullBuy, _tRifles},
            }
        },
        {
            CsTeam.CounterTerrorist, new()
            {
                {RoundType.Pistol, new HashSet<CsItem>(_sharedPistols.Concat(_ctPistols))},
                {RoundType.HalfBuy, new HashSet<CsItem>(_sharedMidRange.Concat(_ctMidRange))},
                {RoundType.FullBuy, _ctRifles},
            }
        }
    };

    public static bool IsValidWeapon(RoundType roundType, CsTeam team, CsItem weapon)
    {
        if (team != CsTeam.Terrorist && team != CsTeam.CounterTerrorist)
        {
            return false;
        }

        if (_snipers.Contains(weapon) || _heavys.Contains(weapon))
        {
            return true;
        }

        return _weaponsByTeamAndRoundType[team][roundType].Contains(weapon);
    }

    public static ICollection<CsItem> FindItemByName(string needle)
    {
        return Enum.GetValues<CsItem>().Where(item => Enum.GetName(item)?.ToLower().Contains(needle.ToLower()) ?? false)
            .ToList();
    }

    private static readonly ICollection<string> _weaponSentinels = new HashSet<string>
    {
        "remove",
        "delete",
        "unset",
        "none",
    };

    public static bool IsRemoveWeaponSentinel(string needle)
    {
        return _weaponSentinels.Contains(needle.ToLower());
    }

    public static CsItem GetRandomWeaponForRoundType(RoundType roundType, CsTeam team)
    {
        var collectionToCheck = roundType switch
        {
            RoundType.Pistol => team == CsTeam.Terrorist ? _tPistols : _ctPistols,
            RoundType.HalfBuy => team == CsTeam.Terrorist ? _tMidRange : _ctMidRange,
            RoundType.FullBuy => team == CsTeam.Terrorist ? _tRifles : _ctRifles,
        };
        return Utils.Choice(collectionToCheck);
    }

    public static IEnumerable<CsItem> GetRandomWeaponsForRoundType(RoundType roundType, CsTeam team)
    {
        var weapons = new List<CsItem>();
        if (roundType == RoundType.Pistol)
        {
            weapons.Add(GetRandomWeaponForRoundType(roundType, team));
            return weapons;
        }

        weapons.Add(team == CsTeam.Terrorist ? CsItem.Glock : CsItem.USP);

        if (roundType == RoundType.FullBuy && new Random().NextDouble() < 0.2)
        {
            weapons.Add(CsItem.AWP);
        }
        else
        {
            weapons.Add(GetRandomWeaponForRoundType(roundType, team));
        }

        return weapons;
    }
}
