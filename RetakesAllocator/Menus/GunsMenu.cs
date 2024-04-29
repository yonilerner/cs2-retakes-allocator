using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocator.Menus.Interfaces;
using RetakesAllocatorCore;
using static RetakesAllocatorCore.PluginInfo;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace RetakesAllocator.Menus;

public class GunsMenu : AbstractBaseMenu
{
    private readonly Dictionary<CCSPlayerController, Timer> _menuTimeoutTimers = new();

    private static void Print(CCSPlayerController player, string message)
    {
        Helpers.WriteNewlineDelimited(message, player.PrintToChat);
    }

    public override void OpenMenu(CCSPlayerController player)
    {
        if (Helpers.GetSteamId(player) == 0)
        {
            Print(player, Translator.Instance["guns_menu.invalid_steam_id"]);
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
        Print(player, Translator.Instance["menu.timeout", MenuTimeout]);

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
        Print(player, Translator.Instance["guns_menu.complete"]);

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

    private ChatMenu CreateMenu(CsTeam team, string weaponTypeString)
    {
        var teamString = Utils.TeamString(team);
        return new ChatMenu(
            $"{MessagePrefix}{Translator.Instance["guns_menu.select_weapon", teamString, weaponTypeString]}");
    }

    private void OpenTPrimaryMenu(CCSPlayerController player)
    {
        var menu = CreateMenu(CsTeam.Terrorist, Translator.Instance["weapon_type.primary"]);

        foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.FullBuyPrimary,
                     CsTeam.Terrorist))
        {
            menu.AddMenuOption(weapon.GetName(), OnTPrimarySelect);
        }

        menu.AddMenuOption(Translator.Instance["menu.exit"], OnSelectExit);

        MenuManager.OpenChatMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }

