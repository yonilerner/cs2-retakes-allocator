using CounterStrikeSharp.API.Core;
using RetakesAllocator.Menus;

namespace RetakesAllocator.Managers;

public class MenuManager
{
    public const float DefaultMenuTimeout = 30.0f;
    
    private GunsMenu _gunsMenu = new();
    private NextRoundMenu _nextRoundMenu = new();
    
    public void OpenWeaponsMenu(CCSPlayerController player)
    {
        _gunsMenu.OpenGunsMenu(player);
    }
    
    public void OpenNextRoundMenu(CCSPlayerController player)
    {
        _nextRoundMenu.OpenNextRoundMenu(player);
    }
    
    public bool IsUserInMenu(CCSPlayerController player)
    {
        return
            _gunsMenu.PlayersInMenu.Contains(player) 
            || _nextRoundMenu.PlayersInMenu.Contains(player);
    }
}