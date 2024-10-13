
echo off

rem Lua path. https://www.lua.org/pil/8.1.html
set LUA_PATH=?;?.lua;%~dp0..\..\lua\?.lua;%~dp0..\?.lua;

:: Run lua tests. test_defs.lua  test_nebulua.lua  test_bar_time.lua
lua %~dp0pnut_runner.lua  %~dp0test_nebulua
lua %~dp0pnut_runner.lua  %~dp0test_defs
lua %~dp0pnut_runner.lua  %~dp0test_bar_time
