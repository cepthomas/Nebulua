:: Nebulua.
:: Recommended to create a symlink: mklink /d some_path\Nebulua\LBOT other_path\LuaBagOfTricks

:: Convert spec into interop code.

echo off
cls

pushd LBOT
rem set LUA_PATH="%ODIR%\?.lua";?.lua;;
lua gen_interop.lua -c ..\Script\interop_spec.lua ..\Script
lua gen_interop.lua -cppcli ..\Script\interop_spec.lua ..\Script
popd

rem set "ODIR=%cd%"
rem pushd LBOT
rem set LUA_PATH=;;"%ODIR%\?.lua";?.lua;
rem lua gen_interop.lua -c "%ODIR%\interop_spec.lua" "%ODIR%"
rem lua gen_interop.lua -cppcli "%ODIR%\interop_spec.lua" "%ODIR%"
rem popd

rem pause
