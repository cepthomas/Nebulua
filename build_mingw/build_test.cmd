
echo off

if not exist build_test mkdir build_test

rem pushd build_test

:: Build the c test app.
cmake -G "MinGW Makefiles" -DDO_TEST=1 ..
make

rem popd
