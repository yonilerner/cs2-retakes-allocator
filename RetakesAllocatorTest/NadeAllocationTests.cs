using System.Collections;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;

namespace RetakesAllocatorTest;

public class NadeAllocationTests : BaseTestFixture
{
    [Test]
    public void TestGetUtilForTeam()
    {
        var util = NadeHelpers.GetUtilForTeam("de_mirage", RoundType.Pistol, CsTeam.Terrorist, 4);
        Assert.That(util.Count, Is.EqualTo(4));

        util = NadeHelpers.GetUtilForTeam(null, RoundType.Pistol, CsTeam.CounterTerrorist, 0);
        Assert.That(util.Count, Is.EqualTo(0));
    }

    [Test]
    public void TestAllocateNadesToPlayers()
    {
        var util = NadeHelpers.GetUtilForTeam(null, RoundType.Pistol, CsTeam.Terrorist, 4);
        Dictionary<int, ICollection<CsItem>> nadesByPlayer = new();
        NadeHelpers.AllocateNadesToPlayers(new Stack<CsItem>(util), new List<int> {1, 2, 3, 4}, nadesByPlayer);
        Assert.That(util, Is.EquivalentTo(nadesByPlayer.Values.SelectMany(x => x)));
        
        util = NadeHelpers.GetUtilForTeam("de_dust2", RoundType.Pistol, CsTeam.CounterTerrorist, 0);
        nadesByPlayer = new();
        NadeHelpers.AllocateNadesToPlayers(new Stack<CsItem>(util), new List<int>(), nadesByPlayer);
        Assert.That(util, Is.EquivalentTo(nadesByPlayer.Values.SelectMany(x => x)));
    }
}