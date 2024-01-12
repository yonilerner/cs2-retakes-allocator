namespace RetakesAllocatorTest;

public class WeaponHelpersTests
{
    [Test]
    [TestCase(true, true, true)]
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(false, false, false)]
    public void TestIsWeaponAllocationAllowed(bool allowAfterFreezeTime, bool isFreezeTime, bool expected)
    {
        
    }
}
