using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;

namespace RetakesAllocatorTest;

public class RoundTypeTests
{
    [SetUp]
    public void Setup()
    {
        Configs.Load(".");
    }

    [Test]
    [TestCase(10, .1f)]
    [TestCase(100, 1f)]
    [TestCase(33, .33f)]
    public void TestRoundPercentages(int configPercentage, double expectedPercentage)
    {
        var config = Configs.GetDefaultConfigData();
        config.RoundTypePercentages[RoundType.Pistol] = configPercentage;
        Configs.OverrideConfigDataForTests(config);

        expectedPercentage = Math.Round(expectedPercentage, 2);

        Assert.That(Configs.GetConfigData().GetRoundTypePercentage(RoundType.Pistol), Is.EqualTo(expectedPercentage));
    }
}
