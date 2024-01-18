using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using RetakesAllocator.Managers;
using RetakesAllocator.Menus.Interfaces;
using static RetakesAllocatorCore.PluginInfo;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace RetakesAllocator.Menus;

public class VoteMenu<TVoteValue> : BaseMenu where TVoteValue : notnull
{
    private new const float MenuTimeout = 20.0f;
    private readonly Dictionary<CCSPlayerController, Timer> _menuTimeoutTimers = new();
    private readonly AbstractVoteManager<TVoteValue> _voteManager;

    public VoteMenu(AbstractVoteManager<TVoteValue> voteManager)
    {
        _voteManager = voteManager;
    }

    public override void OpenMenu(CCSPlayerController player)
    {
        PlayersInMenu.Add(player);

        var menu = new ChatMenu($"{_voteManager.VoteMessagePrefix} Select your vote!");

        foreach (var voteValue in _voteManager.GetVoteValues())
        {
            menu.AddMenuOption(_voteManager.SerializeVoteValue(voteValue), OnVoteSelect);
        }

        menu.AddMenuOption("Exit", OnSelectExit);

        ChatMenus.OpenMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }
    
    public void GatherAndHandleVotes()
    {
        _voteManager.OnVoteComplete();
    }

    public override bool PlayerIsInMenu(CCSPlayerController player)
    {
        return PlayersInMenu.Contains(player);
    }

    private void OnMenuTimeout(CCSPlayerController player)
    {
        player.PrintToChat($"{MessagePrefix}You did not interact with the menu in {MenuTimeout} seconds!");

        PlayersInMenu.Remove(player);
        _menuTimeoutTimers[player].Kill();
        _menuTimeoutTimers.Remove(player);
    }

    private void CreateMenuTimeoutTimer(CCSPlayerController player)
    {
        if (_menuTimeoutTimers.TryGetValue(player, out var existingTimer))
        {
            existingTimer.Kill();
            _menuTimeoutTimers.Remove(player);
        }

        _menuTimeoutTimers[player] = new Timer(MenuTimeout, () => OnMenuTimeout(player));
    }

    private void OnMenuComplete(CCSPlayerController player)
    {
        PlayersInMenu.Remove(player);
        _menuTimeoutTimers[player].Kill();
        _menuTimeoutTimers.Remove(player);
    }

    private void OnSelectExit(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInMenu.Contains(player))
        {
            return;
        }

        OnMenuComplete(player);
    }


    private void OnVoteSelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInMenu.Contains(player))
        {
            return;
        }

        _voteManager.CastVote(player, option.Text);

        OnMenuComplete(player);
    }
}
