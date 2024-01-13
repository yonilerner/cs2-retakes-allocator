using CounterStrikeSharp.API.Core;

namespace RetakesAllocator.Menus.Interfaces;

public class BaseMenu
{
    protected const float MenuTimeout = 30.0f;
    
    public readonly HashSet<CCSPlayerController> PlayersInMenu = new();
}
