using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore.Managers;
using RetakesAllocator.Menus;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;
using SQLitePCL;
using static RetakesAllocatorCore.PluginInfo;

using RetakesPluginShared;
using RetakesPluginShared.Events;

namespace RetakesAllocator;

[MinimumApiVersion(180)]
public class RetakesAllocator : BasePlugin
{
    public override string ModuleName => "Retakes Allocator Plugin";
    public override string ModuleVersion => PluginInfo.Version;
    public override string ModuleAuthor => "Yoni Lerner";
    public override string ModuleDescription => "https://github.com/yonilerner/cs2-retakes-allocator";

    private readonly MenuManager _menuManager = new();
    private readonly Dictionary<CCSPlayerController, Dictionary<ItemSlotType, CsItem>> _allocatedPlayerItems = new();
    
    private IRetakesPluginEventSender? RetakesPluginEventSender { get; set; }

    #region Setup

    public override void Load(bool hotReload)
    {
        Log.Debug($"Loaded. Hot reload: {hotReload}");
        ResetState();
        Batteries.Init();

        RegisterListener<Listeners.OnMapStart>(mapName =>
        {
            ResetState();
            RoundTypeManager.Instance.SetMap(mapName);
        });
        AddCommandListener("say", OnPlayerChat, HookMode.Post);

        AddTimer(0.1f, () =>
        {
            GetRetakesPluginEventSender().RetakesPluginEventHandlers += RetakesEventHandler;
        });

        if (Configs.GetConfigData().MigrateOnStartup)
        {
            Queries.Migrate();
        }

        if (hotReload)
        {
            HandleHotReload();
        }
    }

    private void ResetState(bool loadConfig = true)
    {
        if (loadConfig)
        {
            Configs.Load(ModuleDirectory, true);
        }

        Translator.Initialize(Localizer);

        RoundTypeManager.Instance.SetNextRoundTypeOverride(null);
        RoundTypeManager.Instance.SetCurrentRoundType(null);
        RoundTypeManager.Instance.Initialize();

        _allocatedPlayerItems.Clear();
    }

    private void HandleHotReload()
    {
        Server.ExecuteCommand($"map {Server.MapName}");
    }

    public override void Unload(bool hotReload)
    {
        Log.Debug("Unloaded");
        ResetState(loadConfig: false);
        Queries.Disconnect();

        GetRetakesPluginEventSender().RetakesPluginEventHandlers -= RetakesEventHandler;
    }
    
    private IRetakesPluginEventSender GetRetakesPluginEventSender()
    {
        if (RetakesPluginEventSender is not null)
        {
            return RetakesPluginEventSender;
        }
        
        var sender = new PluginCapability<IRetakesPluginEventSender>("retakes_plugin:event_sender").Get();
        if (sender is null)
        {
            throw new Exception("Couldn't load retakes plugin event sender capability");
        }
        RetakesPluginEventSender = sender;
        return sender;
    }

    private void RetakesEventHandler(object? _, IRetakesPluginEvent @event)
    {
        Log.Trace("Got retakes event");
        Action? handler = @event switch
        {
            AllocateEvent => HandleAllocateEvent,
            _ => null
        };
        handler?.Invoke();
    }

    #endregion

    #region Commands

    private void RegisterCommands()
    {
    }

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

        _menuManager.OpenMenuForPlayer(player!, MenuType.Guns);
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

