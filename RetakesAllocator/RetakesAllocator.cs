using System.Text;
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
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore.Managers;
using RetakesAllocator.Menus;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;
using SQLitePCL;
using RetakesAllocator.AdvancedMenus;
using static RetakesAllocatorCore.PluginInfo;
using RetakesPluginShared;
using RetakesPluginShared.Events;
using CounterStrikeSharp.API.Modules.Events;

namespace RetakesAllocator;

[MinimumApiVersion(201)]
public class RetakesAllocator : BasePlugin
{
    public override string ModuleName => "Retakes Allocator Plugin";
    public override string ModuleVersion => PluginInfo.Version;
    public override string ModuleAuthor => "Yoni Lerner, B3none, Gold KingZ";
    public override string ModuleDescription => "https://github.com/yonilerner/cs2-retakes-allocator";

    private readonly AllocatorMenuManager _allocatorMenuManager = new();
    private readonly AdvancedGunMenu _advancedGunMenu = new();
    private readonly Dictionary<CCSPlayerController, Dictionary<ItemSlotType, CsItem>> _allocatedPlayerItems = new();
    private IRetakesPluginEventSender? RetakesPluginEventSender { get; set; }

    private CustomGameData? CustomFunctions { get; set; }

    private bool IsAllocatingForRound { get; set; }
    private string _bombsite = "";
    private bool _announceBombsite;
    private bool _bombsiteAnnounceOneTime;

    #region Setup

