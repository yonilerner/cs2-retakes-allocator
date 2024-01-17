using CounterStrikeSharp.API.Core;

namespace RetakesAllocator.Menus.Interfaces;

public abstract class BaseMenu
{
    protected const float MenuTimeout = 30.0f;

    protected readonly HashSet<CCSPlayerController> PlayersInMenu = new();

    public abstract void OpenMenu(CCSPlayerController player);
    public abstract bool PlayerIsInMenu(CCSPlayerController player);
}
