using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Db;

namespace RetakesAllocatorTest;

public class WeaponSelectionTests
{
    [SetUp]
    public void Setup()
    {
        Queries.Wipe();
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
    [TestCase(CsTeam.Terrorist, "galil", CsItem.Galil, "Galil' is now", "Galil' is no longer")]
    [TestCase(CsTeam.Terrorist, "krieg", CsItem.Krieg, "SG553' is now", "SG553' is no longer")]
    [TestCase(CsTeam.Terrorist, "mac10", CsItem.Mac10, "Mac10' is now", "Mac10' is no longer")]
    [TestCase(CsTeam.CounterTerrorist, "deag", CsItem.Deagle, "Deagle' is now", "Deagle' is no longer")]
    [TestCase(CsTeam.CounterTerrorist, "galil", null, "Galil' is not valid", null)]
    [TestCase(CsTeam.CounterTerrorist, "tec9", null, "Tec9' is not valid", null)]
    [TestCase(CsTeam.Terrorist, "poop", null, "not found", null)]
    public void SetWeaponPreferenceCommandSingleArg(
        CsTeam team, string itemInput,
        CsItem? expectedItem,
        string message,
        string? removeMessage
    )
    {
        var args = new List<string> {itemInput};

        var result = OnWeaponCommandHelper.Handle(args, 1, team, false, out var selectedItem);

        Assert.That(result, Does.Contain(message));
        Assert.That(selectedItem, Is.EqualTo(expectedItem));

        var roundType = expectedItem != null
            ? WeaponHelpers.GetRoundTypeForWeapon(expectedItem.Value) ?? RoundType.Pistol
            : RoundType.Pistol;

        var setWeapon = Queries.GetUserSettings(1)?
            .GetWeaponsForTeamAndRound(team, roundType).FirstOrDefault();
        Assert.That(setWeapon, Is.EqualTo(expectedItem));

        if (removeMessage != null)
        {
            result = OnWeaponCommandHelper.Handle(args, 1, team, true, out _);
            Assert.That(result, Does.Contain(removeMessage));

            setWeapon = Queries.GetUserSettings(1)?.GetWeaponPreference(team, roundType);
            Assert.That(setWeapon, Is.EqualTo(null));
        }
    }

    [Test]
    [TestCase("T", "galil", CsItem.Galil, "Galil' is now")]
    [TestCase("T", "krieg", CsItem.Krieg, "SG553' is now")]
    [TestCase("T", "mac10", CsItem.Mac10, "Mac10' is now")]
    [TestCase("CT", "deag", CsItem.Deagle, "Deagle' is now")]
    [TestCase("CT", "galil", null, "Galil' is not valid")]
    [TestCase("CT", "tec9", null, "Tec9' is not valid")]
    [TestCase("T", "poop", null, "not found")]
    public void SetWeaponPreferenceCommandMultiArg(
        string teamInput, string itemInput,
        CsItem? expectedItem,
        string message
    )
    {
        var args = new List<string> {itemInput, teamInput};

        var result = OnWeaponCommandHelper.Handle(args, 1, CsTeam.None, false, out var selectedItem);

        Assert.That(result, Does.Contain(message));
        Assert.That(selectedItem, Is.EqualTo(expectedItem));

        var roundType = expectedItem != null
            ? WeaponHelpers.GetRoundTypeForWeapon(expectedItem.Value) ?? RoundType.Pistol
            : RoundType.Pistol;

        var setWeapon = Queries.GetUserSettings(1)?.GetWeaponPreference(Utils.ParseTeam(teamInput), roundType);
        Assert.That(setWeapon, Is.EqualTo(expectedItem));
    }
}
