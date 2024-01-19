using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using RetakesAllocator.Managers;
using RetakesAllocator.Menus;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;
using SQLitePCL;
using static RetakesAllocatorCore.PluginInfo;

namespace RetakesAllocator;

[MinimumApiVersion(147)]
public class RetakesAllocator : BasePlugin
{
    public override string ModuleName => "Retakes Allocator Plugin";
    public override string ModuleVersion => PluginInfo.Version;
    public override string ModuleAuthor => "Yoni Lerner";
    public override string ModuleDescription => "https://github.com/yonilerner/cs2-retakes-allocator";
    
    private readonly MenuManager _menuManager = new();

    #region Setup

    public override void Load(bool hotReload)
    {
        Log.Write($"Loaded. Hot reload: {hotReload}");
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

    private void ResetState(bool loadConfig = true)
    {
        if (loadConfig)
        {
            Configs.Load(ModuleDirectory, true);
        }

        RoundTypeManager.GetInstance().SetNextRoundType(null);
        RoundTypeManager.GetInstance().SetCurrentRoundType(null);
    }

    private void HandleHotReload()
    {
        Server.ExecuteCommand($"map {Server.MapName}");
    }

    public override void Unload(bool hotReload)
    {
        Log.Write("Unloaded");
        ResetState(loadConfig: false);
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
            RoundTypeManager.GetInstance().GetCurrentRoundType(),
            currentTeam,
            false,
            out var selectedWeapon
        );
        Helpers.WriteNewlineDelimited(result, l => commandInfo.ReplyToCommand(l));

        if (Helpers.IsWeaponAllocationAllowed() && selectedWeapon is not null)
        {
            var selectedWeaponAllocationType =
                WeaponHelpers.GetWeaponAllocationTypeForWeaponAndRound(RoundTypeManager.GetInstance().GetCurrentRoundType(), currentTeam,
                    selectedWeapon.Value);
            if (selectedWeaponAllocationType is not null)
            {
                Helpers.RemoveWeapons(
                    player,
                    item =>
                        WeaponHelpers.GetWeaponAllocationTypeForWeaponAndRound(RoundTypeManager.GetInstance().GetCurrentRoundType(), currentTeam, item) ==
                        selectedWeaponAllocationType
                );
                var slot = selectedWeaponAllocationType.Value switch
                {
                    WeaponAllocationType.FullBuyPrimary => "slot1",
                    WeaponAllocationType.HalfBuyPrimary => "slot1",
                    WeaponAllocationType.Secondary => "slot2",
                    WeaponAllocationType.PistolRound => "slot2",
                    WeaponAllocationType.Preferred => "slot1",
                    _ => throw new ArgumentOutOfRangeException()
                };
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
        var currentTeam = player!.Team;

        var currentPreferredSetting = Queries.GetUserSettings(playerId)
            ?.GetWeaponPreference(currentTeam, WeaponAllocationType.Preferred);

        var result = OnWeaponCommandHelper.Handle(
            new List<string> {CsItem.AWP.ToString()},
            playerId,
            RoundTypeManager.GetInstance().GetCurrentRoundType(),
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
            RoundTypeManager.GetInstance().GetCurrentRoundType(),
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
            commandInfo.ReplyToCommand($"{MessagePrefix}Invalid round type: {roundTypeInput}.");
        }
        else
        {
            RoundTypeManager.GetInstance().SetNextRoundType(roundType);
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
        var team = player.Team;
        var playerId = Helpers.GetSteamId(player);
        var isPreferred = WeaponHelpers.IsPreferred(team, item);

        var purchasedAllocationType = RoundTypeManager.GetInstance().GetCurrentRoundType() is not null
            ? WeaponHelpers.GetWeaponAllocationTypeForWeaponAndRound(
                RoundTypeManager.GetInstance().GetCurrentRoundType(), team, item
            )
            : null;

        var isValidAllocation = WeaponHelpers.IsAllocationTypeValidForRound(purchasedAllocationType, RoundTypeManager.GetInstance().GetCurrentRoundType());

        // Log.Write($"item {item} team {team} player {playerId}");
        // Log.Write($"curRound {_currentRoundType} weapon alloc {purchasedAllocationType} valid? {isValidAllocation}");
        // Log.Write($"Preferred? {isPreferred}");

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

                    if (RoundTypeManager.GetInstance().GetCurrentRoundType() is null)
                    {
                        return true;
                    }

                    var at = WeaponHelpers.GetWeaponAllocationTypeForWeaponAndRound(RoundTypeManager.GetInstance().GetCurrentRoundType(), team, i);
                    // Log.Write($"at: {at}");
                    return at is null || at == purchasedAllocationType;
                });
            Log.Write($"Removed {item}? {removedAnyWeapons}");

            var replacedWeapon = false;
            var slotToSelect = RoundTypeManager.GetInstance().GetCurrentRoundType() == RoundType.Pistol ? "slot2" : "slot1";
            if (removedAnyWeapons && RoundTypeManager.GetInstance().GetCurrentRoundType() is not null && WeaponHelpers.IsWeapon(item))
            {
                var replacementAllocationType =
                    WeaponHelpers.GetReplacementWeaponAllocationTypeForWeapon(RoundTypeManager.GetInstance().GetCurrentRoundType());
                // Log.Write($"Replacement allocation type {replacementAllocationType}");
                if (replacementAllocationType is not null)
                {
                    var replacementItem = WeaponHelpers.GetWeaponForAllocationType(replacementAllocationType.Value,
                        team,
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
                        // Log.Write($"Removing {p.DesignerName}");
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
                    RoundTypeManager.GetInstance().GetCurrentRoundType(),
                    team,
                    false,
                    out _
                );
                Helpers.WriteNewlineDelimited(message, player.PrintToChat);
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
        
        var menu = _menuManager.GetMenu<VoteMenu>(MenuType.NextRoundVote);
        menu.GatherAndHandleVotes();

        var allPlayers = Utilities.GetPlayers()
            .Where(Helpers.PlayerIsValid)
            .ToList();

        OnRoundPostStartHelper.Handle(
            RoundTypeManager.GetInstance().GetNextRoundType(),
            allPlayers,
            Helpers.GetSteamId,
            Helpers.GetTeam,
            GiveDefuseKit,
            AllocateItemsForPlayer,
            Helpers.IsVip,
            out var currentRoundType
        );
        RoundTypeManager.GetInstance().SetCurrentRoundType(currentRoundType);
        RoundTypeManager.GetInstance().SetNextRoundType(null);

        if (Configs.GetConfigData().EnableRoundTypeAnnouncement)
        {
            Server.PrintToChatAll(
                $"{MessagePrefix}{Enum.GetName(RoundTypeManager.GetInstance().GetCurrentRoundType()!.Value)} Round"
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
