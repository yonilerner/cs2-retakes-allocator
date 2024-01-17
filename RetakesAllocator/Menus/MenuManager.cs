using CounterStrikeSharp.API.Core;
using RetakesAllocator.Menus.Interfaces;
using static RetakesAllocatorCore.PluginInfo;

namespace RetakesAllocator.Menus;

public enum MenuType
{
    Guns,
    NextRoundVote,
}

public class MenuManager
{
    private readonly Dictionary<MenuType, BaseMenu> _menus = new()
    {
        {MenuType.Guns, new GunsMenu()},
        {MenuType.NextRoundVote, new NextRoundVoteMenu()},
    };

    private bool MenuAlreadyOpenCheck(CCSPlayerController player)
    {
        if (IsUserInMenu(player))
        {
            player.PrintToChat($"{MessagePrefix}You are already using another menu!");
            return true;
        }

        return false;
    }

    public bool OpenMenuForPlayer(CCSPlayerController player, MenuType menuType)
    {
        if (MenuAlreadyOpenCheck(player))
        {
            return false;
        }

        if (!_menus.TryGetValue(menuType, out var menu))
        {
            return false;
        }

        menu.OpenMenu(player);
        return true;
    }

    private bool IsUserInMenu(CCSPlayerController player)
    {
        return _menus.Values.Any(menu => menu.PlayerIsInMenu(player));
    }
}
