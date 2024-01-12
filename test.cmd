
echo off

if not exist .\c\build_test mkdir .\c\build_test
rem mkdir .\c\build_test
rem del /F /Q .\c\build_test\*.*

pushd .\c\build_test

:: Build the c app.
cmake -G "MinGW Makefiles" -DDO_TEST=1 ..
make

:: Run c tests.
nebulua

popd

:: Run lua tests.
pushd lua\test\
:: Set the lua path.
set LUA_PATH=;;C:\Dev\repos\Lua\LuaBagOfTricks\?.lua;C:\Dev\repos\Audio\Nebulua\lua\?.lua;C:\Dev\repos\Audio\Nebulua\lua\test\?.lua;

:: Run the unit tests. test_defs.lua  test_nebulua.lua
lua C:\Dev\repos\Lua\LuaBagOfTricks\pnut_runner.lua test_nebulua.lua

popd
