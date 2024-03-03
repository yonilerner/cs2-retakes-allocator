using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using RetakesAllocator.Managers;
using RetakesAllocator.Menus.Interfaces;
using static RetakesAllocatorCore.PluginInfo;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace RetakesAllocator.Menus;

public class VoteMenu : AbstractBaseMenu
{
    private new const float MenuTimeout = 20.0f;
    private readonly Dictionary<CCSPlayerController, Timer> _menuTimeoutTimers = new();
    private readonly AbstractVoteManager _voteManager;

    public VoteMenu(AbstractVoteManager voteManager)
    {
        _voteManager = voteManager;
    }

    public override void OpenMenu(CCSPlayerController player)
    {
        PlayersInMenu.Add(player);

        var menu = new ChatMenu($"{_voteManager.VoteMessagePrefix} Select your vote!");

        foreach (var option in _voteManager.GetVoteOptions())
        {
            menu.AddMenuOption(option, OnVoteSelect);
        }

        menu.AddMenuOption("Exit", OnSelectExit);

        ChatMenus.OpenMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }
    
    public void GatherAndHandleVotes()
    {
        _voteManager.CompleteVote();
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
