using CounterStrikeSharp.API.Core;
using RetakesAllocator.Menus;

namespace RetakesAllocator.Managers;

public class MenuManager
{
    public const float DefaultMenuTimeout = 30.0f;
    
    // Menus
    private WeaponsMenu _weaponsMenu = new();
    private NextRoundMenu _nextRoundMenu = new();
    
    public void OpenWeaponsMenu(CCSPlayerController player)
    {
        _weaponsMenu.OpenGunsMenu(player);
    }
    
    public void OpenNextRoundMenu(CCSPlayerController player)
    {
        _nextRoundMenu.OpenNextRoundMenu(player);
    }
}