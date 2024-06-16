
echo off

pushd "CommandProc\bin\x64\Debug\net8.0-windows"
TestCommandProc.exe
popd

pushd "Core\bin\x64\Debug\net8.0-windows"
TestCore.exe
popd

pushd "Interop\bin\x64\Debug\net8.0-windows"
TestInterop.exe
popd

pushd "Misc\bin\x64\Debug\net8.0-windows"
TestMisc.exe
popd