    public override void Load(bool hotReload)
    {
        Configs.Shared.Module = ModuleDirectory;
        
        Log.Debug($"Loaded. Hot reload: {hotReload}");
        ResetState();
        Batteries.Init();

        RegisterListener<Listeners.OnMapStart>(mapName =>
        {
            ResetState();
            Log.Debug($"Setting map name {mapName}");
            RoundTypeManager.Instance.SetMap(mapName);
        });

        _ = Task.Run(async () =>
        {
            var downloadedNewGameData = await Helpers.DownloadMissingFiles();
            if (!downloadedNewGameData)
            {
                return;
            }

            Server.NextFrame(() =>
            {
                CustomFunctions ??= new();
                // Must unhook the old functions before reloading and rehooking
                CustomFunctions.CCSPlayer_ItemServices_CanAcquireFunc?.Unhook(OnWeaponCanAcquire, HookMode.Pre);
                CustomFunctions.LoadCustomGameData();
                if (Configs.GetConfigData().EnableCanAcquireHook)
                {
                    CustomFunctions.CCSPlayer_ItemServices_CanAcquireFunc?.Hook(OnWeaponCanAcquire, HookMode.Pre);
                }
            });

        });

        if (Configs.GetConfigData().UseOnTickFeatures)
        {
            RegisterListener<Listeners.OnTick>(OnTick);
        }

        AddTimer(0.1f, () => { GetRetakesPluginEventSender().RetakesPluginEventHandlers += RetakesEventHandler; });

        if (Configs.GetConfigData().MigrateOnStartup)
        {
            Queries.Migrate();
        }

        CustomFunctions = new();

        if (Configs.GetConfigData().EnableCanAcquireHook)
        {
            CustomFunctions.CCSPlayer_ItemServices_CanAcquireFunc?.Hook(OnWeaponCanAcquire, HookMode.Pre);
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
        _bombsite = "";
        _announceBombsite = false;
        _bombsiteAnnounceOneTime = false;
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

        if (Configs.GetConfigData().EnableCanAcquireHook && CustomFunctions != null)
        {
            CustomFunctions.CCSPlayer_ItemServices_CanAcquireFunc?.Unhook(OnWeaponCanAcquire, HookMode.Pre);
        }
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

        _allocatorMenuManager.OpenMenuForPlayer(player!, MenuType.NextRoundVote);
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
        Helpers.WriteNewlineDelimited(result, commandInfo.ReplyToCommand);

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

        if (Configs.GetConfigData().NumberOfExtraVipChancesForPreferredWeapon == -1 && !Helpers.IsVip(player))
        {
            var message = Translator.Instance["weapon_preference.only_vip_can_use"];
            commandInfo.ReplyToCommand($"{MessagePrefix}{message}");
            return;
        }

        var result = Task.Run(async () =>
        {
            var currentPreferredSetting = (await Queries.GetUserSettings(playerId))
                ?.GetWeaponPreference(currentTeam, WeaponAllocationType.Preferred);

            return await OnWeaponCommandHelper.HandleAsync(
                new List<string> {CsItem.AWP.ToString()},
                playerId,
                RoundTypeManager.Instance.GetCurrentRoundType(),
                currentTeam,
                currentPreferredSetting is not null
            );
        }).Result;
        Helpers.WriteNewlineDelimited(result.Item1, commandInfo.ReplyToCommand);
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

    [ConsoleCommand("css_print_config", "Print the entire config or a specific config.")]
    [CommandHelper(usage: "<config>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnPrintConfigCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var configName = commandInfo.ArgCount > 1 ? commandInfo.GetArg(1) : null;
        var response = Configs.StringifyConfig(configName);
        if (response is null)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}Invalid config name.");
            return;
        }

        commandInfo.ReplyToCommand($"{MessagePrefix}{response}");
        Log.Info(response);
    }

    #endregion

    #region Events

    public HookResult OnWeaponCanAcquire(DynamicHook hook)
    {
        Log.Debug("OnWeaponCanAcquire");
        
        var acquireMethod = hook.GetParam<AcquireMethod>(2);
        if (acquireMethod == AcquireMethod.PickUp)
        {
            return HookResult.Continue;
        }

        if (Helpers.IsWarmup())
        {
            return HookResult.Continue;
        }

        // Log.Trace($"OnWeaponCanAcquire enter {IsAllocatingForRound}");
        if (IsAllocatingForRound)
        {
            Log.Debug("Skipping OnWeaponCanAcquire because we're allocating for round");
            return HookResult.Continue;
        }

        HookResult RetStop()
        {
            // Log.Debug($"Exiting OnWeaponCanAcquire {acquireMethod}");
            hook.SetReturn(
                acquireMethod != AcquireMethod.PickUp
                    ? AcquireResult.AlreadyOwned
                    : AcquireResult.InvalidItem
            );

            return HookResult.Stop;
        }

        if (CustomFunctions is null)
        {
            return RetStop();
        }

        var weaponData = CustomFunctions.GetCSWeaponDataFromKeyFunc?.Invoke(-1,
            hook.GetParam<CEconItemView>(1).ItemDefinitionIndex.ToString());

        var player = hook.GetParam<CCSPlayer_ItemServices>(0).Pawn.Value.Controller.Value?.As<CCSPlayerController>();
        if (player is null || !player.IsValid || !player.PawnIsAlive)
        {
            Log.Debug($"Invalid player controller {player} {player?.IsValid} {player?.PawnIsAlive}");
            return HookResult.Continue;
        }

        if (weaponData == null)
        {
            Log.Warn($"Invalid weapon data {hook.GetParam<CEconItemView>(1).ItemDefinitionIndex}");
            return HookResult.Continue;
        }

        var team = player.Team;
        var item = Utils.ToEnum<CsItem>(weaponData.Name);

        if (item is CsItem.KnifeT or CsItem.KnifeCT)
        {
            return HookResult.Continue;
        }

        if (item is CsItem.Taser)
        {
            return Configs.GetConfigData().ZeusPreference == ZeusPreference.Always ? HookResult.Continue : RetStop();
        }

        if (!WeaponHelpers.IsUsableWeapon(item))
        {
            return RetStop();
        }

        var isPreferred = WeaponHelpers.IsPreferred(team, item);
        var purchasedAllocationType = RoundTypeManager.Instance.GetCurrentRoundType() is not null
            ? WeaponHelpers.GetWeaponAllocationTypeForWeaponAndRound(
                RoundTypeManager.Instance.GetCurrentRoundType(), team, item
            )
            : null;
        var isValidAllocation = WeaponHelpers.IsAllocationTypeValidForRound(purchasedAllocationType,
            RoundTypeManager.Instance.GetCurrentRoundType());

        // Log.Debug($"item {item} team {team} player {playerId}");
        // Log.Debug($"weapon alloc {purchasedAllocationType} valid? {isValidAllocation}");
        // Log.Debug($"Preferred? {isPreferred}");

        if (
            Helpers.IsWeaponAllocationAllowed() &&
            !isPreferred &&
            isValidAllocation &&
            purchasedAllocationType is not null
        )
        {
            return HookResult.Continue;
        }

        return RetStop();
    }

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
            RoundTypeManager.Instance.GetCurrentRoundType()) && WeaponHelpers.IsUsableWeapon(item);

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

