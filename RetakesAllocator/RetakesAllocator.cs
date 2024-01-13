using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;
using RetakesAllocator.Menus;
using SQLitePCL;
using static RetakesAllocatorCore.PluginInfo;

namespace RetakesAllocator;

[MinimumApiVersion(142)]
public class RetakesAllocator : BasePlugin
{
    public override string ModuleName => "Retakes Allocator Plugin";
    public override string ModuleVersion => PluginInfo.Version;

    private RoundType? _nextRoundType;
    private RoundType? _currentRoundType;
    private MenuManager _menuManager = new();

    #region Setup

    public override void Load(bool hotReload)
    {
        Log.Write("Loaded");
        ResetState();
        Batteries.Init();

        RegisterListener<Listeners.OnMapStart>(mapName => { ResetState(); });
        AddCommandListener("say", OnPlayerChat, HookMode.Post);
        
        if (Configs.GetConfigData().MigrateOnStartup)
        {
            Queries.Migrate();
        }


        if (hotReload)
        {
            HandleHotReload();
        }
    }

    private void ResetState()
    {
        Configs.Load(ModuleDirectory, true);
        _nextRoundType = null;
        _currentRoundType = null;
    }

    private void HandleHotReload()
    {
        Server.ExecuteCommand($"map {Server.MapName}");
    }

    public override void Unload(bool hotReload)
    {
        Log.Write($"Unloaded");
        ResetState();
        Queries.Disconnect();
    }

    #endregion

    #region Commands
    private HookResult OnPlayerChat(CCSPlayerController? player, CommandInfo info)
    {
        if (!Helpers.PlayerIsValid(player))
        {
            return HookResult.Continue;
        }

        var message = info.ArgByIndex(1).ToLower();

        switch (message)
        {
            case "guns":
                HandleGunsCommand(player, info);
                break;
        }

        return HookResult.Continue;
    }
    
    [ConsoleCommand("css_guns")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnGunsCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        HandleGunsCommand(player, commandInfo);
    }
    
