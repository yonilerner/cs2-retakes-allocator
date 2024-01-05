using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesAllocator;

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
        var possibleItems = new List<CsItem>
        {
            CsItem.Flashbang,
            CsItem.Smoke,
            CsItem.HEGrenade,
            team == CsTeam.Terrorist ? CsItem.Molotov : CsItem.Incendiary,
        };

        // Everyone gets one util
        var randomUtil = new List<CsItem>
        {
            Utils.Choice(possibleItems),
        };

        // 50% chance to get an extra util item
        // We cant give people duplicate of anything other than a flash though, so
        //  try up to 50 times to give them a duplicate flash or a non-duplicate other nade
        if (new Random().NextDouble() < .5)
        {
            var i = 0;
            while (i < 50)
            {
                var extraItem = Utils.Choice(possibleItems);
                if (extraItem == CsItem.Flashbang || !randomUtil.Contains(extraItem))
                {
                    randomUtil.Add(extraItem);
                    break;
                }

                i++;
            }
        }

        return randomUtil;
    }

    public static CsItem GetArmorForRoundType(RoundType roundType) =>
        roundType == RoundType.Pistol ? CsItem.Kevlar : CsItem.KevlarHelmet;
}