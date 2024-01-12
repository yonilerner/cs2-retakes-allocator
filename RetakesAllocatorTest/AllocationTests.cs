using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;

namespace RetakesAllocatorTest;

public class AllocationTests
{
    [SetUp]
    public void Setup()
    {
        Queries.Wipe();
        Configs.Load(".", false);
    }

    [Test]
    public void UseableWeaponsTest()
    {
        Queries.SetWeaponPreferenceForUser(1, CsTeam.Terrorist, RoundType.FullBuy, CsItem.Galil);

        var weapon = WeaponHelpers.GetWeaponForRoundType(RoundType.FullBuy, CsTeam.Terrorist, Queries.GetUserSettings(1));
        Assert.That(weapon, Is.EqualTo(CsItem.Galil));
        
        var configData = Configs.GetDefaultConfigData();
        configData.UsableWeapons = configData.UsableWeapons
            .Where(w => w != CsItem.Galil)
            .ToList();
        Configs.OverrideConfigDataForTests(configData);
        
        weapon = WeaponHelpers.GetWeaponForRoundType(RoundType.FullBuy, CsTeam.Terrorist, Queries.GetUserSettings(1));
        Assert.That(weapon, Is.EqualTo(CsItem.AK47));
    }
}