        if (!Configs.GetConfigData().EnableNextRoundTypeVoting)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}Next round voting is disabled.");
            return;
        }

        _menuManager.OpenMenuForPlayer(player!, MenuType.NextRoundVote);
    }

    [ConsoleCommand("css_gun")]
    [CommandHelper(usage: "<gun> [T|CT]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
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
        var currentTeam = player!.Team;

        var result = OnWeaponCommandHelper.Handle(
            Helpers.CommandInfoToArgList(commandInfo),
            playerId,
            RoundTypeManager.Instance.GetCurrentRoundType(),
            currentTeam,
            false,
            out var selectedWeapon
        );
        Helpers.WriteNewlineDelimited(result, l => commandInfo.ReplyToCommand(l));

        if (Helpers.IsWeaponAllocationAllowed() && selectedWeapon is not null)
        {
            var selectedWeaponAllocationType =
                WeaponHelpers.GetWeaponAllocationTypeForWeaponAndRound(RoundTypeManager.Instance.GetCurrentRoundType(),
                    currentTeam,
                    selectedWeapon.Value);
            if (selectedWeaponAllocationType is not null)
            {
                Helpers.RemoveWeapons(
                    player,
                    item =>
                        WeaponHelpers.GetWeaponAllocationTypeForWeaponAndRound(
                            RoundTypeManager.Instance.GetCurrentRoundType(), currentTeam, item) ==
                        selectedWeaponAllocationType
                );

                var slotType = WeaponHelpers.GetSlotTypeForItem(selectedWeapon.Value);
                var slot = WeaponHelpers.GetSlotNameForSlotType(slotType);
                AllocateItemsForPlayer(player, new List<CsItem> {selectedWeapon.Value}, slot);
            }
        }
    }

    [ConsoleCommand("css_awp", "Join or leave the AWP queue.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnAwpCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!Helpers.PlayerIsValid(player))
        {
            return;
        }

        var playerId = Helpers.GetSteamId(player);
        if (playerId == 0)
        {
            commandInfo.ReplyToCommand("Cannot save preferences with invalid Steam ID.");
            return;
        }
        var currentTeam = player!.Team;

        var currentPreferredSetting = Queries.GetUserSettings(playerId)
            ?.GetWeaponPreference(currentTeam, WeaponAllocationType.Preferred);

        var result = OnWeaponCommandHelper.Handle(
            new List<string> {CsItem.AWP.ToString()},
            playerId,
            RoundTypeManager.Instance.GetCurrentRoundType(),
            currentTeam,
            currentPreferredSetting is not null,
            out _
        );
        Helpers.WriteNewlineDelimited(result, l => commandInfo.ReplyToCommand(l));
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
        var currentTeam = player!.Team;

        var result = OnWeaponCommandHelper.Handle(
            Helpers.CommandInfoToArgList(commandInfo),
            playerId,
            RoundTypeManager.Instance.GetCurrentRoundType(),
            currentTeam,
            true,
            out _
        );
        commandInfo.ReplyToCommand($"{MessagePrefix}{result}");
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
            var message = Translator.Instance["announcement.next_roundtype_set_invalid", roundTypeInput];
            commandInfo.ReplyToCommand($"{MessagePrefix}{message}");
        }
        else
        {
            RoundTypeManager.Instance.SetNextRoundTypeOverride(roundType);
            var roundTypeName = RoundTypeHelpers.TranslateRoundTypeName(roundType.Value);
            var message = Translator.Instance["announcement.next_roundtype_set", roundTypeName];
            commandInfo.ReplyToCommand($"{MessagePrefix}{message}");
        }
    }

    [ConsoleCommand("css_reload_allocator_config", "Reloads the cs2-retakes-allocator config.")]
    [RequiresPermissions("@css/root")]
    public void OnReloadAllocatorConfigCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        commandInfo.ReplyToCommand($"{MessagePrefix}Reloading config for version {ModuleVersion}");
        Configs.Load(ModuleDirectory);
        RoundTypeManager.Instance.Initialize();
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
        var team = player.Team;
        var playerId = Helpers.GetSteamId(player);
        var isPreferred = WeaponHelpers.IsPreferred(team, item);

        var purchasedAllocationType = RoundTypeManager.Instance.GetCurrentRoundType() is not null
            ? WeaponHelpers.GetWeaponAllocationTypeForWeaponAndRound(
                RoundTypeManager.Instance.GetCurrentRoundType(), team, item
            )
            : null;

        var isValidAllocation = WeaponHelpers.IsAllocationTypeValidForRound(purchasedAllocationType,
            RoundTypeManager.Instance.GetCurrentRoundType());

        Log.Debug($"item {item} team {team} player {playerId}");
        Log.Debug($"weapon alloc {purchasedAllocationType} valid? {isValidAllocation}");
        Log.Debug($"Preferred? {isPreferred}");

        if (
            Helpers.IsWeaponAllocationAllowed() &&
            // Preferred weapons are treated like un-buy-able weapons, but at the end we'll set the user preference
            !isPreferred &&
            isValidAllocation &&
            // redundant, just for null checker
            purchasedAllocationType is not null
        )
        {
            Queries.SetWeaponPreferenceForUser(
                playerId,
                team,
                purchasedAllocationType.Value,
                item
            );
            var slotType = WeaponHelpers.GetSlotTypeForItem(item);
            if (slotType is not null)
            {
                SetPlayerRoundAllocation(player, slotType.Value, item);
            }
            else
            {
                Log.Debug($"WARN: No slot for {item}");
            }
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

                    if (RoundTypeManager.Instance.GetCurrentRoundType() is null)
                    {
                        return true;
                    }

                    var at = WeaponHelpers.GetWeaponAllocationTypeForWeaponAndRound(
                        RoundTypeManager.Instance.GetCurrentRoundType(), team, i);
                    Log.Trace($"at: {at}");
                    return at is null || at == purchasedAllocationType;
                });
            Log.Debug($"Removed {item}? {removedAnyWeapons}");

            var replacementSlot = RoundTypeManager.Instance.GetCurrentRoundType() == RoundType.Pistol
                ? ItemSlotType.Secondary
                : ItemSlotType.Primary;

            var replacedWeapon = false;
            var slotToSelect = WeaponHelpers.GetSlotNameForSlotType(replacementSlot);
            if (removedAnyWeapons && RoundTypeManager.Instance.GetCurrentRoundType() is not null &&
                WeaponHelpers.IsWeapon(item))
            {
                var replacementAllocationType =
                    WeaponHelpers.GetReplacementWeaponAllocationTypeForWeapon(RoundTypeManager.Instance
                        .GetCurrentRoundType());
                Log.Debug($"Replacement allocation type {replacementAllocationType}");
                if (replacementAllocationType is not null)
                {
                    var replacementItem = GetPlayerRoundAllocation(player, replacementSlot);
                    Log.Debug($"Replacement item {replacementItem} for slot {replacementSlot}");
                    if (replacementItem is not null)
                    {
                        replacedWeapon = true;
                        AllocateItemsForPlayer(player, new List<CsItem>
                        {
                            replacementItem.Value
                        }, slotToSelect);
                    }
                }
            }

            if (!replacedWeapon)
            {
                AddTimer(0.1f, () =>
                {
                    if (Helpers.PlayerIsValid(player) && player.UserId is not null)
                    {
                        NativeAPI.IssueClientCommand((int)player.UserId, slotToSelect);
                    }
                });
            }
        }

        var playerPos = player.PlayerPawn.Value?.AbsOrigin;

        var pEntity = new CEntityIdentity(EntitySystem.FirstActiveEntity);
        for (; pEntity is not null && pEntity.Handle != IntPtr.Zero; pEntity = pEntity.Next)
        {
            var p = Utilities.GetEntityFromIndex<CBasePlayerWeapon>((int)pEntity.EntityInstance.Index);
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
                        Log.Trace($"Removing {p.DesignerName}");
                        p.Remove();
                    }
                });
            }
        }

        if (isPreferred)
        {
            var itemName = Enum.GetName(item);
            if (itemName is not null)
            {
                var message = OnWeaponCommandHelper.Handle(
                    new List<string> {itemName},
                    Helpers.GetSteamId(player),
                    RoundTypeManager.Instance.GetCurrentRoundType(),
                    team,
                    false,
                    out _
                );
                Helpers.WriteNewlineDelimited(message, player.PrintToChat);
            }
        }

        return HookResult.Continue;
    }

    private void HandleAllocateEvent() {
        Log.Trace("Handling allocate event");
        Server.ExecuteCommand("mp_max_armor 0");

        var menu = _menuManager.GetMenu<VoteMenu>(MenuType.NextRoundVote);
        menu.GatherAndHandleVotes();

        var allPlayers = Utilities.GetPlayers()
            .Where(Helpers.PlayerIsValid)
            .ToList();

        OnRoundPostStartHelper.Handle(
            allPlayers,
            Helpers.GetSteamId,
            Helpers.GetTeam,
            GiveDefuseKit,
            AllocateItemsForPlayer,
            Helpers.IsVip,
            out var currentRoundType
        );
        RoundTypeManager.Instance.SetCurrentRoundType(currentRoundType);
        RoundTypeManager.Instance.SetNextRoundTypeOverride(null);

        if (Configs.GetConfigData().EnableRoundTypeAnnouncement)
        {
            var roundType = RoundTypeManager.Instance.GetCurrentRoundType()!.Value;
            var roundTypeName = RoundTypeHelpers.TranslateRoundTypeName(roundType);
            var message = Translator.Instance["announcement.roundtype", roundTypeName];
            Server.PrintToChatAll(
                $"{MessagePrefix}{message}"
            );
        }
    }

    #endregion

    #region Helpers

    private void SetPlayerRoundAllocation(CCSPlayerController player, ItemSlotType slotType, CsItem item)
    {
        if (!_allocatedPlayerItems.TryGetValue(player, out var playerAllocatedItems))
        {
            _allocatedPlayerItems[player] = new();
        }

        _allocatedPlayerItems[player][slotType] = item;
        Log.Debug($"Round allocation for player {player.Slot} {slotType} {item}");
    }

    private CsItem? GetPlayerRoundAllocation(CCSPlayerController player, ItemSlotType? slotType)
    {
        if (slotType is null || !_allocatedPlayerItems.TryGetValue(player, out var playerItems))
        {
            return null;
        }

        if (playerItems.TryGetValue(slotType.Value, out var localReplacementItem))
        {
            return localReplacementItem;
        }

        return null;
    }
    
    public MemoryFunctionVoid<IntPtr, string, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr> GiveNamedItem2 = new(@"\x55\x48\x89\xE5\x41\x57\x41\x56\x41\x55\x41\x54\x53\x48\x83\xEC\x18\x48\x89\x7D\xC8\x48\x85\xF6\x74");
    public void PlayerGiveNamedItem(CCSPlayerController player, string item)
    {
        if (!player.PlayerPawn.IsValid) return;
        if (player.PlayerPawn.Value == null) return;
        if (!player.PlayerPawn.Value.IsValid) return;
        if (player.PlayerPawn.Value.ItemServices == null) return;

        GiveNamedItem2.Invoke(player.PlayerPawn.Value.ItemServices.Handle, item, 0, 0, 0, 0, 0, 0);
    }

    private void AllocateItemsForPlayer(CCSPlayerController player, ICollection<CsItem> items, string? slotToSelect)
    {
        Log.Trace($"Allocating items: {string.Join(",", items)}; selecting slot {slotToSelect}");

        AddTimer(0.1f, () =>
        {
            if (!Helpers.PlayerIsValid(player))
            {
                Log.Trace("Player is not valid when allocating item");
                return;
            }

            foreach (var item in items)
            {
                string? itemString = EnumUtils.GetEnumMemberAttributeValue(item);
                if (string.IsNullOrWhiteSpace(itemString))
                {
                    continue;
                }
                PlayerGiveNamedItem(player, itemString);
                var slotType = WeaponHelpers.GetSlotTypeForItem(item);
                if (slotType is not null)
                {
                    SetPlayerRoundAllocation(player, slotType.Value, item);
                }
            }

            if (slotToSelect is not null)
            {
                AddTimer(0.1f, () =>
                {
                    if (Helpers.PlayerIsValid(player) && player.UserId is not null)
                    {
                        NativeAPI.IssueClientCommand((int)player.UserId, slotToSelect);
                    }
                });
            }
        });
    }

    private void GiveDefuseKit(CCSPlayerController player)
    {
        AddTimer(0.1f, () =>
        {
            if (!Helpers.PlayerIsValid(player) || !player.PlayerPawn.IsValid || player.PlayerPawn.Value is null ||
                !player.PlayerPawn.Value.IsValid || player.PlayerPawn.Value?.ItemServices?.Handle is null)
            {
                Log.Trace($"Player is not valid when giving defuse kit");
                return;
            }

            var itemServices = new CCSPlayer_ItemServices(player.PlayerPawn.Value.ItemServices.Handle);
            itemServices.HasDefuser = true;
        });
    }

    #endregion
}