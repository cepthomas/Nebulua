
echo off

LBOT = "..\..\lbot"

:: Run lua tests. test_defs.lua  test_nebulua.lua  test_bar_time.lua
set LUA_PATH=;;%~dp0..\..\lua_code\?.lua;%~dp0..\?.lua;%LBOT%\?.lua;

lua %LBOT%\pnut_runner.lua  test_defs.lua  test_nebulua.lua  test_bar_time.lua
