
echo off

rem if not exist build_test mkdir build_test
rem mkdir build_test
rem del /F /Q build_test\*.*

pushd build_test

rem :: Build the c test app.
rem cmake -G "MinGW Makefiles" -DDO_TEST=1 ..
rem make

:: Run c tests.
nebulua

popd
