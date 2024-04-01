using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocator.Menus.Interfaces;
using RetakesAllocatorCore;
using static RetakesAllocatorCore.PluginInfo;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace RetakesAllocator.Menus;

public class GunsMenu: AbstractBaseMenu
{
    private readonly Dictionary<CCSPlayerController, Timer> _menuTimeoutTimers = new();
    
    public override void OpenMenu(CCSPlayerController player)
    {
        if (Helpers.GetSteamId(player) == 0)
        {
            player.PrintToChat($"{MessagePrefix}You cannot set weapon preferences with invalid Steam ID.");
            return;
        }

        PlayersInMenu.Add(player);
        
        OpenTPrimaryMenu(player);
    }

    public override bool PlayerIsInMenu(CCSPlayerController player)
    {
        return PlayersInMenu.Contains(player);
    }

    private void OnMenuTimeout(CCSPlayerController player)
    {
        player.PrintToChat($"{MessagePrefix}You did not interact with the menu in {MenuTimeout} seconds!");

        PlayersInMenu.Remove(player);
        if (_menuTimeoutTimers.Remove(player, out var playerTimer))
        {
            playerTimer.Kill();
        }
    }

    private void CreateMenuTimeoutTimer(CCSPlayerController player)
    {
        if (_menuTimeoutTimers.Remove(player, out var existingTimer))
        {
            existingTimer.Kill();
        }

        _menuTimeoutTimers[player] = new Timer(MenuTimeout, () => OnMenuTimeout(player));
    }

    private void OnMenuComplete(CCSPlayerController player)
    {
        player.PrintToChat($"{MessagePrefix}You have finished setting up your weapons!");
        player.PrintToChat($"{MessagePrefix}The weapons you have selected will be given to you at the start of the next round!");

        PlayersInMenu.Remove(player);
        if (_menuTimeoutTimers.Remove(player, out var playerTimer))
        {
            playerTimer.Kill();
        }
    }

    private void OnSelectExit(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInMenu.Contains(player))
        {
            return;
        }

