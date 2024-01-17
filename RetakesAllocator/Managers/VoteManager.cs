using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static RetakesAllocatorCore.PluginInfo;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace RetakesAllocator.Managers;

public class VoteManager
{
    protected const float VoteTimeout = 30.0f;

    private string _voteFor;
    private string _voteCommand;
    private Timer? _voteTimer;
    private readonly Dictionary<CCSPlayerController, string> _votes = new();
    
    public VoteManager(string voteFor, string voteCommand)
    {
        _voteFor = voteFor;
        _voteCommand = voteCommand;
    }
    
    public void CastVote(CCSPlayerController player, string vote)
    {
        var players = Utilities.GetPlayers().Where(Helpers.PlayerIsValid);
        
        if (_voteTimer == null)
        {
            _voteTimer = new Timer(VoteTimeout, OnVoteComplete);

            foreach (var innerPlayer in players)
            {
                if (innerPlayer != player)
                {
                    innerPlayer.PrintToChat($"{MessagePrefix}A vote has been started! Type {_voteCommand} to vote for {_voteFor}!");
                }
            }
        }

        _votes[player] = vote;
        player.PrintToChat($"{MessagePrefix}Your vote has been registered for {_voteFor}!");
        
        // TODO: Check if all players have voted, and end the vote early.
        Utilities.GetPlayers()
            .Where(Helpers.PlayerIsValid)
            .ToList();
        
        if (_votes.Count == 0)
        {
            OnVoteComplete();
        }
    }

    private void OnVoteComplete()
    {
        if (_voteTimer != null)
        {
            _voteTimer.Kill();
            _voteTimer = null;
        }

        var countedVotes = new Dictionary<string, int>();
        
        foreach (var (player, vote) in _votes)
        {
            if (!player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected)
            {
                continue;
            }
            
            if (!countedVotes.TryAdd(vote, 1))
            {
                countedVotes[vote]++;
            }
        }

        _votes.Clear();

        var highestScore = 0;
        var highestVoted = new HashSet<string>();
        foreach (var (vote, count) in countedVotes)
        {
            if (count > highestScore)
            {
                highestScore = count;
                highestVoted.Clear();
                highestVoted.Add(vote);
            }
            else if (count == highestScore)
            {
                highestVoted.Add(vote);
            }
        }

        var numPlayers = Helpers.GetNumPlayersOnTeam();
        if (numPlayers == 0 || (float)highestScore / numPlayers < 0.5f)
        {
            Server.PrintToChatAll($"{MessagePrefix}Not enough players voted for {_voteFor}! Vote failed.");
            return;
        }
        
        var random = new Random();
        var chosenVote = highestVoted.ElementAt(random.Next(highestVoted.Count));

        var nextRoundType = RetakesAllocatorCore.RoundTypeHelpers.ParseRoundType(chosenVote);
        
        RoundTypeManager.GetInstance().SetNextRoundType(nextRoundType);
    }
}
