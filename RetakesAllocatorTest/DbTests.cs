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

        Queries.SetWeaponPreferenceForUser(1, CsTeam.Terrorist, WeaponAllocationType.HalfBuyPrimary, CsItem.Bizon);
        Queries.SetWeaponPreferenceForUser(1, CsTeam.Terrorist, WeaponAllocationType.PistolRound, null);
        Queries.SetWeaponPreferenceForUser(1, CsTeam.Terrorist, WeaponAllocationType.HalfBuyPrimary, CsItem.MP5);
        Queries.SetWeaponPreferenceForUser(1, CsTeam.CounterTerrorist, WeaponAllocationType.FullBuyPrimary, CsItem.AK47);
        // Should set for both T and CT
        Queries.SetPreferredWeaponPreference(1, CsItem.AWP);
        
        Queries.SetWeaponPreferenceForUser(2, CsTeam.Terrorist, WeaponAllocationType.FullBuyPrimary, CsItem.AK47);
        Queries.SetWeaponPreferenceForUser(2, CsTeam.Terrorist, WeaponAllocationType.Secondary, CsItem.Deagle);
        Queries.SetWeaponPreferenceForUser(2, CsTeam.CounterTerrorist, WeaponAllocationType.Secondary, CsItem.FiveSeven);
        // Will get different snipers for different teams
        Queries.SetPreferredWeaponPreference(2, CsItem.SCAR20);

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

            Assert.That(usersSettings[1].GetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.HalfBuyPrimary),
                Is.EqualTo(CsItem.MP5));
            Assert.That(usersSettings[1].GetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.PistolRound),
                Is.EqualTo(null));
            Assert.That(usersSettings[1].GetWeaponPreference(CsTeam.CounterTerrorist, WeaponAllocationType.FullBuyPrimary),
                Is.EqualTo(CsItem.AK47));
            Assert.That(usersSettings[1].GetWeaponPreference(CsTeam.CounterTerrorist, WeaponAllocationType.HalfBuyPrimary),
                Is.EqualTo(null));
            Assert.That(usersSettings[1].GetWeaponPreference(CsTeam.CounterTerrorist, WeaponAllocationType.Preferred),
                Is.EqualTo(CsItem.AWP));
            Assert.That(usersSettings[1].GetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.Preferred),
                Is.EqualTo(CsItem.AWP));

            Assert.That(usersSettings[2].GetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.FullBuyPrimary),
                Is.EqualTo(CsItem.AK47));
            Assert.That(usersSettings[2].GetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.Secondary),
                Is.EqualTo(CsItem.Deagle));
            Assert.That(usersSettings[2].GetWeaponPreference(CsTeam.CounterTerrorist, WeaponAllocationType.Secondary),
                Is.EqualTo(CsItem.FiveSeven));
            Assert.That(usersSettings[2].GetWeaponPreference(CsTeam.Terrorist, WeaponAllocationType.Preferred),
                Is.EqualTo(CsItem.AutoSniperT));
            Assert.That(usersSettings[2].GetWeaponPreference(CsTeam.CounterTerrorist, WeaponAllocationType.Preferred),
                Is.EqualTo(CsItem.AutoSniperCT));
            
        });
    }
}
