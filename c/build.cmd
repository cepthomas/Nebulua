
echo off

cls

mkdir build
cd build
del /F /Q *.*

rem Build the app.
cmake -G "MinGW Makefiles" ..
make
cd ..

rem This really should be done by CMake but it's kind of a pain.
rem copy source\lua\*.lua build

rem pause
