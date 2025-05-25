:: Convert spec into Nebulua interop code.
:: This requires the top level folder of https://github.com/cepthomas/LuaInterop.

echo off
cls

:: Save paths.
set "ORIGINAL_DIR=%cd%"
cd ..
set "NEB_DIR=%cd%"

:: Go into your LuaInterop dir.
cd C:\Dev\Libs\LuaInterop

:: Setup for lua.
set LUA_PATH=;%NEB_DIR%\LBOT\?.lua;?.lua;

:: Gen the C and C++ components from the spec.
lua gen_interop.lua -c "%ORIGINAL_DIR%\interop_spec.lua" "%ORIGINAL_DIR%"
lua gen_interop.lua -cppcli "%ORIGINAL_DIR%\interop_spec.lua" "%ORIGINAL_DIR%"

:: Go home.
cd %ORIGINAL_DIR%
