
echo off

:: Run lua tests. test_defs.lua  test_nebulua.lua  test_bar_time.lua  TODO1 fix/run.

set LUA_PATH=;;C:\Dev\repos\Lua\Nebulua\lua_code\?.lua;C:\Dev\repos\Lua\Nebulua\test\?.lua;C:\Dev\repos\Lua\LuaBagOfTricks\?.lua;

lua C:\Dev\repos\Lua\LuaBagOfTricks\pnut_runner.lua  test_nebulua.lua
