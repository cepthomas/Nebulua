
echo off

rem if not exist build_test mkdir build_test
rem mkdir build_test
rem del /F /Q build_test\*.*

pushd build_test

rem :: Build the c test app.
rem cmake -G "MinGW Makefiles" -DDO_TEST=1 ..
rem make

:: Run c tests.
nebulua

popd


:: Run lua tests.  test_defs.lua  test_nebulua.lua
set LUA_PATH=;;C:\Dev\repos\Lua\Nebulua\lua\?.lua;C:\Dev\repos\Lua\Nebulua\test\?.lua;C:\Dev\repos\Lua\LuaBagOfTricks\?.lua;
rem lua C:\Dev\repos\Lua\LuaBagOfTricks\pnut_runner.lua  test_defs.lua
rem lua C:\Dev\repos\Lua\LuaBagOfTricks\pnut_runner.lua  test_nebulua.lua

