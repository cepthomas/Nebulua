
echo off

if not exist build_test mkdir build_test
rem mkdir build_test
rem del /F /Q build_test\*.*

pushd build_test

:: Build the c test app.
cmake -G "MinGW Makefiles" -DDO_TEST=1 ..
make

popd
