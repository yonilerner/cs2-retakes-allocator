using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesAllocatorCore;

public enum RoundType
{
    Pistol,
    HalfBuy,
    FullBuy,
}

public static class RoundTypeHelpers
{
    public static RoundType GetRandomRoundType()
    {
        var randomValue = new Random().NextDouble();

        return randomValue switch
        {
            // 15% chance of pistol round
            < 0.15 => RoundType.Pistol,
            // 25% chance of halfbuy round
            < 0.40 => RoundType.HalfBuy,
            // 60% chance of fullbuy round
            _ => RoundType.FullBuy,
        };
    }

    public static IEnumerable<CsItem> GetRandomUtilForRoundType(RoundType roundType, CsTeam team)
    {
        // Limited util on pistol rounds
        if (roundType == RoundType.Pistol)
        {
            return new List<CsItem>
            {
                Utils.Choice(new List<CsItem>
                {
                    CsItem.Flashbang,
                    CsItem.Smoke,
                }),
            };
        }

        // All util options are available on buy rounds
        var possibleItems = new HashSet<CsItem>()
        {
            CsItem.Flashbang,
            CsItem.Smoke,
            CsItem.HEGrenade,
            team == CsTeam.Terrorist ? CsItem.Molotov : CsItem.Incendiary,
        };

        var firstUtil = Utils.Choice(possibleItems);

        // Everyone gets one util
        var randomUtil = new List<CsItem>
        {
            firstUtil,
        };

        // 50% chance to get an extra util item
        if (new Random().NextDouble() < .5)
        {
            // We cant give people duplicate of anything other than a flash though
            if (firstUtil != CsItem.Flashbang)
            {
                possibleItems.Remove(firstUtil);
            }

            randomUtil.Add(Utils.Choice(possibleItems));
        }

        return randomUtil;
    }

    public static CsItem GetArmorForRoundType(RoundType roundType) =>
        roundType == RoundType.Pistol ? CsItem.Kevlar : CsItem.KevlarHelmet;
}
