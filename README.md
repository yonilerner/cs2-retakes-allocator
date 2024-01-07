# CS2 Retakes Allocator

Very early version, should be working as described below.

## Retakes

This plugin is made to run alongside B3none's retakes implementation: https://github.com/b3none/cs2-retakes

## Installation

- Ensure you have https://github.com/b3none/cs2-retakes installed already
- Update the `RetakesPlugin` config to have `EnableFallbackAllocation` disabled
- Download a release from https://github.com/yonilerner/cs2-retakes-allocator/releases
- Extract the zip archive and upload the `RetakesAllocator` plugin to your CounterStrikeSharp plugins folder on your
  server
    - [ADVANCED] If you want to reduce the upload size, you can delete all runtimes in the `RetakesAllocator` folder
      except the one needed for your particular server

## How it works

The plugin will randomly select a type of round, and allocate weapons and utility based on that:

- Pistol (15% chance)
    - Randomly get a pistol
    - Kevlar and no helmet
    - Everyone gets a flash or smoke
    - One CT gets a defuse kit
- Half buy (25% chance)
    - Randomly get an mid-range weapon (SMG/Shotgun)
    - Everyone gets kevlar and helmet
    - Everyone gets one of any nade, and has a 50% chance of getting a 2nd nade
- Full buys (60% chance)
    - Randomly get a rifle
    - Everyone gets kevlar and helmet
    - Everyone gets one of any nade, and has a 50% chance of getting a 2nd nade
    - 20% chance of getting an AWP

You can also use the following commands to select specific weapon preferences:

- `!weapon <weapon>` - Set a preference the chosen weapon for the team you are currently on
    - For example, if you are currently a terrorist and you do `!weapon galil`, your preference for rifle rounds will be
      Galil
- `!removeweapon <weapon>` - Remove a preference for the chosen weapon for the team you are currently on
    - For example, if you previously did `!weapon galil` while a terrorist, and you do `!removeweapon galil` while a
      terrorist, you will no longer prefer the galil, and will instead get a random weapon

## Current Features

- [x] Implement weapon allocation
- [x] Implement armour allocation
- [x] Implement grenade allocation
- [x] Implement different round types
- [x] Per-player weapon preferences
- [ ] Option for more predictable rounds
- [ ] File-based configuration

# Building

- Download a release (I used 142) from https://github.com/roflmuffin/CounterStrikeSharp/releases/ and copy the
  extracted `addon` folder to this project's `CounterStrikeSharp` folder
- [Optional] To automatically copy the built DLL to your running server location, set the build variable `CopyPath` to
  the folder where the mod should be copied to
    - *This only works on Windows*

Notes:

- Run the dedicated server
  with `start cs2.exe -dedicated -insecure +game_type 0 +game_mode 0 +map de_dust2 +servercfgfile server.cfg`
