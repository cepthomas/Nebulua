
echo off
cls

mkdir .\c\build
del /F /Q .\c\build\*.*

pushd .\c\build

rem Build the app.

rem cmake -G "MinGW Makefiles" ..
cmake -G "MinGW Makefiles" -DDO_TEST=1 ..


make
popd

rem Copy lua files to output. This really should be done by CMake but it's kind of a pain.
copy lua\*.lua c\build\


rem String syntax
rem    IF [/I] [NOT] item1==item2 command
rem    IF [/I] [NOT] "item1" == "item2" command
rem    IF [/I] item1 compare-op item2 command
rem    IF [/I] item1 compare-op item2 (command) ELSE (command)
rem Error Check Syntax
rem    IF [NOT] DEFINED variable command
rem    IF [NOT] ERRORLEVEL number command 

rem To test for the existence of a command line parameter - use empty brackets like this:
rem IF [%1]==[] ECHO Value Missing
rem or
rem IF [%1] EQU [] ECHO Value Missing   