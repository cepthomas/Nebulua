
echo off
cls

set "ODIR=%cd%"
set LUA_PATH=%ODIR%\?.lua;%ODIR%\..\..\lua\?.lua;?.lua;;

pushd ..\..\LBOT
lua pnut_runner.lua  %ODIR%\test_defs %ODIR%\test_bar_time %ODIR%\test_api
popd
