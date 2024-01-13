
echo off
cls

if not exist .\c\build mkdir .\c\build
rem del /F /Q .\c\build\*.*

pushd .\c\build

:: Build the app.
cmake -G "MinGW Makefiles" ..
make

popd

:: Copy lua files to output.
copy lua\*.lua c\build\
