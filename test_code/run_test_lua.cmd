
echo off

:: Run lua tests. test_defs.lua  test_nebulua.lua
set LUA_PATH=;;C:\Dev\repos\Lua\Nebulua\lua_code\?.lua;C:\Dev\repos\Lua\Nebulua\test_code\?.lua;C:\Dev\repos\Lua\LuaBagOfTricks\?.lua;
rem lua C:\Dev\repos\Lua\LuaBagOfTricks\pnut_runner.lua  test_defs.lua
lua C:\Dev\repos\Lua\LuaBagOfTricks\pnut_runner.lua  test_nebulua.lua
