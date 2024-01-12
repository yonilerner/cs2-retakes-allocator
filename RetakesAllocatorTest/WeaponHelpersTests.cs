using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;

namespace RetakesAllocatorTest;

public class WeaponHelpersTests : BaseTestFixture
{
    [Test]
    [TestCase(true, true, true)]
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(false, false, false)]
    public void TestIsWeaponAllocationAllowed(bool allowAfterFreezeTime, bool isFreezeTime, bool expected)
    {
        Configs.OverrideConfigDataForTests(new ConfigData() {AllowAllocationAfterFreezeTime = allowAfterFreezeTime});

        var canAllocate = WeaponHelpers.IsWeaponAllocationAllowed(isFreezeTime);

        Assert.That(canAllocate, Is.EqualTo(expected));
    }
}