        OnMenuComplete(player);
    }

    private void OpenTPrimaryMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu($"{MessagePrefix} Select a T Primary Weapon");

        foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.FullBuyPrimary,
                     CsTeam.Terrorist))
        {
            menu.AddMenuOption(weapon.GetName(), OnTPrimarySelect);
        }

        menu.AddMenuOption("Exit", OnSelectExit);

        ChatMenus.OpenMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }

    private void OnTPrimarySelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInMenu.Contains(player))
        {
            return;
        }

        var weaponName = option.Text;

        player.PrintToChat($"{MessagePrefix} You selected {weaponName} as T Primary!");
        HandlePreferenceSelection(player, CsTeam.Terrorist, weaponName);

        OpenTSecondaryMenu(player);
    }

    private void OpenTSecondaryMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu($"{MessagePrefix} Select a T Secondary Weapon");

        foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.Secondary,
                     CsTeam.Terrorist))
        {
            menu.AddMenuOption(weapon.GetName(), OnTSecondarySelect);
        }

        menu.AddMenuOption("Exit", OnSelectExit);

        ChatMenus.OpenMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }

    private void OnTSecondarySelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInMenu.Contains(player))
        {
            return;
        }

        var weaponName = option.Text;

        player.PrintToChat($"{MessagePrefix} You selected {weaponName} as T Secondary!");
        HandlePreferenceSelection(player, CsTeam.Terrorist, weaponName, RoundType.FullBuy);

        OpenCtPrimaryMenu(player);
    }

    private void OpenCtPrimaryMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu($"{MessagePrefix} Select a CT Primary Weapon");

        foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.FullBuyPrimary,
                     CsTeam.CounterTerrorist))
        {
            menu.AddMenuOption(weapon.GetName(), OnCtPrimarySelect);
        }

        menu.AddMenuOption("Exit", OnSelectExit);

        ChatMenus.OpenMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }

    private void OnCtPrimarySelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInMenu.Contains(player))
        {
            return;
        }

        var weaponName = option.Text;

        player.PrintToChat($"{MessagePrefix} You selected {weaponName} as CT Primary!");
        HandlePreferenceSelection(player, CsTeam.CounterTerrorist, weaponName);

        OpenCtSecondaryMenu(player);
    }

    private void OpenCtSecondaryMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu($"{MessagePrefix} Select a CT Secondary Weapon");

        foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.Secondary,
                     CsTeam.CounterTerrorist))
        {
            menu.AddMenuOption(weapon.GetName(), OnCtSecondarySelect);
        }

        menu.AddMenuOption("Exit", OnSelectExit);

        ChatMenus.OpenMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }

    private void OnCtSecondarySelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInMenu.Contains(player))
        {
            return;
        }

        var weaponName = option.Text;

        player.PrintToChat($"{MessagePrefix} You selected {weaponName} as CT Secondary!");
        HandlePreferenceSelection(player, CsTeam.CounterTerrorist, weaponName, RoundType.FullBuy);

        OpenTPistolMenu(player);
    }

    private void OpenTPistolMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu($"{MessagePrefix} Select a T Pistol Round Weapon");

        foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.PistolRound,
                     CsTeam.Terrorist))
        {
            menu.AddMenuOption(weapon.GetName(), OnTPistolSelect);
        }

        menu.AddMenuOption("Exit", OnSelectExit);

        ChatMenus.OpenMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }

    private void OnTPistolSelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInMenu.Contains(player))
        {
            return;
        }

        var weaponName = option.Text;

        player.PrintToChat($"{MessagePrefix} You selected {weaponName} as T Pistol Round Weapon!");
        HandlePreferenceSelection(player, CsTeam.Terrorist, weaponName);

        OpenCtPistolMenu(player);
    }

    private void OpenCtPistolMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu($"{MessagePrefix} Select a CT Pistol Round Weapon");

        foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.PistolRound,
                     CsTeam.CounterTerrorist))
        {
            menu.AddMenuOption(weapon.GetName(), OnCtPistolSelect);
        }

        menu.AddMenuOption("Exit", OnSelectExit);

        ChatMenus.OpenMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }

    private void OnCtPistolSelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInMenu.Contains(player))
        {
            return;
        }

        var weaponName = option.Text;

        player.PrintToChat($"{MessagePrefix} You selected {weaponName} as CT Pistol Round Weapon!");
        HandlePreferenceSelection(player, CsTeam.CounterTerrorist, weaponName);

        OpenTHalfBuyMenu(player);
    }

    private void OpenTHalfBuyMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu($"{MessagePrefix} Select a T Half Buy Weapon");

        foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.HalfBuyPrimary,
                     CsTeam.Terrorist))
        {
            menu.AddMenuOption(weapon.GetName(), OnTHalfBuySelect);
        }

        menu.AddMenuOption("Exit", OnSelectExit);
        
        ChatMenus.OpenMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }
    
    private void OnTHalfBuySelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInMenu.Contains(player))
        {
            return;
        }

        var weaponName = option.Text;

        player.PrintToChat($"{MessagePrefix} You selected {weaponName} as T Half Buy Weapon!");
        HandlePreferenceSelection(player, CsTeam.Terrorist, weaponName);

        OpenCTHalfBuyMenu(player);
    }
    
    private void OpenCTHalfBuyMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu($"{MessagePrefix} Select a CT Half Buy Weapon");

        foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.HalfBuyPrimary,
                     CsTeam.CounterTerrorist))
        {
            menu.AddMenuOption(weapon.GetName(), OnCTHalfBuySelect);
        }

        menu.AddMenuOption("Exit", OnSelectExit);
        
        ChatMenus.OpenMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }
    
    private void OnCTHalfBuySelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInMenu.Contains(player))
        {
            return;
        }

        var weaponName = option.Text;

        player.PrintToChat($"{MessagePrefix} You selected {weaponName} as CT Half Buy Weapon!");
        HandlePreferenceSelection(player, CsTeam.CounterTerrorist, weaponName);

        OpenGiveAwpMenu(player);
    }

    private const string AwpNeverOption = "Never";
    private const string AwpMyTurnOption = "Always";

    private void OpenGiveAwpMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu($"{MessagePrefix} Select when to give the AWP");

        menu.AddMenuOption(AwpNeverOption, OnGiveAwpSelect);
        // Implementing "Sometimes" will require a more complex AWP queue
        // menu.AddMenuOption("Sometimes", OnGiveAwpSelect);
        menu.AddMenuOption(AwpMyTurnOption, OnGiveAwpSelect);

        menu.AddMenuOption("Exit", OnSelectExit);

        ChatMenus.OpenMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }

    private void OnGiveAwpSelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInMenu.Contains(player))
        {
            return;
        }

        player.PrintToChat($"{MessagePrefix} You selected '{option.Text}' as when to give the AWP!");

        switch (option.Text)
        {
            case AwpNeverOption:
                // Team doesnt matter for AWP
                HandlePreferenceSelection(player, CsTeam.Terrorist, CsItem.AWP.ToString(), remove: true);
                break;
            case AwpMyTurnOption:
                HandlePreferenceSelection(player, CsTeam.Terrorist, CsItem.AWP.ToString(), remove: false);
                break;
        }

        OnMenuComplete(player);
    }

    // TODO This is temporary until this menu knows about the current round
    public static void HandlePreferenceSelection(CCSPlayerController player, CsTeam team, string weapon,
        bool remove = false)
    {
        HandlePreferenceSelection(player, team, weapon, null, remove);
    }

    public static void HandlePreferenceSelection(CCSPlayerController player, CsTeam team, string weapon,
        RoundType? roundTypeOverride,
        bool remove = false)
    {
        var message = OnWeaponCommandHelper.Handle(
            new List<string> {weapon},
            Helpers.GetSteamId(player),
            roundTypeOverride,
            team,
            remove,
            out _
        );
        Log.Debug(message);
    }
}
