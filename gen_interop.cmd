
echo off
cls

:: Convert spec into interop library.

:: Set the lua path.
set LUA_PATH=;;C:\Dev\repos\Lua\LuaBagOfTricks\?.lua;

:: Args.
set spec_fn=%~dp0%interop_spec.lua
set out_path=%~dp0%c
echo %spec_fn%
echo %out_path%
:: Build the interop.
pushd "..\..\Lua\LuaBagOfTricks"
lua gen_interop.lua -ch -d -t %spec_fn% %out_path%
:: Relocate to preferred destination.
mv -f %out_path%\luainterop.c %out_path%\output
popd
