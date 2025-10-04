:: Convert spec into interop library.

echo off
cls

set ODIR=%cd%
cd ..\
set LDIR=%cd%\LBOT
set LUA_PATH=%LDIR%\?.lua;%ODIR%\?.lua;?.lua;
:: yuck:
cd ..\..\Libs\LuaInterop\Generator
:: Gen the C and C++ components from the spec.
lua gen_interop.lua -c %ODIR%\interop_spec.lua %ODIR%
lua gen_interop.lua -cppcli %ODIR%\interop_spec.lua %ODIR%
cd %ODIR%
