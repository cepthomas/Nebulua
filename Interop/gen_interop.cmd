:: Convert spec into interop library.
:: Should not need to be regenerated after the api is finalized.
:: Note this is specific to my configuration only.
:: Requires env var DEV_PATH to be set.

echo off
cls

set INTEROP_DIR=%cd%
cd ..\
set LBOT_DIR=%cd%\LBOT
set LUA_PATH=%LBOT_DIR%\?.lua;%INTEROP_DIR%\?.lua;?.lua;
cd %DEV_PATH%\Libs\LuaInterop\Generator
:: Gen the C and C++ components from the spec.
lua do_gen.lua -c %INTEROP_DIR%\interop_spec.lua %INTEROP_DIR%
lua do_gen.lua -cppcli %INTEROP_DIR%\interop_spec.lua %INTEROP_DIR%
cd %INTEROP_DIR%
