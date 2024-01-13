using CounterStrikeSharp.API.Core;
using static RetakesAllocatorCore.PluginInfo;

namespace RetakesAllocator.Menus;

public class MenuManager
{
    public const float DefaultMenuTimeout = 30.0f;
    
    private readonly GunsMenu _gunsMenu = new();
    private readonly NextRoundMenu _nextRoundMenu = new();
    
    public void OpenGunsMenu(CCSPlayerController player)
    {
        if (IsUserInMenu(player))
        {
            player.PrintToChat($"{MessagePrefix}You are already using another menu!");
            return;
        }
        
        _gunsMenu.OpenGunsMenu(player);
    }
    
    public void OpenNextRoundMenu(CCSPlayerController player)
    {
        if (IsUserInMenu(player))
        {
            player.PrintToChat($"{MessagePrefix}You are already using another menu!");
            return;
        }
        
        _nextRoundMenu.OpenNextRoundMenu(player);
    }
    
    private bool IsUserInMenu(CCSPlayerController player)
    {
        return
            _gunsMenu.PlayersInMenu.Contains(player) 
            || _nextRoundMenu.PlayersInMenu.Contains(player);
    }
}