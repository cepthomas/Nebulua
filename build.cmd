
echo off

cls

cd c

mkdir build
cd build
del /F /Q *.*

rem Build the app.
cmake -G "MinGW Makefiles" ..
make
cd ..

cd ..

rem TODO This really should be done by CMake but it's kind of a pain.
rem copy source\lua\*.lua build

rem pause
