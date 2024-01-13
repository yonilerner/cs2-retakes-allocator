using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Db;

namespace RetakesAllocatorTest;

public class DbTests : BaseTestFixture
{
    [Test]
    public void TestGetUsersSettings()
    {
        var usersSettings = Queries.GetUsersSettings(new List<ulong>());
        Assert.That(usersSettings, Is.EqualTo(new Dictionary<ulong, UserSetting>()));

        // TODO Add secondary and sniper allocation types
        Queries.SetWeaponPreferenceForUser(1, CsTeam.Terrorist, WeaponAllocationType.HalfBuyPrimary, CsItem.Bizon);
        Queries.SetWeaponPreferenceForUser(1, CsTeam.Terrorist, WeaponAllocationType.PistolRound, null);
        Queries.SetWeaponPreferenceForUser(1, CsTeam.Terrorist, WeaponAllocationType.HalfBuyPrimary, CsItem.MP5);
        Queries.SetWeaponPreferenceForUser(1, CsTeam.CounterTerrorist, WeaponAllocationType.FullBuyPrimary, CsItem.AWP);
        Queries.SetWeaponPreferenceForUser(2, CsTeam.Terrorist, WeaponAllocationType.FullBuyPrimary, CsItem.AK47);

        usersSettings = Queries.GetUsersSettings(new List<ulong> {1});
        Assert.Multiple(() =>
        {
            Assert.That(usersSettings.Keys, Is.EquivalentTo(new List<ulong> {1}));
            Assert.That(usersSettings.Values.Select(v => v.UserId), Is.EquivalentTo(new List<ulong> {1}));
        });
        usersSettings = Queries.GetUsersSettings(new List<ulong> {2});
        Assert.Multiple(() =>
        {
            Assert.That(usersSettings.Keys, Is.EquivalentTo(new List<ulong> {2}));
            Assert.That(usersSettings.Values.Select(v => v.UserId), Is.EquivalentTo(new List<ulong> {2}));
        });
        usersSettings = Queries.GetUsersSettings(new List<ulong> {1, 2});
        Assert.Multiple(() =>
        {
            Assert.That(usersSettings.Keys, Is.EquivalentTo(new List<ulong> {1, 2}));
            Assert.That(usersSettings.Values.Select(v => v.UserId), Is.EquivalentTo(new List<ulong> {1, 2}));

            Assert.That(usersSettings[1].WeaponPreferences[CsTeam.Terrorist][WeaponAllocationType.HalfBuyPrimary],
                Is.EqualTo(CsItem.MP5));
            Assert.That(usersSettings[1].WeaponPreferences[CsTeam.Terrorist].TryGetValue(WeaponAllocationType.PistolRound, out _),
                Is.EqualTo(false));
            Assert.That(usersSettings[1].WeaponPreferences[CsTeam.CounterTerrorist][WeaponAllocationType.FullBuyPrimary],
                Is.EqualTo(CsItem.AWP));
            Assert.That(
                usersSettings[1].WeaponPreferences[CsTeam.CounterTerrorist].TryGetValue(WeaponAllocationType.HalfBuyPrimary, out _),
                Is.EqualTo(false));
            Assert.That(usersSettings[2].WeaponPreferences[CsTeam.Terrorist][WeaponAllocationType.FullBuyPrimary],
                Is.EqualTo(CsItem.AK47));
        });
    }
}
