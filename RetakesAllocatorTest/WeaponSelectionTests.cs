using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;

namespace RetakesAllocatorTest;

public class WeaponSelectionTests : BaseTestFixture
{
    [Test]
    public void SetWeaponPreferenceDirectly()
    {
        Assert.That(
            Queries.GetUserSettings(1)?.GetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.FullBuyPrimary),
            Is.EqualTo(null));

        Queries.SetWeaponPreferenceForUser(1, CsTeam.Terrorist, WeaponAllocationType.FullBuyPrimary, CsItem.Galil);
        Assert.That(
            Queries.GetUserSettings(1)?.GetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.FullBuyPrimary),
            Is.EqualTo(CsItem.Galil));

        Queries.SetWeaponPreferenceForUser(1, CsTeam.Terrorist, WeaponAllocationType.FullBuyPrimary, CsItem.AWP);
        Assert.That(
            Queries.GetUserSettings(1)?.GetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.FullBuyPrimary),
            Is.EqualTo(CsItem.AWP));

        Queries.SetWeaponPreferenceForUser(1, CsTeam.Terrorist, WeaponAllocationType.PistolRound, CsItem.Deagle);
        Assert.That(Queries.GetUserSettings(1)?.GetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.PistolRound),
            Is.EqualTo(CsItem.Deagle));

        Assert.That(
            Queries.GetUserSettings(1)
                ?.GetWeaponPreference(CsTeam.CounterTerrorist, WeaponAllocationType.HalfBuyPrimary),
            Is.EqualTo(null));
        Queries.SetWeaponPreferenceForUser(1, CsTeam.CounterTerrorist, WeaponAllocationType.HalfBuyPrimary, CsItem.MP9);
        Assert.That(
            Queries.GetUserSettings(1)
                ?.GetWeaponPreference(CsTeam.CounterTerrorist, WeaponAllocationType.HalfBuyPrimary),
            Is.EqualTo(CsItem.MP9));
    }

    [Test]
    [TestCase(RoundType.FullBuy, CsTeam.Terrorist, "galil", CsItem.Galil, "Galil' is now", "Galil' is no longer")]
    [TestCase(RoundType.HalfBuy, CsTeam.Terrorist, "galil", null, "Galil' is now;;;at the next FullBuy", "Galil' is no longer")]
    [TestCase(RoundType.FullBuy, CsTeam.Terrorist, "krieg", CsItem.Krieg, "SG553' is now", "SG553' is no longer")]
    [TestCase(RoundType.HalfBuy, CsTeam.Terrorist, "mac10", CsItem.Mac10, "Mac10' is now", "Mac10' is no longer")]
    [TestCase(RoundType.FullBuy, CsTeam.Terrorist, "mac10", null, "Mac10' is now;;;at the next HalfBuy", "Mac10' is no longer")]
    [TestCase(RoundType.Pistol, CsTeam.CounterTerrorist, "deag", CsItem.Deagle, "Deagle' is now",
        "Deagle' is no longer")]
    [TestCase(RoundType.FullBuy, CsTeam.CounterTerrorist, "deag", CsItem.Deagle, "Deagle' is now",
        "Deagle' is no longer")]
    [TestCase(RoundType.HalfBuy, CsTeam.CounterTerrorist, "deag", CsItem.Deagle, "Deagle' is now",
        "Deagle' is no longer")]
    [TestCase(RoundType.FullBuy, CsTeam.CounterTerrorist, "galil", null, "Galil' is not valid", null)]
    [TestCase(RoundType.Pistol, CsTeam.CounterTerrorist, "tec9", null, "Tec9' is not valid", null)]
    [TestCase(RoundType.FullBuy, CsTeam.Terrorist, "poop", null, "not found", null)]
    [TestCase(RoundType.FullBuy, CsTeam.Terrorist, "galil,T", CsItem.Galil, "Galil' is now", null)]
    [TestCase(RoundType.FullBuy, CsTeam.Terrorist, "krieg,T", CsItem.Krieg, "SG553' is now", null)]
    [TestCase(RoundType.HalfBuy, CsTeam.Terrorist, "mac10,T", CsItem.Mac10, "Mac10' is now", null)]
    [TestCase(RoundType.HalfBuy, CsTeam.None, "mac10,T", null, "Mac10' is now", null)]
    [TestCase(RoundType.Pistol, CsTeam.CounterTerrorist, "deag,CT", CsItem.Deagle, "Deagle' is now", null)]
    [TestCase(RoundType.FullBuy, CsTeam.CounterTerrorist, "galil,CT", null, "Galil' is not valid", null)]
    [TestCase(RoundType.Pistol, CsTeam.CounterTerrorist, "tec9,CT", null, "Tec9' is not valid", null)]
    [TestCase(RoundType.FullBuy, CsTeam.Terrorist, "poop,T", null, "not found", null)]
    public void SetWeaponPreferenceCommandSingleArg(
        RoundType roundType,
        CsTeam team,
        string strArgs,
        CsItem? expectedItem,
        string message,
        string? removeMessage
    )
    {
        var args = strArgs.Split(",");

        var result = OnWeaponCommandHelper.Handle(args, 1, roundType, team, false, out var selectedItem);

        var messages = message.Split(";;;");
        foreach (var m in messages)
        {
            Assert.That(result, Does.Contain(m));
        }

        Assert.That(selectedItem, Is.EqualTo(expectedItem));

        var allocationType =
            selectedItem is not null
                ? WeaponHelpers.GetWeaponAllocationTypeForWeapon(selectedItem.Value, roundType)
                : null;

        var setWeapon = allocationType is not null
            ? Queries.GetUserSettings(1)?
                .GetWeaponPreference(team, allocationType.Value)
            : null;
        Assert.That(setWeapon, Is.EqualTo(expectedItem));

        if (removeMessage is not null)
        {
            result = OnWeaponCommandHelper.Handle(args, 1, roundType, team, true, out _);
            Assert.That(result, Does.Contain(removeMessage));

            setWeapon = allocationType is not null
                ? Queries.GetUserSettings(1)?.GetWeaponPreference(team, allocationType.Value)
                : null;
            Assert.That(setWeapon, Is.EqualTo(null));
        }
    }

    [Test]
    [TestCase("ak", CsItem.AK47, WeaponSelectionType.PlayerChoice, CsItem.AK47, "AK47' is now")]
    [TestCase("ak", CsItem.Galil, WeaponSelectionType.PlayerChoice, null, "not allowed")]
    [TestCase("ak", CsItem.AK47, WeaponSelectionType.Default, null, "cannot choose")]
    public void SetWeaponPreferencesConfig(
        string itemName,
        CsItem? allowedItem,
        WeaponSelectionType weaponSelectionType,
        CsItem? expectedItem,
        string message
    )
    {
        var team = CsTeam.Terrorist;
        Configs.GetConfigData().AllowedWeaponSelectionTypes = new List<WeaponSelectionType> {weaponSelectionType};
        Configs.GetConfigData().UsableWeapons = new List<CsItem> { };
        if (allowedItem is not null)
        {
            Configs.GetConfigData().UsableWeapons.Add(allowedItem.Value);
        }
    
        var args = new List<string> {itemName};
        var result = OnWeaponCommandHelper.Handle(args, 1, RoundType.FullBuy, team, false, out var selectedItem);
    
        Assert.That(result, Does.Contain(message));
        Assert.That(selectedItem, Is.EqualTo(expectedItem));
    
        var setWeapon = Queries.GetUserSettings(1)?.GetWeaponPreference(team, WeaponAllocationType.FullBuyPrimary);
        Assert.That(setWeapon, Is.EqualTo(expectedItem));
    }

    [Test]
    [Retry(3)]
    public void RandomWeaponSelection()
    {
        Configs.OverrideConfigDataForTests(new ConfigData
            {
                RoundTypePercentages = new()
                {
                    {RoundType.Pistol, 5},
                    {RoundType.HalfBuy, 5},
                    {RoundType.FullBuy, 90},
                }
            });
        var numPistol = 0;
        var numHalfBuy = 0;
        var numFullBuy = 0;
        for (var i = 0; i < 1000; i++)
        {
            var randomRoundType = RoundTypeHelpers.GetRandomRoundType();
            switch (randomRoundType)
            {
                case RoundType.Pistol:
                    numPistol++;
                    break;
                case RoundType.HalfBuy:
                    numHalfBuy++;
                    break;
                case RoundType.FullBuy:
                    numFullBuy++;
                    break;
            }
        }

        // Ranges are very permissive to avoid flakes
        Assert.That(numPistol, Is.InRange(20, 80));
        Assert.That(numHalfBuy, Is.InRange(20, 80));
        Assert.That(numFullBuy, Is.InRange(850, 950));
    }
}