    private void HandleGunsCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!Helpers.PlayerIsValid(player))
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}This command can only be executed by a valid player.");
            return;
        }

        _menuManager.OpenGunsMenu(player!);
    }
    
    [ConsoleCommand("css_nextround", "Opens the menu to vote for the next round type.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnNextRoundCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!Helpers.PlayerIsValid(player))
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}This command can only be executed by a valid player.");
            return;
        }

        _menuManager.OpenNextRoundMenu(player!);
    }

    [ConsoleCommand("css_gun")]
    [CommandHelper(minArgs: 1, usage: "<gun> [T|CT]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnWeaponCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        HandleWeaponCommand(player, commandInfo);
    }

    private void HandleWeaponCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!Helpers.PlayerIsValid(player))
        {
            return;
        }

        var playerId = Helpers.GetSteamId(player);
        var currentTeam = (CsTeam) player!.TeamNum;

        var result = OnWeaponCommandHelper.Handle(
            Helpers.CommandInfoToArgList(commandInfo),
            playerId,
            currentTeam,
            false,
            out var selectedWeapon
        );
        if (result is not null)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}{result}");
        }

        if (Helpers.IsWeaponAllocationAllowed() && selectedWeapon is not null)
        {
            var selectedWeaponRoundType = WeaponHelpers.GetRoundTypeForWeapon(selectedWeapon.Value);
            if (selectedWeaponRoundType == RoundType.Pistol || selectedWeaponRoundType == _currentRoundType)
            {
                Helpers.RemoveWeapons(
                    player,
                    item => WeaponHelpers.GetRoundTypeForWeapon(item) == selectedWeaponRoundType
                );
                var slot = selectedWeaponRoundType == RoundType.Pistol ? "slot2" : "slot1";
                AllocateItemsForPlayer(player, new List<CsItem> {selectedWeapon.Value}, slot);
            }
        }
    }

    [ConsoleCommand("css_removegun")]
    [CommandHelper(minArgs: 1, usage: "<gun> [T|CT]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnRemoveWeaponCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!Helpers.PlayerIsValid(player))
        {
            return;
        }

        var playerId = Helpers.GetSteamId(player);
        var currentTeam = (CsTeam) player!.TeamNum;

        var result = OnWeaponCommandHelper.Handle(
            Helpers.CommandInfoToArgList(commandInfo),
            playerId,
            currentTeam,
            true,
            out _
        );
        if (result is not null)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}{result}");
        }
    }

    [ConsoleCommand("css_setnextround", "Sets the next round type.")]
    [CommandHelper(minArgs: 1, usage: "<P/H/F>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnSetNextRoundCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var roundTypeInput = commandInfo.GetArg(1).ToLower();
        var roundType = RoundTypeHelpers.ParseRoundType(roundTypeInput);
        if (roundType is null)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}Invalid round type: {roundTypeInput}.");
        }
        else
        {
            _nextRoundType = roundType;
            commandInfo.ReplyToCommand($"{MessagePrefix}Next round will be a {roundType} round.");
        }
    }

    [ConsoleCommand("css_reload_allocator_config", "Reloads the cs2-retakes-allocator config.")]
    [RequiresPermissions("@css/root")]
    public void OnReloadAllocatorConfigCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        commandInfo.ReplyToCommand($"{MessagePrefix}Reloading config for version {ModuleVersion}");
        Configs.Load(ModuleDirectory);
    }

    #endregion

    #region Events

    [GameEventHandler]
    public HookResult OnPostItemPurchase(EventItemPurchase @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (Helpers.IsWarmup() || !Helpers.PlayerIsValid(player) || !player.PlayerPawn.IsValid)
        {
            return HookResult.Continue;
        }

        var item = Utils.ToEnum<CsItem>(@event.Weapon);
        var team = (CsTeam) player.TeamNum;
        var playerId = Helpers.GetSteamId(player);
        var weaponRoundType = WeaponHelpers.GetRoundTypeForWeapon(item);

        // Log.Write($"item {item} team {team} player {playerId}");
        // Log.Write($"curRound {_currentRoundType} weapon Round {weaponRoundType}");

        if (
            Helpers.IsWeaponAllocationAllowed() &&
            weaponRoundType is not null &&
            (weaponRoundType == _currentRoundType || weaponRoundType == RoundType.Pistol)
        )
        {
            Queries.SetWeaponPreferenceForUser(
                playerId,
                team,
                weaponRoundType.Value,
                item
            );
        }
        else
        {
            var removedAnyWeapons = Helpers.RemoveWeapons(player,
                i =>
                {
                    if (!WeaponHelpers.IsWeapon(i))
                    {
                        return i == item;
                    }

                    // Some weapons identify as other weapons, so we just remove them all
                    return WeaponHelpers.GetRoundTypeForWeapon(i) == weaponRoundType;
                });
            // Log.Write($"Removed {item}? {removedAnyWeapons}");

            var replacedWeapon = false;
            var slotToSelect = _currentRoundType == RoundType.Pistol ? "slot2" : "slot1";
            if (removedAnyWeapons && _currentRoundType is not null && WeaponHelpers.IsWeapon(item))
            {
                var replacementItem = WeaponHelpers.GetWeaponForRoundType(_currentRoundType.Value, team,
                    Queries.GetUserSettings(playerId));
                // Log.Write($"Replacement item: {replacementItem}");
                if (replacementItem is not null)
                {
                    replacedWeapon = true;
                    AllocateItemsForPlayer(player, new List<CsItem>
                    {
                        replacementItem.Value
                    }, slotToSelect);
                }
            }

            if (!replacedWeapon)
            {
                AddTimer(0.1f, () =>
                {
                    if (Helpers.PlayerIsValid(player) && player.UserId is not null)
                    {
                        NativeAPI.IssueClientCommand((int) player.UserId, slotToSelect);
                    }
                });
            }
        }

        var playerPos = player.PlayerPawn.Value?.AbsOrigin;

        var pEntity = new CEntityIdentity(EntitySystem.FirstActiveEntity);
        for (; pEntity is not null && pEntity.Handle != IntPtr.Zero; pEntity = pEntity.Next)
        {
            var p = Utilities.GetEntityFromIndex<CBasePlayerWeapon>((int) pEntity.EntityInstance.Index);
            if (
                !p.IsValid ||
                !p.DesignerName.StartsWith("weapon") ||
                p.DesignerName.Equals("weapon_c4") ||
                playerPos is null ||
                p.AbsOrigin is null
            )
            {
                continue;
            }

            var distance = Helpers.GetVectorDistance(playerPos, p.AbsOrigin);
            if (distance < 30)
            {
                AddTimer(.5f, () =>
                {
                    if (p.IsValid && !p.OwnerEntity.IsValid)
                    {
                        p.Remove();
                    }
                });
            }
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundPostStart(EventRoundPoststart @event, GameEventInfo info)
    {
        if (Helpers.IsWarmup())
        {
            return HookResult.Continue;
        }

        var allPlayers = Utilities.GetPlayers()
            .Where(Helpers.PlayerIsValid)
            .ToList();

        OnRoundPostStartHelper.Handle(
            _nextRoundType,
            allPlayers,
            Helpers.GetSteamId,
            Helpers.GetTeam,
            GiveDefuseKit,
            AllocateItemsForPlayer,
            out var currentRoundType
        );
        _currentRoundType = currentRoundType;
        _nextRoundType = null;

        if (Configs.GetConfigData().EnableRoundTypeAnnouncement)
        {
            Server.PrintToChatAll(
                $"{MessagePrefix}{Enum.GetName(_currentRoundType.Value)} Round"
            );
        }

        return HookResult.Continue;
    }

    #endregion

    #region Helpers

    private void AllocateItemsForPlayer(CCSPlayerController player, ICollection<CsItem> items, string? slotToSelect)
    {
        // Log.Write($"Allocating items: {string.Join(",", items)}");
        AddTimer(0.1f, () =>
        {
            if (!Helpers.PlayerIsValid(player))
            {
                // Log.Write($"Player is not valid when allocating item");
                return;
            }

            foreach (var item in items)
            {
                player.GiveNamedItem(item);
            }

            if (slotToSelect is not null)
            {
                AddTimer(0.1f, () =>
                {
                    if (Helpers.PlayerIsValid(player) && player.UserId is not null)
                    {
                        NativeAPI.IssueClientCommand((int) player.UserId, slotToSelect);
                    }
                });
            }
        });
    }

    private void GiveDefuseKit(CCSPlayerController player)
    {
        AddTimer(0.1f, () =>
        {
            if (player.PlayerPawn.Value?.ItemServices?.Handle is null || !Helpers.PlayerIsValid(player))
            {
                // Log.Write($"Player is not valid when giving defuse kit");
                return;
            }

            var itemServices = new CCSPlayer_ItemServices(player.PlayerPawn.Value.ItemServices.Handle);
            itemServices.HasDefuser = true;
        });
    }

    #endregion
}
