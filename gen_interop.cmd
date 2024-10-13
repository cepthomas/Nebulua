
echo off

:: Convert spec into interop files.
:: Not generally useful or interesting to other than me.

:: Args.
set spec_fn=%~dp0%interop_spec.lua
set out_path=%~dp0%interop

:: Build the interop.
pushd "..\..\Libs\LuaBagOfTricks"
lua gen_interop.lua -ch %spec_fn% %out_path%
popd
