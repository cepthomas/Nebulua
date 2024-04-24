
echo off

rem Lua path. https://www.lua.org/pil/8.1.html
set LUA_PATH=?;?.lua;%~dp0..\..\lua_code\?.lua;%~dp0..\..\lbot;

:: Run lua tests. test_defs.lua  test_nebulua.lua  test_bar_time.lua
lua %~dp0..\..\lbot\pnut_runner.lua  %~dp0test_nebulua
