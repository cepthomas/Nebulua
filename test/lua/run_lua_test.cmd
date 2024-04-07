
echo off

set LBOT="%~dp0..\..\lbot"

:: Run lua tests. test_defs.lua  test_nebulua.lua  test_bar_time.lua
set LUA_PATH=;;%~dp0..\..\lua_code\?.lua;%~dp0..\..\lbot\?.lua;%~dp0..\files\?.lua;

lua %LBOT%\pnut_runner.lua  test_defs.lua  test_nebulua.lua  test_bar_time.lua
