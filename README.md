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

## Game Data/Signatures (Optional Information)
This section is optional. If you dont care, you can skip it. The defaults are fine.

This plugin relies on some function signatures that:
- Regularly break with game updates:
  - `GetCSWeaponDataFromKey`
  - `CCSPlayer_ItemServices_CanAcquire`
- Are not included in the default CS# signatures:
  - `GiveNamedItem2`

Custom game data signatures are maintained in https://github.com/yonilerner/cs2-retakes-allocator/blob/main/Resources/RetakesAllocator_gamedata.json. There are a few ways to keep these up to date on your server:
- If you want the plugin to automatically download the signatures, you can do so by running the plugin with the `AutoUpdateSignatures` config set to `true`. **This is the recommended approach**. See more below in the "Configuration" section.
- If you want to manually download the signatures, you can do so by downloading the `RetakesAllocator_gamedata.json` file from Github and placing it in the `RetakesAllocator/gamedata` folder in the plugin. You may have to create that folder if it does not exist.

If you do not want to use any custom game data/signatures, you can disable `AutoUpdateSignatures` and `CapabilityWeaponPaints`. If you do this (and if you previously had downloaded custom game data, make sure to delete the `RetakesAllocator/gamedata/RetakesAllocator_gamedata.json` file), the plugin will fallback to using the default CS# signatures. See more below in the "Configuration" section.

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


- `ZeusPreference`: Whether or not to give a Zeus. Options are `Always` or `Never`. Defaults to `Never`.
- `AllowPreferredWeaponForEveryone`: If `true`, everyone can get the AWP. This overrides every other "preferred" weapon
  setting. Defaults to `false`.
- `MaxPreferredWeaponsPerTeam`: The maximum number of AWPs for each team.
- `MinPlayersPerTeamForPreferredWeapon`: The minimum number of players on each team necessary for someone to get an AWP.
- `ChanceForPreferredWeapon`: The % chance that the round will have an AWP.

#### Nade Configuration

- `MaxNades`: You can set the maximum number of each type of nade for each team and on each map (or default). By default
  the config includes some limits that you may want to change.
  The way `MaxNades` works is that the GLOBAL option sets the max for *all* maps, and then you can also specify subsets
  of the config for specific maps.
  For example, if your config is:

```json
{
  "MaxNades": {
    "GLOBAL": {
      "Terrorist": {
        "Flashbang": 2,
        "Smoke": 1,
        "Molotov": 1,
        "HighExplosive": 1
      },
      "CounterTerrorist": {
        "Flashbang": 2,
        "Smoke": 1,
        "Molotov": 2,
        "HighExplosive": 1
      }
    }
  }
}
```

but you specifically want to allow 2 smokes for CT on mirage, you can do:

```json
{
  "MaxNades": {
    "GLOBAL": {
      "Terrorist": {
        "Flashbang": 2,
        "Smoke": 1,
        "Molotov": 1,
        "HighExplosive": 1
      },
      "CounterTerrorist": {
        "Flashbang": 2,
        "Smoke": 1,
        "Incendiary": 2,
        "HighExplosive": 1
      }
    },
    "de_mirage": {
      "CounterTerrorist": {
        "Smoke": 2
      }
    }
  }
}
```

This will keep the defaults the same for everything but override just CT smokes on mirage.

The valid keys for nades on `Terrorist` are:

- `Flashbang`
- `Smoke`
- `Molotov`
- `HighExplosive`

The valid keys for nades on `CounterTerrorist` are:

- `Flashbang`
- `Smoke`
- `Incendiary`
- `HighExplosive`

If you mix up `Incendiary` and `Molotov`, the plugin will fix it for you.

- `MaxTeamNades` - This config works similarly to `MaxNades`, except it affects the max number of nades an entire team
  can have. The structure is the same as `MaxNades` except that after the map and team keys, it maps a round type to a
  max nade setting. The possible max nade settings are:
    - `One`, `Two`, ... until `Ten`
    - `AveragePointFivePerPlayer` (rounds up)
    - `AverageOnePerPlayer` (rounds up)
    - `AverageOnePointFivePerPlayer` (rounds up)
    - `AverageTwoPerPlayer` (rounds up)
    - `None`

*NOTE: There is a bug right now where the plugin will not always give the maximum number of nades, even if players have
room for it*.

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
    - If you set this to -1, only VIPs can get the AWP
- `ChanceForPreferredWeapon`: This allows you to determine chance of players getting preferred weapon. (ie. 100 = %100, 50 = %50)
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
- `LogLevel`: Desired level of logging. Can be set to `Debug` or `Trace` when collecting information for a bug report.
  You probably want the default, which is `Information`. I strongly recommend against setting any higher than `Warning`.
  The options are:
    - `Trace` (spam of information)
    - `Debug` (some useful information for debugging)
    - `Information`
    - `Warning` (warnings & errors only)
    - `Error` (errors only)
    - `Critical`
    - `None` (no logs at all; use with caution)
- `ChatMessagePluginName`: The name that you want to appear between [] in chat messages from the plugin. Defaults
  to `Retakes`.
    - For example, `[Retakes] Next round will be a Terrorist round.`
- `ChatMessagePluginPrefix`: The *entire* prefix that appears in front of chat messages. If set, this
  overrides `ChatMessagePluginName`. If you want the prefix to be colored, the config must also specify the colors. It
  must also specify a space after the prefix if you want one.
- `MigrateOnStartup`: Whether or not to migrate the database on startup. This defaults to yes for now, but production
  servers may want to change this to false so they can control when database migrations are applied.
- `EnableRoundTypeAnnouncement`: Whether or not to announce the round type.
- `EnableRoundTypeAnnouncementCenter`: Whether or not to announce the round type in the center of the users screen. Only
  applies if `EnableRoundTypeAnnouncement` is also set to `true`.
- `UseOnTickFeatures`: Set to false if you want better performance and dont want any OnTick features, including:
  - Bombsite center announcement
  - Advanced gun menu
- `AutoUpdateSignatures `: When true, the plugin will always try to download the latest custom game data/signatures on startup. A game server restart may be required to apply the new signatures after they have been downloaded. If this is disabled, the plugin will fallback to using the default CS# game data/signatures.
- `CapabilityWeaponPaints`: When true, will try to use the custom game data `GiveNamedItem2` that will maintain weapon paints in non-standard situations. This is enabled by default for backwards compatibility, but is less stable. If this option is enabled, `AutoUpdateSignatures` should also be enabled. If you dont want to use `AutoUpdateSignatures`, at least ensure that the custom game data/signatures are updated correctly, since this `GiveNamedItem2` is not in the default game data/signatures.

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
- `!print_config <name>` - For admins only. Print out the config with the given name.

# Building

To automatically copy the built DLL to your running server location, set the build variable `CopyPath` to the folder
where the mod should be copied to. *This only works on Windows.*

Notes:

- Run the dedicated server
  with `start cs2.exe -dedicated -insecure +game_type 0 +game_mode 0 +map de_dust2 +servercfgfile server.cfg`
