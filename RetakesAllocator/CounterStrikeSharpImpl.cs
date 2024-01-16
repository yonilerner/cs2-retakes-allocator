using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;
using RetakesAllocatorCore.CounterStrikeSharpMock;

namespace RetakesAllocator;

public class NativeApiImpl : INativeAPIMock
{
    public void IssueClientCommand(int clientIndex, string command)
    {
        NativeAPI.IssueClientCommand(clientIndex, command);
    }
}

public class ServerImpl : IServerMock
{
    public void PrintToChatAll(string message)
    {
        Server.PrintToChatAll(message);
    }
}

public class PlayerWeaponImpl : IPlayerWeaponMock
{
    private readonly CHandle<CBasePlayerWeapon> _weapon;

    public PlayerWeaponImpl(CHandle<CBasePlayerWeapon> weapon)
    {
        _weapon = weapon;
    }

    public bool IsValid => _weapon.IsValid && (_weapon.Value?.IsValid ?? false);
    public string DesignerName => _weapon.Value?.DesignerName ?? "";

    public CsItem? Item =>
        _weapon.Value is not null
            ? Utils.ToEnum<CsItem>(DesignerName)
            : null;

    public nint? Handle => _weapon.Value?.Handle;

    public void Remove()
    {
        _weapon.Value?.Remove();
    }
}

public class WeaponServicesImpl : IWeaponServicesMock
{
    private readonly CPlayer_WeaponServices _weaponServices;

    public WeaponServicesImpl(CPlayer_WeaponServices weaponServices)
    {
        _weaponServices = weaponServices;
    }

    public ICollection<IPlayerWeaponMock> MyWeapons =>
        _weaponServices
            .MyWeapons
            .Select(w => new PlayerWeaponImpl(w))
            .Cast<IPlayerWeaponMock>()
            .ToList();
}

public class CCSPlayerPawnImpl : ICCSPlayerPawnMock
{
    private readonly CHandle<CCSPlayerPawn> _pawn;

    public CCSPlayerPawnImpl(CHandle<CCSPlayerPawn> pawn)
    {
        _pawn = pawn;
    }

    public IWeaponServicesMock? WeaponServices =>
        _pawn.Value?.WeaponServices is not null
            ? new WeaponServicesImpl(_pawn.Value.WeaponServices)
            : null;

    public void RemovePlayerItem(IPlayerWeaponMock weapon)
    {
        if (weapon.Handle is not null)
        {
            _pawn.Value?.RemovePlayerItem(new CBasePlayerWeapon(weapon.Handle.Value));
        }
    }
}

public class CCSPlayer_ItemServicesImpl : ICCSPlayer_ItemServicesMock
{
    private readonly CCSPlayer_ItemServices? _itemServices;

    public CCSPlayer_ItemServicesImpl(CPlayer_ItemServices? itemServices)
    {
        _itemServices = itemServices is not null
            ? new CCSPlayer_ItemServices(itemServices.Handle)
            : null;
    }

    public bool HasDefuser
    {
        get => _itemServices?.HasDefuser ?? false;
        set
        {
            if (_itemServices is not null)
            {
                _itemServices.HasDefuser = value;
            }
        }
    }

    public bool IsValid => _itemServices?.Handle is not null;
}

public class CCSPlayerControllerImpl : ICCSPlayerControllerMock
{
    private readonly CCSPlayerController? _player;

    public CCSPlayerControllerImpl(CCSPlayerController? player)
    {
        _player = player;
    }

    public int? UserId => _player?.UserId;
    public bool IsValid => _player?.IsValid ?? false;

    public ulong SteamId => _player is not null
        ? _player.AuthorizedSteamID?.SteamId64 ?? 0
        : 0;

    public CsTeam Team => _player?.Team ?? CsTeam.None;

    public ICCSPlayerPawnMock? PlayerPawn => _player is not null
        ? new CCSPlayerPawnImpl(_player.PlayerPawn)
        : null;

    public ICCSPlayer_ItemServicesMock? ItemServices =>
        new CCSPlayer_ItemServicesImpl(_player?.PlayerPawn.Value?.ItemServices);

