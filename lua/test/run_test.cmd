
cls
echo off

:: Script to run LuaBagOfTricks unit tests.

:: Set the lua path.
set LUA_PATH=;;^
C:\Dev\repos\Lua\LuaBagOfTricks\?.lua;^
C:\Dev\repos\Audio\Nebulua\lua\?.lua;^
C:\Dev\repos\Audio\Nebulua\lua\test\?.lua;

:: Run the unit tests. test_defs.lua  test_nebulua.lua
rem pushd ".."
rem lua pnut_runner.lua test\test_nebulua.lua
rem popd


lua C:\Dev\repos\Lua\LuaBagOfTricks\pnut_runner.lua test_nebulua

pause
