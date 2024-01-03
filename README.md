# CS2 Retakes Allocator
**Extremely early dont even look at it yet.**

# Building
- Download a release (I used 142) from https://github.com/roflmuffin/CounterStrikeSharp/releases/ and copy the extracted `addon` folder to this project's `CounterStrikeSharp` folder
- [Optional] To automatically copy the built DLL to your running server location, set the build variable `CopyPath` to the folder where the mod should be copied to
  - *This only works on Windows*

Notes:
- Run the dedicated server with `start cs2.exe -dedicated -insecure +game_type 0 +game_mode 0 +map de_dust2 +servercfgfile server.cfg`