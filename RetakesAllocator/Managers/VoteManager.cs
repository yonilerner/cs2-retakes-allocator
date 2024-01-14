using CounterStrikeSharp.API.Core;
using static RetakesAllocatorCore.PluginInfo;

namespace RetakesAllocator.Managers;

public class VoteManager
{
    protected const float VoteTimeout = 30.0f;
    
    private bool _isVoteInProgress;
    private readonly HashSet<CCSPlayerController> _votes = new();
    
    public void CastVote(CCSPlayerController player)
    {
        if (_isVoteInProgress)
        {
            player.PrintToChat($"{MessagePrefix}A vote is already in progress!");
            return;
        }
        
        _isVoteInProgress = true;
        _votes.Add(player);
        
        player.PrintToChat($"{MessagePrefix}A vote has been started! Type /vote to vote!");
    }
}
