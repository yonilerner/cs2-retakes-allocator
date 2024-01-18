using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using RetakesAllocatorCore;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace RetakesAllocator.Managers;

public abstract class AbstractVoteManager<TVoteValue> where TVoteValue : notnull
{
    protected const float VoteTimeout = 30.0f;
    protected const float EnoughPlayersVotedThreshold = 0.5f;

    private readonly string _voteFor;
    private readonly string _voteCommand;
    private Timer? _voteTimer;
    private readonly Dictionary<CCSPlayerController, TVoteValue> _votes = new();

    protected AbstractVoteManager(string voteFor, string voteCommand)
    {
        _voteFor = voteFor;
        _voteCommand = voteCommand;
    }

    public abstract IEnumerable<TVoteValue> GetVoteValues();
    protected abstract void HandleVoteResult(TVoteValue result);
    protected abstract TVoteValue ParseVoteValue(string voteValueStr);
    public abstract string SerializeVoteValue(TVoteValue voteValue);

    public void CastVote(CCSPlayerController player, string vote)
    {
        CastVote(player, ParseVoteValue(vote));
    }

    public void CastVote(CCSPlayerController player, TVoteValue vote)
    {
        if (_voteTimer == null)
        {
            _voteTimer = new Timer(VoteTimeout, OnVoteComplete);

            PrintToServer($"A vote has been started! Type {_voteCommand} to vote!");
        }

        _votes[player] = vote;
        PrintToPlayer(player, "Your vote has been registered!");

        if (_votes.Count == 0)
        {
            OnVoteComplete();
        }
    }

    public void ExecuteVoteEnd()
    {
        OnVoteComplete();
    }

    private void OnVoteComplete()
    {
        if (_voteTimer is null)
        {
            return;
        }
        _voteTimer.Kill();
        _voteTimer = null;

        var countedVotes = new Dictionary<TVoteValue, int>();

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
        var highestVoted = new HashSet<TVoteValue>();
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
        if (numPlayers == 0 || (float) highestScore / numPlayers < EnoughPlayersVotedThreshold)
        {
            PrintToServer("Vote failed: Not enough players voted!");
            return;
        }

        var random = new Random();
        var chosenVote = highestVoted.ElementAt(random.Next(highestVoted.Count));

        HandleVoteResult(chosenVote);
    }

    public string VoteMessagePrefix => $"{PluginInfo.MessagePrefix}[Voting for {_voteFor}] ";

    public void PrintToPlayer(CCSPlayerController player, string message)
    {
        player.PrintToChat($"{VoteMessagePrefix}{message}");
    }

    public void PrintToServer(string message)
    {
        Server.PrintToChatAll($"{VoteMessagePrefix}{message}");
    }
}
