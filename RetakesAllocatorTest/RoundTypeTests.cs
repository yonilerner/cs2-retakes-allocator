using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;

namespace RetakesAllocatorTest;

public class RoundTypeTests : BaseTestFixture
{
    [Test]
    [TestCase(10, .1f)]
    [TestCase(100, 1f)]
    [TestCase(33, .33f)]
    public void TestRoundPercentages(int configPercentage, double expectedPercentage)
    {
        Configs.OverrideConfigDataForTests(
            new ConfigData
            {
                RoundTypePercentages = new()
                {
                    {RoundType.Pistol, configPercentage},
                    {RoundType.HalfBuy, 0},
                    {RoundType.FullBuy, 100 - configPercentage}
                }
            }
        );

        expectedPercentage = Math.Round(expectedPercentage, 2);

        Assert.That(Configs.GetConfigData().GetRoundTypePercentage(RoundType.Pistol), Is.EqualTo(expectedPercentage));
    }
}
