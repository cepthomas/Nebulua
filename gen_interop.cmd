:: Nebulua. Convert spec into interop code.

echo off
cls

pushd LBOT
lua gen_interop.lua -c ..\Script\interop_spec.lua ..\Script
lua gen_interop.lua -cppcli ..\Script\interop_spec.lua ..\Script
popd

rem pause
