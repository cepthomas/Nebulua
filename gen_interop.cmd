echo off
cls

:: Convert spec into interop library.

:: Set the lua path.
set LUA_PATH=;;C:\Dev\repos\Lua\LuaBagOfTricks\?.lua;

:: Build the interop.
set sfn=%~dp0%api_spec.lua
pushd "..\..\Lua\LuaBagOfTricks"
lua gen_interop.lua -cs -d -t %sfn% %~dp0
popd