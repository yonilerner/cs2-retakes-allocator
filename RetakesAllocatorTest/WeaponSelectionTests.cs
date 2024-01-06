using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;
using RetakesAllocatorCore.db;

namespace RetakesAllocatorTest;

public class WeaponSelectionTests
{
    [SetUp]
    public void Setup()
    {
        Queries.Migrate();
        Queries.Wipe();
    }

    [TearDown]
    public void TearDown()
    {
        Queries.Disconnect();
    }

    [Test]
    public void SetWeaponPreferenceDirectly()
    {
        Assert.That(Queries.GetUserSettings(1)?.GetWeaponPreference(CsTeam.Terrorist, RoundType.FullBuy),
            Is.EqualTo(null));

        Queries.SetWeaponPreferenceForUser(1, CsTeam.Terrorist, RoundType.FullBuy, CsItem.Galil);
        Assert.That(Queries.GetUserSettings(1)?.GetWeaponPreference(CsTeam.Terrorist, RoundType.FullBuy),
            Is.EqualTo(CsItem.Galil));

        Queries.SetWeaponPreferenceForUser(1, CsTeam.Terrorist, RoundType.FullBuy, CsItem.AWP);
        Assert.That(Queries.GetUserSettings(1)?.GetWeaponPreference(CsTeam.Terrorist, RoundType.FullBuy),
            Is.EqualTo(CsItem.AWP));

        Queries.SetWeaponPreferenceForUser(1, CsTeam.Terrorist, RoundType.Pistol, CsItem.Deagle);
        Assert.That(Queries.GetUserSettings(1)?.GetWeaponPreference(CsTeam.Terrorist, RoundType.Pistol),
            Is.EqualTo(CsItem.Deagle));

        Assert.That(Queries.GetUserSettings(1)?.GetWeaponPreference(CsTeam.CounterTerrorist, RoundType.HalfBuy),
            Is.EqualTo(null));
        Queries.SetWeaponPreferenceForUser(1, CsTeam.CounterTerrorist, RoundType.HalfBuy, CsItem.MP9);
        Assert.That(Queries.GetUserSettings(1)?.GetWeaponPreference(CsTeam.CounterTerrorist, RoundType.HalfBuy),
            Is.EqualTo(CsItem.MP9));
    }

    [Test]
    [TestCase(CsTeam.Terrorist, RoundType.FullBuy, "galil", CsItem.Galil, "Galil' is now")]
    [TestCase(CsTeam.Terrorist, RoundType.FullBuy, "krieg", CsItem.Krieg, "SG553' is now")]
    [TestCase(CsTeam.Terrorist, RoundType.HalfBuy, "mac10", CsItem.Mac10, "Mac10' is now")]
    [TestCase(CsTeam.CounterTerrorist, RoundType.Pistol, "deag", CsItem.Deagle, "Deagle' is now")]
    [TestCase(CsTeam.CounterTerrorist, RoundType.FullBuy, "galil", null, "Galil' is not valid")]
    [TestCase(CsTeam.CounterTerrorist, RoundType.Pistol, "tec9", null, "Tec9' is not valid")]
    [TestCase(CsTeam.Terrorist, RoundType.Pistol, "ak47", null, "AK47' is not valid")]
    [TestCase(CsTeam.Terrorist, RoundType.FullBuy, "poop", null, "not found")]
    public void SetWeaponPreferenceCommandSingleArg(CsTeam team, RoundType roundType, string itemInput,
        CsItem? expectedItem,
        string message)
    {
        var args = new List<string> { itemInput };

        var result = OnWeaponCommandHelper.Handle(args, 1, team, roundType);

        Assert.That(result, Does.Contain(message));

        var setWeapon = Queries.GetUserSettings(1)?
            .GetWeaponsForTeamAndRound(team, roundType).FirstOrDefault();
        Assert.That(setWeapon, Is.EqualTo(expectedItem));
    }

    [Test]
    [TestCase("T", "F", "galil", CsItem.Galil, "Galil' is now")]
    [TestCase("T", "F", "krieg", CsItem.Krieg, "SG553' is now")]
    [TestCase("T", "H", "mac10", CsItem.Mac10, "Mac10' is now")]
    [TestCase("CT", "P", "deag", CsItem.Deagle, "Deagle' is now")]
    [TestCase("CT", "F", "galil", null, "Galil' is not valid")]
    [TestCase("CT", "P", "tec9", null, "Tec9' is not valid")]
    [TestCase("T", "P", "ak47", null, "AK47' is not valid")]
    [TestCase("T", "F", "poop", null, "not found")]
    public void SetWeaponPreferenceCommandMultiArg(string teamInput, string roundTypeInput, string itemInput,
        CsItem? expectedItem,
        string message)
    {
        var args = new List<string> { itemInput, teamInput, roundTypeInput };

        var result = OnWeaponCommandHelper.Handle(args, 1, CsTeam.None, RoundType.Pistol);

        Assert.That(result, Does.Contain(message));

        var setWeapon = Queries.GetUserSettings(1)?
            .GetWeaponsForTeamAndRound(Utils.ParseTeam(teamInput),
                RoundTypeHelpers.ParseRoundType(roundTypeInput)!.Value).FirstOrDefault();
        Assert.That(setWeapon, Is.EqualTo(expectedItem));
    }
}
