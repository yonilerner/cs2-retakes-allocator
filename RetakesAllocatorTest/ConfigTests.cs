using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;

namespace RetakesAllocatorTest;

public class ConfigTests : BaseTestFixture
{
    [Test]
    public void TestDefaultWeaponsValidation()
    {
        var usableWeapons = WeaponHelpers.AllWeapons;
        usableWeapons.Remove(CsItem.Glock);
        var warnings = Configs.OverrideConfigDataForTests(
            new ConfigData()
            {
                UsableWeapons = usableWeapons,
            }
        ).Validate();
        Assert.That(warnings[0],
            Is.EqualTo(
                "Glock18 in the DefaultWeapons.Terrorist.PistolRound " +
                "config is not in the UsableWeapons list."));

        var defaults =
            new Dictionary<CsTeam, Dictionary<WeaponAllocationType, CsItem>>(Configs.GetConfigData().DefaultWeapons);
        defaults[CsTeam.Terrorist] = new Dictionary<WeaponAllocationType, CsItem>(defaults[CsTeam.Terrorist]);
        defaults[CsTeam.Terrorist].Remove(WeaponAllocationType.Preferred);
        warnings = Configs.OverrideConfigDataForTests(
            new ConfigData()
            {
                DefaultWeapons = defaults
            }
        ).Validate();
        Assert.That(warnings[0], Is.EqualTo("Missing Preferred in DefaultWeapons.Terrorist config."));

        defaults.Remove(CsTeam.CounterTerrorist);
        warnings = Configs.OverrideConfigDataForTests(
            new ConfigData()
            {
                DefaultWeapons = defaults
            }
        ).Validate();
        Assert.That(warnings[0], Is.EqualTo("Missing Preferred in DefaultWeapons.Terrorist config."));
        Assert.That(warnings[1], Is.EqualTo("Missing CounterTerrorist in DefaultWeapons config."));
    }
}