    private void HandleAllocateEvent()
    {
        IsAllocatingForRound = true;
        Log.Debug($"Handling allocate event");
        Server.ExecuteCommand("mp_max_armor 0");

        var menu = _allocatorMenuManager.GetMenu<VoteMenu>(MenuType.NextRoundVote);
        menu.GatherAndHandleVotes();

        var allPlayers = Utilities.GetPlayers()
            .Where(player => Helpers.PlayerIsValid(player) && player.Connected == PlayerConnectedState.PlayerConnected)
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

        switch(currentRoundType)
        {
            case RoundType.Pistol:
            {
                Server.ExecuteCommand("execifexists cs2-retakes/Pistol.cfg");
                break;
            }
            case RoundType.HalfBuy:
            {
                Server.ExecuteCommand("execifexists cs2-retakes/SmallBuy.cfg");
                break;
            }
            case RoundType.FullBuy:
            {
                Server.ExecuteCommand("execifexists cs2-retakes/FullBuy.cfg");
                break;
            }
        }

        if (Configs.GetConfigData().EnableRoundTypeAnnouncement)
        {
            var roundType = RoundTypeManager.Instance.GetCurrentRoundType()!.Value;
            var roundTypeName = RoundTypeHelpers.TranslateRoundTypeName(roundType);
            var message = Translator.Instance["announcement.roundtype", roundTypeName];
            Server.PrintToChatAll($"{MessagePrefix}{message}");
            if (Configs.GetConfigData().EnableRoundTypeAnnouncementCenter)
            {
                foreach (var player in allPlayers)
                {
                    player.PrintToCenter(
                        $"{MessagePrefix}{Translator.Instance["center.announcement.roundtype", roundTypeName]}");
                }
            }
        }

        AddTimer(.5f, () =>
        {
            Log.Debug("Turning off round allocation");
            IsAllocatingForRound = false;
        });
    }

    public void OnTick()
    {
        if (!string.IsNullOrEmpty(Configs.GetConfigData().InGameGunMenuCenterCommands))
        {
            _advancedGunMenu.OnTick();
        }

        if (_announceBombsite)
        {
            var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
            var countct = Utilities.GetPlayers()
                .Count(p => p.TeamNum == (int) CsTeam.CounterTerrorist && p.PawnIsAlive && !p.IsHLTV);
            var countt = Utilities.GetPlayers()
                .Count(p => p.TeamNum == (int) CsTeam.Terrorist && p.PawnIsAlive && !p.IsHLTV);
            string image = _bombsite == "A" ? Translator.Instance["BombSite.A"] :
                _bombsite == "B" ? Translator.Instance["BombSite.B"] : "";
            foreach (var player in playerEntities)
            {
                if (!player.IsValid || !player.PawnIsAlive || player.IsBot || player.IsHLTV) continue;

                if (player.TeamNum == (byte) CsTeam.Terrorist &&
                    !Configs.GetConfigData().BombSiteAnnouncementCenterToCTOnly)
                {
                    StringBuilder builder = new StringBuilder();
                    builder.AppendFormat(Localizer["T.Message"], _bombsite, image, countt, countct);
                    var centerhtml = builder.ToString();
                    player.PrintToCenterHtml(centerhtml);
                }
                else if (player.TeamNum == (byte) CsTeam.CounterTerrorist)
                {
                    StringBuilder builder = new StringBuilder();
                    builder.AppendFormat(Localizer["CT.Message"], _bombsite, image, countt, countct);
                    var centerhtml = builder.ToString();
                    player.PrintToCenterHtml(centerhtml);
                }
            }
        }
    }

