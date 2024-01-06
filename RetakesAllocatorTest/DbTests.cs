using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;
using RetakesAllocatorCore.db;

namespace RetakesAllocatorTest;

public class DbTests
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
    public void TestGetUsersSettings()
    {
        var usersSettings = Queries.GetUsersSettings(new List<ulong>());
        Assert.That(usersSettings, Is.EqualTo(new Dictionary<ulong, UserSetting>()));

        Queries.SetWeaponPreferenceForUser(1, CsTeam.Terrorist, RoundType.HalfBuy, CsItem.Bizon);
        Queries.SetWeaponPreferenceForUser(1, CsTeam.Terrorist, RoundType.Pistol, null);
        Queries.SetWeaponPreferenceForUser(1, CsTeam.Terrorist, RoundType.HalfBuy, CsItem.MP5);
        Queries.SetWeaponPreferenceForUser(1, CsTeam.CounterTerrorist, RoundType.FullBuy, CsItem.AWP);
        Queries.SetWeaponPreferenceForUser(2, CsTeam.Terrorist, RoundType.FullBuy, CsItem.AK47);

        usersSettings = Queries.GetUsersSettings(new List<ulong> {1});
        Assert.Multiple(() =>
        {
            Assert.That(usersSettings.Keys, Is.EquivalentTo(new List<ulong> { 1 }));
            Assert.That(usersSettings.Values.Select(v => v.UserId), Is.EquivalentTo(new List<ulong> { 1 }));
        });
        usersSettings = Queries.GetUsersSettings(new List<ulong> {2});
        Assert.Multiple(() =>
        {
            Assert.That(usersSettings.Keys, Is.EquivalentTo(new List<ulong> { 2 }));
            Assert.That(usersSettings.Values.Select(v => v.UserId), Is.EquivalentTo(new List<ulong> { 2 }));
        });
        usersSettings = Queries.GetUsersSettings(new List<ulong> {1, 2});
        Assert.Multiple(() =>
        {
            Assert.That(usersSettings.Keys, Is.EquivalentTo(new List<ulong> { 1, 2 }));
            Assert.That(usersSettings.Values.Select(v => v.UserId), Is.EquivalentTo(new List<ulong> { 1, 2 }));

            Assert.That(usersSettings[1].WeaponPreferences[CsTeam.Terrorist][RoundType.HalfBuy], Is.EqualTo(CsItem.MP5));
            Assert.That(usersSettings[1].WeaponPreferences[CsTeam.Terrorist].TryGetValue(RoundType.Pistol, out _),
                Is.EqualTo(false));
            Assert.That(usersSettings[1].WeaponPreferences[CsTeam.CounterTerrorist][RoundType.FullBuy],
                Is.EqualTo(CsItem.AWP));
            Assert.That(usersSettings[1].WeaponPreferences[CsTeam.CounterTerrorist].TryGetValue(RoundType.HalfBuy, out _),
                Is.EqualTo(false));
            Assert.That(usersSettings[2].WeaponPreferences[CsTeam.Terrorist][RoundType.FullBuy], Is.EqualTo(CsItem.AK47));
        });
    }
}
