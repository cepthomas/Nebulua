
echo off
cls

if not exist .\c\build mkdir .\c\build
rem del /F /Q .\c\build\*.*

pushd .\c\build

:: Build the app.
cmake -G "MinGW Makefiles" ..
make

popd

:: Copy lua files to output. This really should be done by CMake but it's kind of a pain.
copy lua\*.lua c\build\
