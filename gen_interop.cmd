
echo off

:: Convert spec into interop library.

:: Args.
set spec_fn=%~dp0%interop_spec.lua
set out_path=%~dp0%source
rem echo %spec_fn%
rem echo %out_path%

:: Build the interop.
pushd "..\..\Lua\LuaBagOfTricks"
lua gen_interop.lua -ch -d -t %spec_fn% %out_path%

rem :: Relocate to preferred destination.
rem mv -f %out_path%\luainterop.c %out_path%\private

popd
