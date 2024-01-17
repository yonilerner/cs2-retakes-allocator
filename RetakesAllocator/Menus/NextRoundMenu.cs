using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using RetakesAllocator.Managers;
using RetakesAllocator.Menus.Interfaces;
using RetakesAllocatorCore;
using static RetakesAllocatorCore.PluginInfo;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace RetakesAllocator.Menus;

public class NextRoundMenu : BaseMenu
{
    private const float DefaultMenuTimeout = 30.0f;
    
    private readonly Dictionary<CCSPlayerController, Timer> _menuTimeoutTimers = new();
    private readonly VoteManager _voteManager = new("the next round", "!nextround");
    
    private void OnMenuTimeout(CCSPlayerController player)
    {
        player.PrintToChat($"{MessagePrefix}You did not interact with the menu in {DefaultMenuTimeout} seconds!");

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
        _menuTimeoutTimers[player] = new Timer(DefaultMenuTimeout, () => OnMenuTimeout(player));
    }
    
    private void OnMenuComplete(CCSPlayerController player)
    {
        player.PrintToChat($"{MessagePrefix}Your vote has been submitted!");
        
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

    public void OpenNextRoundMenu(CCSPlayerController player)
    {
        PlayersInMenu.Add(player);
        
        var menu = new ChatMenu($"{MessagePrefix} Select your vote for the next round type!");

        foreach (var roundType in RoundTypeHelpers.GetRoundTypes())
        {
            menu.AddMenuOption(roundType.ToString(), OnNextRoundSelect);
        }

        menu.AddMenuOption("Exit", OnSelectExit);

        ChatMenus.OpenMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }

    private void OnNextRoundSelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInMenu.Contains(player))
        {
            return;
        }

        var selectedRound = option.Text;
        
        player.PrintToChat($"{MessagePrefix} You selected {selectedRound} as the next round!");
        _voteManager.CastVote(player, selectedRound);
        
        OnMenuComplete(player);
    }
}
