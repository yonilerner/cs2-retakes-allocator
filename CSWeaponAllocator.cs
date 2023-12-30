using System.Collections;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace cs_weapon_allocator;

[MinimumApiVersion(129)]
public class CSWeaponAllocator : BasePlugin
{
    public override string ModuleName => "Weapon Allocator Plugin";
    public override string ModuleVersion => "0.0.1";

    private IList<CCSPlayerController> tPlayers = new List<CCSPlayerController>();
    private IList<CCSPlayerController> ctPlayers = new List<CCSPlayerController>();

    public override void Load(bool hotReload)
    {
        Console.WriteLine("Loaded CSWeaponAllocator");
    }

    public override void Unload(bool hotReload)
    {
        Console.WriteLine("Unloaded CSWeaponAllocator");
    }

    [GameEventHandler]
    public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid;
        var oldteam = (CsTeam)@event.Oldteam;
        var playerTeam = (CsTeam)@event.Team;

        switch (oldteam)
        {
            case CsTeam.Terrorist:
                tPlayers.Remove(player);
                break;
            case CsTeam.CounterTerrorist:
                ctPlayers.Remove(player);
                break;
        }

        switch (playerTeam)
        {
            case CsTeam.Spectator:
            case CsTeam.None:
                tPlayers.Remove(player);
                ctPlayers.Remove(player);
                return HookResult.Continue;
            case CsTeam.Terrorist:
                tPlayers.Add(player);
                break;
            case CsTeam.CounterTerrorist:
                ctPlayers.Add(player);
                break;
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        tPlayers.Remove(@event.Userid);
        ctPlayers.Remove(@event.Userid);
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundPostStart(EventRoundPoststart @event, GameEventInfo info)
    {
        var roundType = GetRandomRoundType();
        Log.Write($"Round type: {roundType}");

        Log.Write("Players:");
        Log.Write($"T: ${tPlayers.Count}");
        Log.Write($"CT: ${ctPlayers.Count}");

        foreach (var player in tPlayers)
        {
            var items = new List<CsItem>
            {
                GetArmorForRoundType(roundType),
                CsItem.Knife,
                CsItem.Bomb,
            };
            items.AddRange(
                GetRandomUtilForRoundType(roundType, CsTeam.Terrorist)
            );
            items.AddRange(
                GetRandomWeaponsForRoundType(roundType, CsTeam.Terrorist)
            );

            AllocateItemsForPlayer(player, items);
        }

        var defuser = Utils.Choice(ctPlayers);
        foreach (var player in ctPlayers)
        {
            var items = new List<CsItem>
            {
                GetArmorForRoundType(roundType),
                CsItem.Knife,
            };
            items.AddRange(
                GetRandomWeaponsForRoundType(roundType, CsTeam.CounterTerrorist)
            );

            // On non-pistol rounds, everyone gets defuse kit and util
            if (roundType != RoundType.Pistol)
            {
                GiveDefuseKit(player);
                items.AddRange(GetRandomUtilForRoundType(roundType, CsTeam.CounterTerrorist));
            }
            else
            {
                // On pistol rounds, you get util *or* a defuse kit
                if (defuser?.UserId == player.UserId)
                {
                    GiveDefuseKit(player);
                }
                else
                {
                    items.AddRange(GetRandomUtilForRoundType(roundType, CsTeam.CounterTerrorist));
                }
            }

            AllocateItemsForPlayer(player, items);
        }

        return HookResult.Continue;
    }

    private void AllocateItemsForPlayer(CCSPlayerController player, IList<CsItem> items)
    {
        AddTimer(0.1f, () =>
        {
            foreach (var item in items)
            {
                player.GiveNamedItem(item);
            }
        });
    }

    private void GiveDefuseKit(CCSPlayerController player)
    {
        if (player.PlayerPawn.Value?.ItemServices?.Handle == null)
        {
            return;
        }
        AddTimer(0.1f, () =>
        {
            var itemServices = new CCSPlayer_ItemServices(player.PlayerPawn.Value.ItemServices.Handle);
            itemServices.HasDefuser = true;
        });
    }

    private RoundType GetRandomRoundType()
    {
        var randomValue = new Random().NextDouble();

        if (randomValue < 0.2)
        {
            // 20% chance of pistol round
            return RoundType.Pistol;
        }
        else if (randomValue < 0.5)
        {
            // 30% chance of half-buy round
            return RoundType.HalfBuy;
        }
        else
        {
            // 50% chance of rifle round
            return RoundType.FullBuy;
        }
    }

    private IList<CsItem> GetRandomUtilForRoundType(RoundType roundType, CsTeam team)
    {
        var pistolItems = new List<CsItem>
        {
            CsItem.Flashbang,
            CsItem.Smoke,
        };
        if (roundType == RoundType.Pistol)
        {
            return new List<CsItem> { Utils.Choice(pistolItems) };
        }

        var possibleItems = new List<CsItem>
        {
            CsItem.Flashbang,
            CsItem.Smoke,
            CsItem.HEGrenade,
            team == CsTeam.Terrorist ? CsItem.Molotov : CsItem.Incendiary,
        };

        var randomUtil = new List<CsItem>
        {
            Utils.Choice(possibleItems)
        };

        var i = 0;
        if (new Random().NextDouble() < .5)
        {
            while (i < 50)
            {
                var extraItem = Utils.Choice(possibleItems);
                if (extraItem == CsItem.Flashbang || !randomUtil.Contains(extraItem))
                {
                    randomUtil.Add(extraItem);
                    break;
                }

                i++;
            }
        }

        return randomUtil;
    }

    private CsItem GetArmorForRoundType(RoundType roundType)
    {
        if (roundType == RoundType.Pistol)
        {
            return CsItem.Kevlar;
        }

        return CsItem.KevlarHelmet;
    }

    private List<CsItem> GetRandomWeaponsForRoundType(RoundType roundType, CsTeam team)
    {
        IList<CsItem> tPistolWeapons = new List<CsItem>
        {
            CsItem.Glock,
            CsItem.P250,
            CsItem.Tec9,
            CsItem.Deagle,
        };
        IList<CsItem> ctPistolWeapons = new List<CsItem>
        {
            CsItem.USP,
            CsItem.P250,
            CsItem.FiveSeven,
            CsItem.Deagle,
        };

        IList<CsItem> tHalfBuyWeapons = new List<CsItem>
        {
            CsItem.Mac10,
            CsItem.MP5,
            CsItem.UMP45,
            CsItem.P90,
        };
        IList<CsItem> ctHalfBuyWeapons = new List<CsItem>
        {
            CsItem.MP9,
            CsItem.MP5,
            CsItem.UMP45,
            CsItem.P90,
        };

        IList<CsItem> tFullBuyWeapons = new List<CsItem>
        {
            CsItem.AK47,
        };

        IList<CsItem> ctFullBuyWeapons = new List<CsItem>
        {
            CsItem.M4A4,
            CsItem.M4A1S,
        };

        var weapons = new List<CsItem>();
        if (roundType == RoundType.Pistol)
        {
            weapons.Add(Utils.Choice(team == CsTeam.Terrorist ? tPistolWeapons : ctPistolWeapons));
            return weapons;
        }

        weapons.Add(team == CsTeam.Terrorist ? CsItem.Glock : CsItem.USP);

        if (roundType == RoundType.HalfBuy)
        {
            weapons.Add(Utils.Choice(team == CsTeam.Terrorist ? tHalfBuyWeapons : ctHalfBuyWeapons));
        }
        else
        {
            var randomlyGetAwp = new Random().NextDouble() < 0.2;
            if (randomlyGetAwp && roundType == RoundType.FullBuy)
            {
                weapons.Add(CsItem.AWP);
            }
            else
            {
                weapons.Add(Utils.Choice(team == CsTeam.Terrorist ? tFullBuyWeapons : ctFullBuyWeapons));
            }
        }

        return weapons;
    }
}