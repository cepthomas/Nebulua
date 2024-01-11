
echo off
cls

if not exist .\c\build_test mkdir .\c\build_test
rem mkdir .\c\build_test
rem del /F /Q .\c\build_test\*.*

pushd .\c\build_test

:: Build the app.
cmake -G "MinGW Makefiles" -DDO_TEST=1 ..
make

:: Run tests.
nebulua

popd
