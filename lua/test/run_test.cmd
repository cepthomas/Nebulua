
cls
echo off

:: Script to run Nebulua unit tests.

:: Set the lua path.
set LUA_PATH=;;C:\Dev\repos\Lua\LuaBagOfTricks\?.lua;C:\Dev\repos\Audio\Nebulua\lua\?.lua;C:\Dev\repos\Audio\Nebulua\lua\test\?.lua;

:: Run the unit tests. test_defs.lua  test_nebulua.lua
lua C:\Dev\repos\Lua\LuaBagOfTricks\pnut_runner.lua test_nebulua.lua

pause
