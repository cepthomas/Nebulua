:: Convert spec into interop library.
:: Should not need to be regenerated after the api is finalized.
:: Note this is specific to my configuration only.

echo off
cls

set ODIR=%cd%
cd ..\
set LBOT_DIR=%cd%\LBOT
set LUA_PATH=%LBOT_DIR%\?.lua;%ODIR%\?.lua;?.lua;
cd %DEV_PATH%\Libs\LuaInterop\Generator
:: Gen the C and C++ components from the spec.
lua do_gen.lua -c %ODIR%\interop_spec.lua %ODIR%
lua do_gen.lua -cppcli %ODIR%\interop_spec.lua %ODIR%
cd %ODIR%
