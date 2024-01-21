
echo off

goto do_lua

if not exist build_test mkdir build_test
rem mkdir build_test
rem del /F /Q build_test\*.*

pushd build_test

:: Build the c app.
cmake -G "MinGW Makefiles" -DDO_TEST=1 ..
make

:: Run c tests.s
nebulua

popd

:do_lua
:: Run lua tests.
set LUA_PATH=;;C:\Dev\repos\Lua\LuaBagOfTricks\?.lua;C:\Dev\repos\Audio\Nebulua\lua\?.lua;C:\Dev\repos\Audio\Nebulua\lua\test\?.lua;
lua C:\Dev\repos\Lua\LuaBagOfTricks\pnut_runner.lua  test_nebulua.lua  test_defs.lua