    public void GiveNamedItem(CsItem item)
    {
        _player?.GiveNamedItem(item);
    }
}

public class UtilitiesImpl : IUtilitiesMock
{
    public List<ICCSPlayerControllerMock> GetPlayers()
    {
        return Utilities
            .GetPlayers()
            .Select(player => new CCSPlayerControllerImpl(player))
            .Cast<ICCSPlayerControllerMock>().ToList();
    }
}

public class CounterStrikeSharpImpl : ICounterStrikeSharpMock
{
    public INativeAPIMock NativeApi => new NativeApiImpl();
    public IServerMock Server => new ServerImpl();
    public IUtilitiesMock Utilities => new UtilitiesImpl();
    public string MessagePrefix => PluginInfo.MessagePrefix;

    private readonly RetakesAllocator _plugin;

    public CounterStrikeSharpImpl(RetakesAllocator plugin)
    {
        _plugin = plugin;
    }

    public void AllocateItemsForPlayer(ICCSPlayerControllerMock player, ICollection<CsItem> items, string? slotToSelect)
    {
        // Log.Write($"Allocating items: {string.Join(",", items)}");
        AddTimer(0.1f, () =>
        {
            if (player.IsValid)
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
                    if (player.IsValid && player.UserId is not null)
                    {
                        NativeApi.IssueClientCommand((int) player.UserId, slotToSelect);
                    }
                });
            }
        });
    }

    public void GiveDefuseKit(ICCSPlayerControllerMock player)
    {
        _plugin.AddTimer(0.1f, () =>
        {
            var itemServices = player.ItemServices;
            if ((itemServices?.IsValid ?? false) && player.IsValid)
            {
                itemServices.HasDefuser = true;
            }
        });
    }

    public void AddTimer(float interval, Action callback)
    {
        _plugin.AddTimer(interval, callback);
    }

    public bool PlayerIsValid(ICCSPlayerControllerMock? player)
    {
        return (player?.IsValid ?? false) && player.SteamId != 0;
    }

    public ICollection<string> CommandInfoToArgList(ICommandInfoMock commandInfo, bool includeFirst = false)
    {
        var result = new List<string>();

        for (var i = includeFirst ? 0 : 1; i < commandInfo.ArgCount; i++)
        {
            result.Add(commandInfo.GetArg(i));
        }

        return result;
    }

    public bool RemoveWeapons(ICCSPlayerControllerMock player, Func<CsItem, bool>? where = null)
    {
        if (!PlayerIsValid(player) || player.PlayerPawn?.WeaponServices is null)
        {
            return false;
        }

        var removed = false;

        foreach (var weapon in player.PlayerPawn.WeaponServices.MyWeapons)
        {
            // Log.Write($"want to remove wep {weapon.Value?.DesignerName} {weapon.IsValid}");
            if (!weapon.IsValid)
            {
                continue;
            }

            // Log.Write($"item to remove: {item}");
            var item = weapon.Item;
            if (
                where is not null &&
                (item is null || !where(item.Value))
            )
            {
                continue;
            }

            // Log.Write($"Removing weapon {weapon.Value.DesignerName} {weapon.IsValid}");

            player.PlayerPawn.RemovePlayerItem(weapon);
            weapon.Remove();

            removed = true;
        }

        return removed;
    }

    private CCSGameRules GetGameRules()
    {
        var gameRulesEntities = CounterStrikeSharp.API.Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
        var gameRules = gameRulesEntities.First().GameRules;

        if (gameRules is null)
        {
            const string message = "Game rules were null.";
            Log.Write(message);
            throw new Exception(message);
        }

        return gameRules;
    }

    public bool IsWarmup()
    {
        return GetGameRules().WarmupPeriod;
    }

    public bool IsWeaponAllocationAllowed()
    {
        return WeaponHelpers.IsWeaponAllocationAllowed(GetGameRules().FreezePeriod);
    }

    public double GetVectorDistance(IVector v1, IVector v2)
    {
        var dx = v1.X - v2.X;
        var dy = v1.Y - v2.Y;

        return Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));
    }
}
