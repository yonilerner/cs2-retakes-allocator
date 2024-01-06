using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.EntityFrameworkCore;
using RetakesAllocatorCore;
using RetakesAllocatorCore.db;

namespace RetakesAllocatorTest;

public class WeaponSelectionTests
{
    [SetUp]
    public void Setup()
    {
        Db.Instance ??= new Db();
        Db.GetInstance().Database.Migrate();
        Db.GetInstance().UserSettings.ExecuteDelete();
    }

    [TearDown]
    public void TearDown()
    {
        Db.Instance?.Dispose();
        Db.Instance = null;
    }

    [Test]
    public void SetWeaponPreferenceDirectly()
    {
        var userSettings = new UserSetting
        {
            UserId = 1,
            WeaponPreferences = { }
        };
        Db.GetInstance().UserSettings.Add(userSettings);
        userSettings.SetWeaponPreference(CsTeam.Terrorist, RoundType.FullBuy, CsItem.Galil);
        Db.GetInstance().SaveChanges();

        userSettings = Queries.GetUserSettings(1);
        Assert.That(userSettings!.GetWeaponsForTeamAndRound(CsTeam.Terrorist, RoundType.FullBuy).ToList(),
            Does.Contain(CsItem.GalilAR));
    }

    [Test]
    [TestCase(CsTeam.Terrorist, "f", "galil", CsItem.Galil, "Galil' is now")]
    [TestCase(CsTeam.Terrorist, "H", "mac10", CsItem.Mac10, "Mac10' is now")]
    [TestCase(CsTeam.CounterTerrorist, "P", "deag", CsItem.Deagle, "Deagle' is now")]
    [TestCase(CsTeam.CounterTerrorist, "F", "galil", null, "Galil' is not valid")]
    [TestCase(CsTeam.CounterTerrorist, "P", "tec9", null, "Tec9' is not valid")]
    [TestCase(CsTeam.Terrorist, "P", "ak47", null, "AK47' is not valid")]
    [TestCase(CsTeam.Terrorist, "c", "ak47", null, "round type")]
    [TestCase(CsTeam.Terrorist, "F", "poop", null, "not found")]
    public void SetWeaponPreferenceCommand(CsTeam team, string roundTypeInput, string itemInput, CsItem? expectedItem,
        string message)
    {
        var args = new List<string> {roundTypeInput, itemInput};

        var result = OnWeaponCommandHelper.Handle(args, 1, team);

        Assert.That(result, Does.Contain(message));

        var setWeapon = Queries.GetUserSettings(1)?
            .GetWeaponsForTeamAndRound(team, RoundTypeHelpers.ParseRoundType(roundTypeInput)!.Value).FirstOrDefault();
        Assert.That(setWeapon, Is.EqualTo(expectedItem));
    }
}