    [GameEventHandler(HookMode.Pre)]
    public HookResult OnEventBombPlanted(EventBombPlanted @event, GameEventInfo info)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (@event == null) return HookResult.Continue;

        if (Configs.GetConfigData().DisableDefaultBombPlantedCenterMessage)
        {
            info.DontBroadcast = true;
        }

        if (Configs.GetConfigData().ForceCloseBombSiteAnnouncementCenterOnPlant)
        {
            _bombsite = "";
            _announceBombsite = false;
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnEventRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (@event == null) return HookResult.Continue;
        _bombsiteAnnounceOneTime = false;
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnEventRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (@event == null) return HookResult.Continue;
        _bombsite = "";
        _announceBombsite = false;
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnEventEnterBombzone(EventEnterBombzone @event, GameEventInfo info)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (@event == null || Helpers.IsWarmup() || _bombsiteAnnounceOneTime) return HookResult.Continue;

        var player = @event.Userid;
        if (player == null || !player.IsValid || player.TeamNum != (byte) CsTeam.Terrorist) return HookResult.Continue;

        var playerPawn = player.PlayerPawn;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (playerPawn == null || !playerPawn.IsValid) return HookResult.Continue;

        var playerPosition = playerPawn.Value!.AbsOrigin;

        foreach (var entity in Utilities.FindAllEntitiesByDesignerName<CBombTarget>("info_bomb_target"))
        {
            var entityPosition = entity.AbsOrigin;
            if (entityPosition != null)
            {
                var distanceVector = playerPosition! - entityPosition;
                var distance = distanceVector.Length();
                float thresholdDistance = 400.0f;

                if (distance <= thresholdDistance)
                {
                    if (entity.DesignerName == "info_bomb_target_hint_A")
                    {
                        _bombsite = "A";
                        if (Configs.GetConfigData().EnableBombSiteAnnouncementCenter)
                        {
                            Server.NextFrame(() =>
                            {
                                AddTimer(Configs.GetConfigData().BombSiteAnnouncementCenterDelay, () =>
                                {
                                    _bombsiteAnnounceOneTime = true;
                                    _announceBombsite = true;
                                    AddTimer(Configs.GetConfigData().BombSiteAnnouncementCenterShowTimer, () =>
                                    {
                                        _bombsite = "";
                                        _announceBombsite = false;
                                    }, TimerFlags.STOP_ON_MAPCHANGE);
                                }, TimerFlags.STOP_ON_MAPCHANGE);
                            });
                        }

                        if (Configs.GetConfigData().EnableBombSiteAnnouncementChat)
                        {
                            Server.PrintToChatAll(Localizer["chatAsite.line1"]);
                            Server.PrintToChatAll(Localizer["chatAsite.line2"]);
                            Server.PrintToChatAll(Localizer["chatAsite.line3"]);
                            Server.PrintToChatAll(Localizer["chatAsite.line4"]);
                            Server.PrintToChatAll(Localizer["chatAsite.line5"]);
                            Server.PrintToChatAll(Localizer["chatAsite.line6"]);
                        }

                        break;
                    }
                    else if (entity.DesignerName == "info_bomb_target_hint_B")
                    {
                        _bombsite = "B";
                        if (Configs.GetConfigData().EnableBombSiteAnnouncementCenter)
                        {
                            Server.NextFrame(() =>
                            {
                                AddTimer(Configs.GetConfigData().BombSiteAnnouncementCenterDelay, () =>
                                {
                                    _bombsiteAnnounceOneTime = true;
                                    _announceBombsite = true;
                                    AddTimer(Configs.GetConfigData().BombSiteAnnouncementCenterShowTimer, () =>
                                    {
                                        _bombsite = "";
                                        _announceBombsite = false;
                                    }, TimerFlags.STOP_ON_MAPCHANGE);
                                }, TimerFlags.STOP_ON_MAPCHANGE);
                            });
                        }

                        if (Configs.GetConfigData().EnableBombSiteAnnouncementChat)
                        {
                            Server.PrintToChatAll(Localizer["chatBsite.line1"]);
                            Server.PrintToChatAll(Localizer["chatBsite.line2"]);
                            Server.PrintToChatAll(Localizer["chatBsite.line3"]);
                            Server.PrintToChatAll(Localizer["chatBsite.line4"]);
                            Server.PrintToChatAll(Localizer["chatBsite.line5"]);
                            Server.PrintToChatAll(Localizer["chatBsite.line6"]);
                        }

                        break;
                    }
                }
            }
        }

        return HookResult.Continue;
    }


