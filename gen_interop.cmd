
echo off

:: Convert spec into interop files.

:: Args.
set spec_fn=%~dp0%interop_spec.lua
set out_path=%~dp0%source_code
rem echo %spec_fn%
rem echo %out_path%

:: Build the interop.
pushd "..\..\Lua\LuaBagOfTricks"
lua gen_interop.lua -ch -d %spec_fn% %out_path%

popd
