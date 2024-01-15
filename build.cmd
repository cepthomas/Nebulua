
echo off
cls

if not exist build mkdir build
rem del /F /Q build\*.*

pushd build

:: Build the app.
cmake -G "MinGW Makefiles" ..
make

popd
