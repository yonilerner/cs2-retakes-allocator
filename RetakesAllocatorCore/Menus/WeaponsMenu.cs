using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using static RetakesAllocatorCore.PluginInfo;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace RetakesAllocatorCore.Menus;

public class WeaponsMenu
{
    private const float DefaultMenuTimeout = 30.0f;
    
    public readonly HashSet<CCSPlayerController> PlayersInGunsMenu = new();
    private readonly Dictionary<CCSPlayerController, Timer> _menuTimeoutTimers = new();
    
    private void OnMenuTimeout(CCSPlayerController player)
    {
        player.PrintToChat($"{MessagePrefix}You did not interact with the menu in {DefaultMenuTimeout} seconds!");

        PlayersInGunsMenu.Remove(player);
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
        player.PrintToChat($"{MessagePrefix}You have finished setting up your weapons!");
        player.PrintToChat($"{MessagePrefix}The weapons you have selected will be given to you at the start of the next round!");
        
        PlayersInGunsMenu.Remove(player);
        _menuTimeoutTimers[player].Kill();
        _menuTimeoutTimers.Remove(player);
    }
    
    private void OnSelectExit(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInGunsMenu.Contains(player))
        {
            return;
        }

        OnMenuComplete(player);
    }

    public void OpenTPrimaryMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu($"{MessagePrefix} Select a T Primary Weapon");

        foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForRoundType(RoundType.FullBuy, CsTeam.Terrorist))
        {
            menu.AddMenuOption(weapon.ToString(), OnTPrimarySelect);
        }

        menu.AddMenuOption("Exit", OnSelectExit);

        ChatMenus.OpenMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }

    private void OnTPrimarySelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInGunsMenu.Contains(player))
        {
            return;
        }

        var weaponName = option.Text;
        
        player.PrintToChat($"{MessagePrefix} You selected {weaponName} as T Primary!");
        HandleAllocation(player, CsTeam.Terrorist, weaponName);
        
        OpenTSecondaryMenu(player);
    }
    
    private void OpenTSecondaryMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu($"{MessagePrefix} Select a T Secondary Weapon");

        foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForRoundType(RoundType.Pistol, CsTeam.Terrorist))
        {
            menu.AddMenuOption(weapon.ToString(), OnTSecondarySelect);
        }

        menu.AddMenuOption("Exit", OnSelectExit);

        ChatMenus.OpenMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }

    private void OnTSecondarySelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInGunsMenu.Contains(player))
        {
            return;
        }

        var weaponName = option.Text;
        
        // TODO: Separate allocation for CT pistol and T pistol
        player.PrintToChat($"{MessagePrefix} You selected {weaponName} as T Secondary!");
        HandleAllocation(player, CsTeam.Terrorist, weaponName);
        
        OpenCtPrimaryMenu(player);
    }

    public void OpenCtPrimaryMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu($"{MessagePrefix} Select a CT Primary Weapon");

        foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForRoundType(RoundType.FullBuy, CsTeam.CounterTerrorist))
        {
            menu.AddMenuOption(weapon.ToString(), OnCTPrimarySelect);
        }

        menu.AddMenuOption("Exit", OnSelectExit);

        ChatMenus.OpenMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }

    private void OnCTPrimarySelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInGunsMenu.Contains(player))
        {
            return;
        }
        
        var weaponName = option.Text;
        
        player.PrintToChat($"{MessagePrefix} You selected {weaponName} as CT Primary!");
        HandleAllocation(player, CsTeam.Terrorist, weaponName);

        OpenCtSecondaryMenu(player);
    }

    private void OpenCtSecondaryMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu($"{MessagePrefix} Select a CT Secondary Weapon");

        foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForRoundType(RoundType.Pistol, CsTeam.Terrorist))
        {
            menu.AddMenuOption(weapon.ToString(), OnCTSecondarySelect);
        }

        menu.AddMenuOption("Exit", OnSelectExit);

        ChatMenus.OpenMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }

    private void OnCTSecondarySelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInGunsMenu.Contains(player))
        {
            return;
        }

        var weaponName = option.Text;
        
        // TODO: Separate allocation for CT pistol and T pistol
        player.PrintToChat($"{MessagePrefix} You selected {weaponName} as CT Secondary!");
        HandleAllocation(player, CsTeam.Terrorist, weaponName);

        // OpenGiveAwpMenu(player);
        OnMenuComplete(player);
    }

    // private void OpenGiveAwpMenu(CCSPlayerController player)
    // {
    //     var menu = new ChatMenu($"{MessagePrefix} Select when to give the AWP");
    //
    //     menu.AddMenuOption("Never", OnGiveAwpSelect);
    //     menu.AddMenuOption("Sometimes", OnGiveAwpSelect);
    //     menu.AddMenuOption("Always", OnGiveAwpSelect);
    //
    //     menu.AddMenuOption("Exit", OnSelectExit);
    //
    //     ChatMenus.OpenMenu(player, menu);
    // }
    //
    // private void OnGiveAwpSelect(CCSPlayerController player, ChatMenuOption option)
    // {
    //     if (!PlayersInGunsMenu.Contains(player))
    //     {
    //         return;
    //     }
    //
    //     player.PrintToChat($"{MessagePrefix} You selected {option.Text} as when to give the AWP!");
    //
    //     switch (option.Text)
    //     {
    //         case "Never":
    //             TODO: Implement weapon allocation selection
    //             break;
    //         case "Sometimes":
    //             TODO: Implement weapon allocation selection
    //             break;
    //         case "Always":
    //             TODO: Implement weapon allocation selection
    //             break;
    //     }
    //
    //     player.PrintToChat($"{MessagePrefix} You have finished setting up your weapons!");
    //     player.PrintToChat($"{MessagePrefix} The weapons you have selected will be given to you at the start of the next round!");
    //
    //     PlayersInGunsMenu.Remove(player);
    // }

    private static void HandleAllocation(CCSPlayerController player, CsTeam team, string weapon)
    {
        OnWeaponCommandHelper.Handle(
            new List<string>{weapon},
            player.AuthorizedSteamID?.SteamId64 ?? 0,
            team,
            false,
            out _
        );
    }
}
