
echo off
cls

set "ODIR=%cd%"

set LUA_PATH=%ODIR%\?.lua;%ODIR%\..\..\lua\?.lua;?.lua;;

rem echo %LUA_PATH%
rem C:\Dev\Apps\Nebulua\Test\?.lua;C:\Dev\Apps\Nebulua\Test\..\lua\?.lua;?.lua;;


pushd ..\..\LBOT

rem dir

lua pnut_runner.lua  %ODIR%\test_defs %ODIR%\test_bar_time %ODIR%\test_api

popd


rem rem pushd ..

rem set LUA_PATH=.\?.lua;..\LBOT\?.lua;;
rem :: Run the unit tests.
rem lua ..\LBOT\pnut_runner.lua  test_defs test_bar_time test_api

rem rem popd
