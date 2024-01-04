# CS2 Retakes Allocator

Very early version, should be working as described below.

## How it works
The plugin will randomly select a type of round, and allocate weapons and utility based on that:
- Pistol (15% chance)
  - Randomly get a pistol
  - Kevlar and no helmet
  - Everyone gets a flash or smoke
  - One CT gets a defuse kit
- Half buy (25% chance)
  - Randomly get an SMG
  - Everyone gets kevlar and helmet
  - Everyone gets one of any nade, and has a 50% chance of getting a 2nd nade
- Full buys (60% chance)
  - Randomly get a rifle
  - Everyone gets kevlar and helmet
  - Everyone gets one of any nade, and has a 50% chance of getting a 2nd nade
  - 20% chance of getting an AWP

## Retakes
This plugin is made to run alongside B3none's retakes implementation: https://github.com/b3none/cs2-retakes

## Current Features
- [x] Implement weapon allocation
- [x] Implement armour allocation
- [x] Implement grenade allocation 
- [x] Implement different round types
- [ ] Option for more predictable rounds
- [ ] File-based configuration
- [ ] Per-player weapon preferences

# Building
- Download a release (I used 142) from https://github.com/roflmuffin/CounterStrikeSharp/releases/ and copy the extracted `addon` folder to this project's `CounterStrikeSharp` folder
- [Optional] To automatically copy the built DLL to your running server location, set the build variable `CopyPath` to the folder where the mod should be copied to
  - *This only works on Windows*

Notes:
- Run the dedicated server with `start cs2.exe -dedicated -insecure +game_type 0 +game_mode 0 +map de_dust2 +servercfgfile server.cfg`
