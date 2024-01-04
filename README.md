# CS2 Retakes Allocator
Very early version, somewhat working **MAY HAVE BUGS / USE WITH CAUTION**

## Retakes
This plugin is made to run alongside B3none's retakes implementation: https://github.com/b3none/cs2-retakes

## Progress
- [x] Implement weapon allocation
- [x] Implement armour allocation
- [x] Implement grenade allocation 
- [x] Implement different round types
- [ ] Implement config file to set round type percentages
- [ ] Store a players selection in a database table for stored preferences across multiple servers

# Building
- Download a release (I used 142) from https://github.com/roflmuffin/CounterStrikeSharp/releases/ and copy the extracted `addon` folder to this project's `CounterStrikeSharp` folder
- [Optional] To automatically copy the built DLL to your running server location, set the build variable `CopyPath` to the folder where the mod should be copied to
  - *This only works on Windows*

Notes:
- Run the dedicated server with `start cs2.exe -dedicated -insecure +game_type 0 +game_mode 0 +map de_dust2 +servercfgfile server.cfg`