    private void OnTPrimarySelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInMenu.Contains(player))
        {
            return;
        }

        var weaponName = option.Text;

        Print(player, Translator.Instance[
            "guns_menu.weapon_selected",
            weaponName,
            Utils.TeamString(CsTeam.Terrorist),
            Translator.Instance["weapon_type.primary"]
        ]);

        HandlePreferenceSelection(player, CsTeam.Terrorist, weaponName);

        OpenTSecondaryMenu(player);
    }

    private void OpenTSecondaryMenu(CCSPlayerController player)
    {
        var menu = CreateMenu(CsTeam.Terrorist, Translator.Instance["weapon_type.secondary"]);

        foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.Secondary,
                     CsTeam.Terrorist))
        {
            menu.AddMenuOption(weapon.GetName(), OnTSecondarySelect);
        }

        menu.AddMenuOption(Translator.Instance["menu.exit"], OnSelectExit);

        MenuManager.OpenChatMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }

    private void OnTSecondarySelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInMenu.Contains(player))
        {
            return;
        }

        var weaponName = option.Text;

        Print(player, Translator.Instance[
            "guns_menu.weapon_selected",
            weaponName,
            Utils.TeamString(CsTeam.Terrorist),
            Translator.Instance["weapon_type.secondary"]
        ]);
        HandlePreferenceSelection(player, CsTeam.Terrorist, weaponName, RoundType.FullBuy);

        OpenCtPrimaryMenu(player);
    }

    private void OpenCtPrimaryMenu(CCSPlayerController player)
    {
        var menu = CreateMenu(CsTeam.CounterTerrorist, Translator.Instance["weapon_type.primary"]);

        foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.FullBuyPrimary,
                     CsTeam.CounterTerrorist))
        {
            menu.AddMenuOption(weapon.GetName(), OnCtPrimarySelect);
        }

        menu.AddMenuOption(Translator.Instance["menu.exit"], OnSelectExit);

        MenuManager.OpenChatMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }

    private void OnCtPrimarySelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInMenu.Contains(player))
        {
            return;
        }

        var weaponName = option.Text;

        Print(player, Translator.Instance[
            "guns_menu.weapon_selected",
            weaponName,
            Utils.TeamString(CsTeam.CounterTerrorist),
            Translator.Instance["weapon_type.primary"]
        ]);
        HandlePreferenceSelection(player, CsTeam.CounterTerrorist, weaponName);

        OpenCtSecondaryMenu(player);
    }

    private void OpenCtSecondaryMenu(CCSPlayerController player)
    {
        var menu = CreateMenu(CsTeam.CounterTerrorist, Translator.Instance["weapon_type.secondary"]);

        foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.Secondary,
                     CsTeam.CounterTerrorist))
        {
            menu.AddMenuOption(weapon.GetName(), OnCtSecondarySelect);
        }

        menu.AddMenuOption(Translator.Instance["menu.exit"], OnSelectExit);

        MenuManager.OpenChatMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }

    private void OnCtSecondarySelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInMenu.Contains(player))
        {
            return;
        }

        var weaponName = option.Text;

        Print(player, Translator.Instance[
            "guns_menu.weapon_selected",
            weaponName,
            Utils.TeamString(CsTeam.CounterTerrorist),
            Translator.Instance["weapon_type.secondary"]
        ]);
        HandlePreferenceSelection(player, CsTeam.CounterTerrorist, weaponName, RoundType.FullBuy);

        OpenTPistolMenu(player);
    }

    private void OpenTPistolMenu(CCSPlayerController player)
    {
        var menu = CreateMenu(CsTeam.Terrorist,
            Translator.Instance["announcement.roundtype", Translator.Instance["roundtype.Pistol"]]);

        foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.PistolRound,
                     CsTeam.Terrorist))
        {
            menu.AddMenuOption(weapon.GetName(), OnTPistolSelect);
        }

        menu.AddMenuOption(Translator.Instance["menu.exit"], OnSelectExit);

        MenuManager.OpenChatMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }

    private void OnTPistolSelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInMenu.Contains(player))
        {
            return;
        }

        var weaponName = option.Text;

        Print(player, Translator.Instance[
            "guns_menu.weapon_selected",
            weaponName,
            Utils.TeamString(CsTeam.Terrorist),
            Translator.Instance["announcement.roundtype", Translator.Instance["roundtype.Pistol"]]
        ]);
        HandlePreferenceSelection(player, CsTeam.Terrorist, weaponName);

        OpenCtPistolMenu(player);
    }

    private void OpenCtPistolMenu(CCSPlayerController player)
    {
        var menu = CreateMenu(CsTeam.CounterTerrorist,
            Translator.Instance["announcement.roundtype", Translator.Instance["roundtype.Pistol"]]);

        foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.PistolRound,
                     CsTeam.CounterTerrorist))
        {
            menu.AddMenuOption(weapon.GetName(), OnCtPistolSelect);
        }

        menu.AddMenuOption(Translator.Instance["menu.exit"], OnSelectExit);

        MenuManager.OpenChatMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }

    private void OnCtPistolSelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInMenu.Contains(player))
        {
            return;
        }

        var weaponName = option.Text;

        Print(player, Translator.Instance[
            "guns_menu.weapon_selected",
            weaponName,
            Utils.TeamString(CsTeam.CounterTerrorist),
            Translator.Instance["announcement.roundtype", Translator.Instance["roundtype.Pistol"]]
        ]);
        HandlePreferenceSelection(player, CsTeam.CounterTerrorist, weaponName);

        OpenTHalfBuyMenu(player);
    }

    private void OpenTHalfBuyMenu(CCSPlayerController player)
    {
        var menu = CreateMenu(CsTeam.Terrorist, Translator.Instance["roundtype.HalfBuy"]);

        foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.HalfBuyPrimary,
                     CsTeam.Terrorist))
        {
            menu.AddMenuOption(weapon.GetName(), OnTHalfBuySelect);
        }

        menu.AddMenuOption(Translator.Instance["menu.exit"], OnSelectExit);

        MenuManager.OpenChatMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }

    private void OnTHalfBuySelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInMenu.Contains(player))
        {
            return;
        }

        var weaponName = option.Text;

        Print(player, Translator.Instance[
            "guns_menu.weapon_selected",
            weaponName,
            Utils.TeamString(CsTeam.Terrorist),
            Translator.Instance["roundtype.HalfBuy"]
        ]);
        HandlePreferenceSelection(player, CsTeam.Terrorist, weaponName);

        OpenCTHalfBuyMenu(player);
    }

    private void OpenCTHalfBuyMenu(CCSPlayerController player)
    {
        var menu = CreateMenu(CsTeam.CounterTerrorist, Translator.Instance["roundtype.HalfBuy"]);

        foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.HalfBuyPrimary,
                     CsTeam.CounterTerrorist))
        {
            menu.AddMenuOption(weapon.GetName(), OnCTHalfBuySelect);
        }

        menu.AddMenuOption(Translator.Instance["menu.exit"], OnSelectExit);

        MenuManager.OpenChatMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }

    private void OnCTHalfBuySelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInMenu.Contains(player))
        {
            return;
        }

        var weaponName = option.Text;

        Print(player, Translator.Instance[
            "guns_menu.weapon_selected",
            weaponName,
            Utils.TeamString(CsTeam.CounterTerrorist),
            Translator.Instance["roundtype.HalfBuy"]
        ]);
        HandlePreferenceSelection(player, CsTeam.CounterTerrorist, weaponName);

        OpenGiveAwpMenu(player);
    }

    private string AwpNeverOption => Translator.Instance["guns_menu.awp_never"];
    private string AwpMyTurnOption => Translator.Instance["guns_menu.awp_always"];

    private void OpenGiveAwpMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu($"{MessagePrefix}{Translator.Instance["guns_menu.awp_menu"]}");

        menu.AddMenuOption(AwpNeverOption, OnGiveAwpSelect);
        // Implementing "Sometimes" will require a more complex AWP queue
        // menu.AddMenuOption("Sometimes", OnGiveAwpSelect);
        menu.AddMenuOption(AwpMyTurnOption, OnGiveAwpSelect);

        menu.AddMenuOption(Translator.Instance["menu.exit"], OnSelectExit);

        MenuManager.OpenChatMenu(player, menu);
        CreateMenuTimeoutTimer(player);
    }

    private void OnGiveAwpSelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (!PlayersInMenu.Contains(player))
        {
            return;
        }

        Print(
            player,
            Translator.Instance["guns_menu.awp_preference_selected", option.Text]
        );

        if (option.Text == AwpNeverOption)
        {
            // Team doesnt matter for AWP
            HandlePreferenceSelection(player, CsTeam.Terrorist, CsItem.AWP.ToString(), remove: true);
        }
        else if (option.Text == AwpMyTurnOption)
        {
            HandlePreferenceSelection(player, CsTeam.Terrorist, CsItem.AWP.ToString(), remove: false);
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
