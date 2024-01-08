
echo off
cls

:: Setup dirs and files.
mkdir build
pushd build
rem del /F /Q *.*

:: Build the app.
cmake -G "MinGW Makefiles" ..
make

rem :: Copy test files.
rem copy ..\test\files\* build

rem test

popd

rem pause
