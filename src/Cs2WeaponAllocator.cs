using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace Cs2WeaponAllocator;

[MinimumApiVersion(129)]
public class Cs2WeaponAllocator : BasePlugin
{
	public override string ModuleName => "Weapon Allocator Plugin";
	public override string ModuleVersion => "0.0.1";

	private readonly IList<CCSPlayerController> tPlayers = new List<CCSPlayerController>();
	private readonly IList<CCSPlayerController> ctPlayers = new List<CCSPlayerController>();

	public override void Load(bool hotReload)
	{
		Console.WriteLine($"Loaded {nameof(Cs2WeaponAllocator)}");
	}

	public override void Unload(bool hotReload)
	{
		Console.WriteLine($"Unloaded {nameof(Cs2WeaponAllocator)}");
	}

	[GameEventHandler]
	public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
	{
		var player = @event.Userid;
		var oldTeam = (CsTeam)@event.Oldteam;
		var playerTeam = (CsTeam)@event.Team;

		switch (oldTeam)
		{
			case CsTeam.Terrorist:
				this.tPlayers.Remove(player);
				break;
			case CsTeam.CounterTerrorist:
				this.ctPlayers.Remove(player);
				break;
		}

		switch (playerTeam)
		{
			case CsTeam.Spectator:
			case CsTeam.None:
				this.tPlayers.Remove(player);
				this.ctPlayers.Remove(player);
				break;
			case CsTeam.Terrorist:
				this.tPlayers.Add(player);
				break;
			case CsTeam.CounterTerrorist:
				this.ctPlayers.Add(player);
				break;
		}

		return HookResult.Continue;
	}

	[GameEventHandler]
	public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
	{
		this.tPlayers.Remove(@event.Userid);
		this.ctPlayers.Remove(@event.Userid);
		return HookResult.Continue;
	}

	[GameEventHandler]
	public HookResult OnRoundPostStart(EventRoundPoststart @event, GameEventInfo info)
	{
		var roundType = GetRandomRoundType();
		Log.Write($"Round type: {roundType}");

		Log.Write("Players:");
		Log.Write($"T: ${this.tPlayers.Count}");
		Log.Write($"CT: ${this.ctPlayers.Count}");

		foreach (var player in this.tPlayers)
		{
			var items = new List<CsItem>
			{
				GetArmorForRoundType(roundType),
				CsItem.Knife,
				CsItem.Bomb
			};
			items.AddRange(GetRandomUtilForRoundType(roundType, CsTeam.Terrorist)
			);
			items.AddRange(GetRandomWeaponsForRoundType(roundType, CsTeam.Terrorist)
			);

			this.AllocateItemsForPlayer(player, items);
		}

		var defusingPlayer = Utils.Choice(this.ctPlayers);
		foreach (var player in this.ctPlayers)
		{
			var items = new List<CsItem>
			{
				GetArmorForRoundType(roundType),
				CsItem.Knife
			};
			items.AddRange(GetRandomWeaponsForRoundType(roundType, CsTeam.CounterTerrorist)
			);

			// On non-pistol rounds, everyone gets defuse kit and util
			if (roundType != RoundType.Pistol)
			{
				this.GiveDefuseKit(player);
				items.AddRange(GetRandomUtilForRoundType(roundType, CsTeam.CounterTerrorist));
			}
			else
			{
				// On pistol rounds, you get util *or* a defuse kit
				if (defusingPlayer?.UserId == player.UserId)
				{
					this.GiveDefuseKit(player);
				}
				else
				{
					items.AddRange(GetRandomUtilForRoundType(roundType, CsTeam.CounterTerrorist));
				}
			}

			this.AllocateItemsForPlayer(player, items);
		}

		return HookResult.Continue;
	}

	private void AllocateItemsForPlayer(CCSPlayerController player, IList<CsItem> items)
	{
		this.AddTimer(0.1f, () =>
		{
			foreach (var item in items)
				player.GiveNamedItem(item);
		});
	}

