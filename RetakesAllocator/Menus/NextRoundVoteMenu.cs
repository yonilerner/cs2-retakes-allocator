using CounterStrikeSharp.API.Core;
using RetakesAllocator.Managers;
using RetakesAllocatorCore;

namespace RetakesAllocator.Menus;

public class NextRoundVoteMenu : AbstractVoteMenu<RoundType>
{
    public NextRoundVoteMenu() : base(new NextRoundVoteManager())
    {
    }
}
