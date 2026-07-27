
:: CLI test runner.

echo off
cls

:: Fix paths for lua.
set LUA_PATH=%cd%\?.lua;%cd%\..\lua\?.lua;?.lua;;

rem set "ODIR=%cd%"
rem set LUA_PATH=%ODIR%\?.lua;%ODIR%\..\lua\?.lua;?.lua;;

rem :: Fix paths for lua and luarocks.
rem set LUA_PATH=%ODIR%\?.lua;%ODIR%\..\lua\?.lua;?.lua;%APPDATA%\luarocks\share\lua\5.4\?.lua;%APPDATA%\luarocks\share\lua\5.4\?\init.lua;;
rem SET LUA_CPATH=%APPDATA%\luarocks\lib\lua\5.4\?.dll;;
rem SET PATH=%PATH%;%APPDATA%\luarocks\bin

pushd ..\LBOT

rem lua pnut_runner.lua  %ODIR%\test_defs
rem lua pnut_runner.lua  %ODIR%\test_music_time
rem lua pnut_runner.lua  %ODIR%\test_api

lua pnut_runner.lua  %ODIR%\test_defs %ODIR%\test_music_time %ODIR%\test_api

popd
