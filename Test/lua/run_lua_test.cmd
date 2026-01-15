
echo off
cls

set "ODIR=%cd%"
set LUA_PATH=%ODIR%\?.lua;%ODIR%\..\..\lua\?.lua;?.lua;;

pushd ..\..\LBOT

rem lua pnut_runner.lua  %ODIR%\test_defs
rem lua pnut_runner.lua  %ODIR%\test_music_time
lua pnut_runner.lua  %ODIR%\test_api

rem lua pnut_runner.lua  %ODIR%\test_defs %ODIR%\test_music_time %ODIR%\test_api

popd
