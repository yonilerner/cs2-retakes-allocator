using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;

namespace RetakesAllocatorTest;

[SetUpFixture]
public class GlobalSetup
{
    [OneTimeSetUp]
    public void Setup()
    {
        Configs.Load(".", true);
        Queries.Migrate();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        Queries.Disconnect();
    }
}

public abstract class BaseTestFixture
{
    [SetUp]
    public void GlobalSetup()
    {
        Configs.Load(".");
        Queries.Wipe();
    }
}