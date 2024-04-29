using CounterStrikeSharp.API.Core;
using RetakesAllocator.Managers;
using RetakesAllocator.Menus.Interfaces;
using RetakesAllocatorCore;

namespace RetakesAllocator.Menus;

public enum MenuType
{
    Guns,
    NextRoundVote,
}

public class AllocatorMenuManager
{
    private readonly Dictionary<MenuType, AbstractBaseMenu> _menus = new()
    {
        {MenuType.Guns, new GunsMenu()},
        {MenuType.NextRoundVote, new VoteMenu(new NextRoundVoteManager())},
    };

    private bool MenuAlreadyOpenCheck(CCSPlayerController player)
    {
        if (IsUserInMenu(player))
        {
            Helpers.WriteNewlineDelimited(Translator.Instance["menu.already_in_menu"], player.PrintToChat);
            return true;
        }

        return false;
    }

    public T GetMenu<T>(MenuType menuType)
    where T : AbstractBaseMenu
    {
        return (T) _menus[menuType];
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
