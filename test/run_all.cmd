
echo off

pushd "Cli\bin\x64\Debug\net8.0-windows"
TestCli.exe
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
