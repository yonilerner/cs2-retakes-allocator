using RetakesAllocatorCore;

namespace RetakesAllocator.Managers;

public class NextRoundVoteManager : AbstractVoteManager<RoundType>
{
    public NextRoundVoteManager() : base("the next round", "!nextround")
    {
    }

    protected override void HandleVoteResult(RoundType nextRoundType)
    {
        RoundTypeManager.GetInstance().SetNextRoundType(nextRoundType);
        PrintToServer($"Vote complete! The next round will be {nextRoundType.ToString()}!");
    }

    protected override RoundType ParseVoteValue(string voteValueStr)
    {
        var parsedRound = RoundTypeHelpers.ParseRoundType(voteValueStr);
        if (parsedRound is null)
        {
            throw new Exception($"Unable to parse {voteValueStr} as RoundType");
        }
        return parsedRound.Value;
    }

    public override string SerializeVoteValue(RoundType voteValue)
    {
        return voteValue.ToString();
    }

    public override IEnumerable<RoundType> GetVoteValues()
    {
        return RoundTypeHelpers.GetRoundTypes();
    }
}