    [GameEventHandler]
    public HookResult OnEventPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        if (!string.IsNullOrEmpty(Configs.GetConfigData().InGameGunMenuCenterCommands))
        {
            _advancedGunMenu.OnEventPlayerDisconnect(@event, info);
        }

        return HookResult.Continue;
    }

    // ReSharper disable once RedundantArgumentDefaultValue
    [GameEventHandler(HookMode.Post)]
    public HookResult OnEventPlayerChat(EventPlayerChat @event, GameEventInfo info)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (@event == null) return HookResult.Continue;

        if (!string.IsNullOrEmpty(Configs.GetConfigData().InGameGunMenuCenterCommands))
        {
            _advancedGunMenu.OnEventPlayerChat(@event, info);
        }

        var eventplayer = @event.Userid;
        var eventmessage = @event.Text;
        var player = Utilities.GetPlayerFromUserid(eventplayer);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (player == null || !player.IsValid) return HookResult.Continue;

        if (string.IsNullOrWhiteSpace(eventmessage)) return HookResult.Continue;
        string trimmedMessageStart = eventmessage.TrimStart();
        string message = trimmedMessageStart.TrimEnd();

        if (!string.IsNullOrEmpty(Configs.GetConfigData().InGameGunMenuChatCommands))
        {
            string[] chatMenuCommands = Configs.GetConfigData().InGameGunMenuChatCommands.Split(',');

            if (chatMenuCommands.Any(cmd => cmd.Equals(message, StringComparison.OrdinalIgnoreCase)))
            {
                _allocatorMenuManager.OpenMenuForPlayer(player, MenuType.Guns);
            }
        }


        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnEventRoundAnnounceWarmup(EventRoundAnnounceWarmup @event, GameEventInfo info)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (!Configs.GetConfigData().ResetStateOnGameRestart || @event == null) return HookResult.Continue;
        ResetState();
        return HookResult.Continue;
    }

    #endregion

    #region Helpers

    private void SetPlayerRoundAllocation(CCSPlayerController player, ItemSlotType slotType, CsItem item)
    {
        if (!_allocatedPlayerItems.TryGetValue(player, out _))
        {
            _allocatedPlayerItems[player] = new();
        }

        _allocatedPlayerItems[player][slotType] = item;
        Log.Trace($"Round allocation for player {player.Slot} {slotType} {item}");
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

                if (Configs.GetConfigData().CapabilityWeaponPaints && CustomFunctions != null && CustomFunctions.PlayerGiveNamedItemEnabled())
                {
                    CustomFunctions?.PlayerGiveNamedItem(player, itemString);
                }
                else
                {
                    player.GiveNamedItem(itemString);
                }
                
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
