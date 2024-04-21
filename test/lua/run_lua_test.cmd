
echo off

set LBOT=C:\Dev\repos\Lua\LuaBagOfTricks

rem Lua path. https://www.lua.org/pil/8.1.html
set LUA_PATH=?;?.lua;%~dp0..\..\lua_code\?.lua;%LBOT%\?.lua;%~dp0..\files\?.lua;

:: Run lua tests. test_defs.lua  test_nebulua.lua  test_bar_time.lua

lua %LBOT%\pnut_runner.lua  %~dp0test_nebulua
