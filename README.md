# CS2 Retakes Allocator

[![Build RetakesAllocator.zip](https://github.com/yonilerner/cs2-retakes-allocator/actions/workflows/build.yml/badge.svg)](https://github.com/yonilerner/cs2-retakes-allocator/actions/workflows/build.yml)

## Retakes

This plugin is made to run alongside B3none's retakes implementation: https://github.com/b3none/cs2-retakes

## Installation

- Ensure you have https://github.com/b3none/cs2-retakes installed already
- Update the `RetakesPlugin` config to have `EnableFallbackAllocation` disabled
- Download a release from https://github.com/yonilerner/cs2-retakes-allocator/releases
- Extract the zip archive and upload the `RetakesAllocator` plugin to your CounterStrikeSharp plugins folder on your
  server
    - Each build comes with two necessary runtimes for sqlite3, one for linux64 and one for win64. If you need a
      different runtime, please submit an issue and I can provide more runtimes
    - If you're wondering why so many DLLs in the build: They are necessary for the Entity Framework that enables modern
      interfaces for databases
- [Buy Menu - Optional] If you want buy menu weapon selection to work, ensure the following convars are set at the
  bottom of `game/csgo/cfg/cs2-retakes/retakes.cfg`:
    - `mp_buy_anywhere 1`
    - `mp_buytime 60000`
    - `mp_maxmoney 65535`
    - `mp_startmoney 65535`
    - `mp_afterroundmoney 65535`
    - More info about this in the "Buy Menu" section below

## Usage

### Round Types

This plugin implements 3 different round types:

- Pistol
    - Weapons: Only pistols
    - Armor: Kevlar and no helmet
    - Util: Flash or smoke, except one CT that gets a defuse kit
- HalfBuy (shotguns and SMGs)
    - Weapons: Shotguns and SMGs
    - Armor: Kevlar and helmet
    - Util: One nade + 50% chance of a 2nd nade. Every CT has a defuse kit
- FullBuy (Rifles, snipers, machine guns)
    - Weapons: Rifles, snipers, machine guns
    - Armor: Kevlar and helmet
    - Util: One nade + 50% chance of a 2nd nade. Every CT has a defuse kit

How these round types are chosen can be configured. See more in the "Configuration" section below.

### Weapon Preferences

There are a few different ways to set weapon preferences:

- Built-in buy menu (See "Buy Menu" section for more info on how to set that up)
- `!gun <gun>` - Set a preference for a particular gun (will automatically figure out the round type)
- `!awp` - Toggles if you want an AWP or not.
- `!guns` - Opens a chat-based menu for weapon preferences

See more info below about the commands in the "Commands" section.

#### AWP Queue

Currently one AWPer will be selected per team as long as at least one person on the team has chosen to get an AWP. AWP
queue features will be expanded over time, you can take a look at existing Github Issues to see what has been proposed
so far.

### Buy Menu

If the convars are set to give players money and let them buy, player weapon choices can be selected via the buy menu.
The buy menu will look like it allows you to buy any weapon, but it will only let you have weapons that are appropriate
for the current round type.

The convars can be tweaked to customize the experience. For example, if you dont want to allow people to use the buy
menu the entire round, you can tweak the `mp_buytime` variable as you see fit.

### Configuration

The config file is located in the plugin folder under `config/config.json`.

#### Round Type Configuration

- `RoundTypeSelection`: Which round type selection system to use. The options are:
    - `Random`: Randomly select a round based on the percentages set in `RoundTypePercentages`
    - `RandomFixedCounts`: Every round will be a random selection based on a fixed set of rounds per type, configured
      by `RoundTypeRandomFixedCounts`.
    - `ManualOrdering`: The round will follow the exact order you specify.
- `RoundTypePercentages`: The frequency of each type of round. The values must add up to `100`.
    - Only used when `RoundTypeSelection` is `Random`
- `RoundTypeRandomFixedCounts`: The fixed counts for each type of round. For example, if your config
  is `{"Pistol": 5, "HalfBuy": 10, "FullBuy": 15}`, then over the next 30 rounds, exactly 5 of them will be pistols, 10
  will be half buys, and 15 will be full buys, but the exact ordering of the rounds will be random.
    - Only used when `RoundTypeSelection` is `RandomFixedCounts`
    - The random ordering will restart back at the beginning if the map is not over.
    - A new random ordering will be selected at the start of each map.
- `RoundTypeManualOrdering`: The exact order of rounds and how many of each round in that order. For example, if your
  config
  is `[{"Type": "Pistol", "Count": 5}, {"Type": "FullBuy", "Count": 25}, {"Type": "Pistol", "Count": 1}]`, then you will
  get 5 pistol rounds, 25 full buy rounds, a single pistol round, and then it will start from the beginning again. A new
  map always starts from the beginning.

#### Weapon Configuration

For any of the weapon configs, the valid weapon names come
from [here](https://github.com/roflmuffin/CounterStrikeSharp/blob/main/managed/CounterStrikeSharp.API/Modules/Entities/Constants/CsItem.cs).
For example in
```cs
        [EnumMember(Value = "item_kevlar")]
        Kevlar = 000,
```
`Kevlar` is the name of the weapon, not `item_kevlar`.
In
```cs
[EnumMember(Value = "weapon_m4a1_silencer")]
M4A1S = 401,
SilencedM4 = M4A1S,
```
both `M4A1S` and `SilencedM4` are valid weapon names, but `weapon_m4a1_silencer` is not.

Here are the weapon configs:
- `UsableWeapons`: The weapons that can be allocated. Any weapon removed from this list cannot be used.
- `DefaultWeapons`: This lets you configure the default weapon for each weapon allocation type. The type of this config
  is map of `Team => WeaponAllocationType => Item`.
    - The valid keys for `DefaultWeapons` are: `Terrorist` and `CounterTerrorist`
    - Under each of those, the valid keys are:
        - `PistolRound`: The pistol round pistol
        - `Secondary`: The pistol for non-pistol rounds
        - `HalfBuyPrimary`: The primary weapon for half buy rounds
        - `FullBuyPrimary`: The primary weapon for full buy rounds
    - The valid values for each subkey this are any `CsItem` that is a weapon.
      To better understand how `DefaultWeapons` works, here is the default config for `DefaultWeapons` as an example:

```json
{
  "DefaultWeapons": {
    "Terrorist": {
      "PistolRound": "Glock",
      "Secondary": "Deagle",
      "HalfBuyPrimary": "Mac10",
      "FullBuyPrimary": "AK47"
    },
    "CounterTerrorist": {
      "PistolRound": "USPS",
      "Secondary": "Deagle",
      "HalfBuyPrimary": "MP9",
      "FullBuyPrimary": "M4A1"
    }
  }
}
```

#### Other Configuration

- `EnableNextRoundTypeVoting`: Whether to allow voting for the next round type via `!nextround`. `false` by default.
- `NumberOfExtraVipChancesForPreferredWeapon`: When randomly selecting preferred weapons per team (ie. "AWP queue"), how
  many extra chances should VIPs get.
    - The default is 1, meaning VIPs will get 1 extra chance. For example, lets say
      there are 3 players on the team and this config is set to 1. Normally each person would have a 33% chance of
      getting
      the AWP, but in this case, since one of the players is a VIP, the VIP will get a 50% chance of getting the AWP,
      and
      the other two players will each have 25% chance of getting the AWP.
    - If you set this to 0, there will be no preference for VIPs.
- `AllowedWeaponSelectionTypes`: The types of weapon allocation that are allowed.
    - Choices:
        - `PlayerChoice` - Allow players to choose their preferences for the round type
        - `Random` - Everyone gets a random weapon for the round type
        - `Default` - Everyone gets a default weapon for the round type. The defaults are:
            - T Pistol: Glock
            - CT Pistol: USPS
            - T HalfBuy: Mac10
            - CT HalfBuy: MP9
            - T Rifle: AK47
            - CT Rifle: M4A4
    - These will be tried in order of `PlayerChoice`, `Random`, and `Default`. If a player preference is not available,
      or this type is removed from the config, a random weapon will be tried. If random weapons are removed from the
      config, a default weapon will be tried. If default weapons are removed from the config, no weapons will be
      allocated.
- `DatabaseProvider`: Which database provider you want to use. The default is `Sqlite`, which requires no setup. The
  available options are:
    - `Sqlite`
    - `MySql`
- `DatabaseConnectionString`: How you connect to the database
    - The connection string for `Sqlite` probably doesnt need to be changed from the default, but you can change it if
      you want the db file to be in a different location.
        - More info on formatting the string here: https://www.connectionstrings.com/sqlite/
    - The connection string for `MySql` should be configured per instructions
      here: https://www.connectionstrings.com/mysql/

- `MigrateOnStartup`: Whether or not to migrate the database on startup. This defaults to yes for now, but production
  servers may want to change this to false so they can control when database migrations are applied.

### Commands

You can use the following commands to select specific weapon preferences per-user:

- `!gun <weapon> [T|CT]` - Set a preference the chosen weapon for the team you are currently on, or T/CT if provided
    - For example, if you are currently a terrorist and you do `!gun galil`, your preference for rifle rounds will be
      Galil
- `!guns` - Opens up a chat-based menu for setting weapon preferences.
- `!awp` - Toggle whether or not you want to get an AWP.
- `!removegun <weapon> [T|CT]` - Remove a preference for the chosen weapon for the team you are currently on, or T/CT if
  provided
    - For example, if you previously did `!gun galil` while a terrorist, and you do `!removegun galil` while a
      terrorist, you will no longer prefer the galil, and will instead get a random weapon
- `!nextround` - Vote for the next round type. Can be enabled with the `EnableNextRoundTypeVoting` config, which
  is `false` by default.
- `!setnextround <P|H|F>` - For admins only. Force the next round to be the selected type.
- `!reload_allocator_config` - For admins only. Reload the JSON config in-place.

# Building

To automatically copy the built DLL to your running server location, set the build variable `CopyPath` to the folder
where the mod should be copied to. *This only works on Windows.*

Notes:

- Run the dedicated server
  with `start cs2.exe -dedicated -insecure +game_type 0 +game_mode 0 +map de_dust2 +servercfgfile server.cfg`
