
echo off

:: Convert spec into interop files.

:: Args.
set spec_fn=%~dp0%interop_spec.lua
set out_path=%~dp0%interop

:: Build the interop.
pushd "%LBOT%"
lua gen_interop.lua -ch %spec_fn% %out_path%

popd
