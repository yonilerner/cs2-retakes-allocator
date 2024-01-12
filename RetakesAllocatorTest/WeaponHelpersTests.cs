using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;

namespace RetakesAllocatorTest;

public class WeaponHelpersTests
{
    [SetUp]
    public void Setup()
    {
        Configs.Load(".", false);
    }

    [Test]
    [TestCase(true, true, true)]
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(false, false, false)]
    public void TestIsWeaponAllocationAllowed(bool allowAfterFreezeTime, bool isFreezeTime, bool expected)
    {
        var configData = Configs.GetDefaultConfigData();
        configData.AllowAllocationAfterFreezeTime = allowAfterFreezeTime;
        Configs.OverrideConfigDataForTests(configData);

        var canAllocate = WeaponHelpers.IsWeaponAllocationAllowed(isFreezeTime);

        Assert.That(canAllocate, Is.EqualTo(expected));
    }
}
