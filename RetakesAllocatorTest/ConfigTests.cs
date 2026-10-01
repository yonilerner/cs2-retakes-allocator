using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;

namespace RetakesAllocatorTest;

public class ConfigTests : BaseTestFixture
{
    [TestCase(false)]
    [TestCase(true)]
    public void SavedConfigExcludesRandomWeaponSelection(bool existingConfig)
    {
        var modulePath = Path.Combine(Path.GetTempPath(), "RetakesAllocatorConfig-" + Guid.NewGuid().ToString("N"));
        var configDirectory = Path.Combine(modulePath, "config");
        var configPath = Path.Combine(configDirectory, "config.json");
        try
        {
            if (existingConfig)
            {
                Directory.CreateDirectory(configDirectory);
                File.WriteAllText(configPath,
                    "{\"AllowedWeaponSelectionTypes\":[\"PlayerChoice\",\"Random\",\"Default\"],\"ChatMessagePluginName\":\"CustomRetakes\"}");
            }

            Configs.Load(modulePath, saveAfterLoad: true);
            using var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(configPath));
            var selections = json.RootElement.GetProperty("AllowedWeaponSelectionTypes")
                .EnumerateArray().Select(value => value.GetString()).ToArray();
            Assert.That(selections, Is.EqualTo(new[] {"PlayerChoice", "Default"}));
            if (existingConfig)
            {
                Assert.That(json.RootElement.GetProperty("ChatMessagePluginName").GetString(), Is.EqualTo("CustomRetakes"));
            }
        }
        finally
        {
            if (Directory.Exists(modulePath))
            {
                Directory.Delete(modulePath, recursive: true);
            }
        }
    }

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
        defaults[CsTeam.Terrorist].Remove(WeaponAllocationType.FullBuyPrimary);
        warnings = Configs.OverrideConfigDataForTests(
            new ConfigData()
            {
                DefaultWeapons = defaults
            }
        ).Validate();
        Assert.That(warnings[0], Is.EqualTo("Missing FullBuyPrimary in DefaultWeapons.Terrorist config."));

        defaults.Remove(CsTeam.CounterTerrorist);
        warnings = Configs.OverrideConfigDataForTests(
            new ConfigData()
            {
                DefaultWeapons = defaults
            }
        ).Validate();
        Assert.That(warnings[0], Is.EqualTo("Missing FullBuyPrimary in DefaultWeapons.Terrorist config."));
        Assert.That(warnings[1], Is.EqualTo("Missing CounterTerrorist in DefaultWeapons config."));

        defaults[CsTeam.Terrorist][WeaponAllocationType.FullBuyPrimary] = CsItem.Kevlar;
        var error = Assert.Catch(() =>
        {
            Configs.OverrideConfigDataForTests(
                new ConfigData()
                {
                    DefaultWeapons = defaults
                }
            );
        });
        Assert.That(error?.Message,
            Is.EqualTo("Kevlar is not a valid weapon in config DefaultWeapons.Terrorist.FullBuyPrimary."));

        defaults =
            new Dictionary<CsTeam, Dictionary<WeaponAllocationType, CsItem>>(Configs.GetConfigData().DefaultWeapons);
        defaults[CsTeam.Terrorist][WeaponAllocationType.Preferred] = CsItem.AWP;
        error = Assert.Catch(() =>
        {
            Configs.OverrideConfigDataForTests(
                new ConfigData()
                {
                    DefaultWeapons = defaults
                }
            );
        });
        Assert.That(error?.Message, Is.EqualTo(
            "Preferred is not a valid default weapon allocation type for config DefaultWeapons.Terrorist."
        ));
    }
}
