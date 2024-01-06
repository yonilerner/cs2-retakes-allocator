using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.EntityFrameworkCore;
using RetakesAllocatorCore;
using RetakesAllocatorCore.db;

namespace RetakesAllocatorTest;

public class Tests
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
    public void Test1()
    {
        var userSettings = new UserSetting
        {
            UserId = 1,
            WeaponPreferences = { }
        };
        Db.GetInstance().UserSettings.Add(userSettings);
        userSettings.SetWeaponPreference(CsTeam.Terrorist, RoundType.FullBuy, CsItem.Galil);
        Db.GetInstance().SaveChanges();

        userSettings = Db.GetInstance().UserSettings.First(u => u.UserId == 1);
        Assert.That(userSettings.GetWeaponsForTeamAndRound(CsTeam.Terrorist, RoundType.FullBuy).ToList(), Does.Contain(CsItem.GalilAR));
    }
}