	private void GiveDefuseKit(CCSPlayerController player)
	{
		if (player.PlayerPawn.Value?.ItemServices?.Handle == null)
		{
			return;
		}

		this.AddTimer(0.1f, () =>
		{
			var itemServices = new CCSPlayer_ItemServices(player.PlayerPawn.Value.ItemServices.Handle)
			{
				HasDefuser = true
			};
		});
	}

	private static RoundType GetRandomRoundType()
	{
		var randomValue = new Random().NextDouble();

		return randomValue switch
		{
			// 15% chance of pistol round
			< 0.15 => RoundType.Pistol,
			// 25% chance of halfbuy round
			< 0.40 => RoundType.HalfBuy,
			// 70% chance of fullbuy round
			_ => RoundType.FullBuy
		};
	}

	private static IEnumerable<CsItem> GetRandomUtilForRoundType(RoundType roundType, CsTeam team)
	{
		// Limited util on pistol rounds
		if (roundType == RoundType.Pistol)
		{
			return new List<CsItem>
			{
				Utils.Choice(new List<CsItem>
				{
					CsItem.Flashbang,
					CsItem.Smoke
				})
			};
		}

		// All util options are available on buy rounds
		var possibleItems = new List<CsItem>
		{
			CsItem.Flashbang,
			CsItem.Smoke,
			CsItem.HEGrenade,
			team == CsTeam.Terrorist ? CsItem.Molotov : CsItem.Incendiary
		};

		// Everyone gets one util
		var randomUtil = new List<CsItem>
		{
			Utils.Choice(possibleItems)
		};

		// 50% chance to get an extra util item
		// We cant give people duplicate of anything other than a flash though, so
		//  try up to 50 times to give them a duplicate flash or a non-duplicate other nade
		if (new Random().NextDouble() < .5)
		{
			var i = 0;
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

	private static CsItem GetArmorForRoundType(RoundType roundType)
		=> roundType == RoundType.Pistol ? CsItem.Kevlar : CsItem.KevlarHelmet;

	private static IEnumerable<CsItem> GetRandomWeaponsForRoundType(RoundType roundType, CsTeam team)
	{
		IList<CsItem> tPistolWeapons = new List<CsItem>
		{
			CsItem.Glock,
			CsItem.P250,
			CsItem.Tec9,
			CsItem.Deagle
		};
		IList<CsItem> ctPistolWeapons = new List<CsItem>
		{
			CsItem.USP,
			CsItem.P250,
			CsItem.FiveSeven,
			CsItem.Deagle
		};

		IList<CsItem> tHalfBuyWeapons = new List<CsItem>
		{
			CsItem.Mac10,
			CsItem.MP5,
			CsItem.UMP45,
			CsItem.P90
		};
		IList<CsItem> ctHalfBuyWeapons = new List<CsItem>
		{
			CsItem.MP9,
			CsItem.MP5,
			CsItem.UMP45,
			CsItem.P90
		};

		IList<CsItem> tFullBuyWeapons = new List<CsItem>
		{
			CsItem.AK47
		};

		IList<CsItem> ctFullBuyWeapons = new List<CsItem>
		{
			CsItem.M4A4,
			CsItem.M4A1S
		};

		var weapons = new List<CsItem>();
		if (roundType == RoundType.Pistol)
		{
			weapons.Add(Utils.Choice(team == CsTeam.Terrorist ? tPistolWeapons : ctPistolWeapons));
			return weapons;
		}

		weapons.Add(team == CsTeam.Terrorist ? CsItem.Glock : CsItem.USP);

		switch (roundType)
		{
			case RoundType.HalfBuy:
				weapons.Add(Utils.Choice(team == CsTeam.Terrorist ? tHalfBuyWeapons : ctHalfBuyWeapons));
				break;
			// 20% chance of getting an AWP on a fullbuy round
			case RoundType.FullBuy when new Random().NextDouble() < 0.2:
				weapons.Add(CsItem.AWP);
				break;
			default:
				weapons.Add(Utils.Choice(team == CsTeam.Terrorist ? tFullBuyWeapons : ctFullBuyWeapons));
				break;
		}

		return weapons;
	}
}