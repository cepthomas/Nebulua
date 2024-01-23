
echo off

goto do_lua

if not exist build_test mkdir build_test
rem mkdir build_test
rem del /F /Q build_test\*.*

pushd build_test

:: Build the c test app.
cmake -G "MinGW Makefiles" -DDO_TEST=1 ..
make

:: Run c tests.
nebulua

popd

:do_lua
:: Run lua tests.  test_stringex.lua  test_defs.lua  test_nebulua.lua
set LUA_PATH=;;C:\Dev\repos\Lua\Nebulua\lua\?.lua;C:\Dev\repos\Lua\Nebulua\test\?.lua;C:\Dev\repos\Lua\LuaBagOfTricks\?.lua;
lua C:\Dev\repos\Lua\LuaBagOfTricks\pnut_runner.lua  test_defs.lua  test_nebulua.lua
