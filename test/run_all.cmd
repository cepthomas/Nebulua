
echo off

pushd "CommandProc\bin\x64\Debug\net8.0-windows7.0"
TestCommandProc.exe
popd

pushd "Core\bin\x64\Debug\net8.0-windows7.0"
TestCore.exe
popd

pushd "Interop\bin\x64\Debug\net8.0-windows7.0"
TestInterop.exe
popd

pushd "Misc\bin\x64\Debug\net8.0-windows7.0"
TestMisc.exe
popd
