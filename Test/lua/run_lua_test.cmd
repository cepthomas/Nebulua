
echo off
cls

:: Fix paths for lua and luarocks.
set "ODIR=%cd%"
set LUA_PATH=%ODIR%\?.lua;%ODIR%\..\..\lua\?.lua;?.lua;%APPDATA%\luarocks\share\lua\5.4\?.lua;%APPDATA%\luarocks\share\lua\5.4\?\init.lua;;
SET LUA_CPATH=%APPDATA%\luarocks\lib\lua\5.4\?.dll;;
SET PATH=%PATH%;%APPDATA%\luarocks\bin

pushd ..\..\LBOT

rem lua pnut_runner.lua  %ODIR%\test_defs
rem lua pnut_runner.lua  %ODIR%\test_music_time
lua pnut_runner.lua  %ODIR%\test_api

rem lua pnut_runner.lua  %ODIR%\test_defs %ODIR%\test_music_time %ODIR%\test_api

popd
