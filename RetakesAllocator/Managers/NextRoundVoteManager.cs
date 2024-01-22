using RetakesAllocatorCore;
using RetakesAllocatorCore.Managers;

namespace RetakesAllocator.Managers;

public class NextRoundVoteManager : AbstractVoteManager
{
    private readonly IEnumerable<string> _options = RoundTypeHelpers
        .GetRoundTypes()
        .Select(r => r.ToString());

    public NextRoundVoteManager() : base("the next round", "!nextround")
    {
    }


    public override IEnumerable<string> GetVoteOptions()
    {
        return _options;
    }

    protected override void HandleVoteResult(string option)
    {
        RoundTypeManager.Instance.SetNextRoundTypeOverride(RoundTypeHelpers.ParseRoundType(option));
        PrintToServer($"Vote complete! The next round will be {option}!");
    }
}
