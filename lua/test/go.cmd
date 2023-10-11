cls
echo off

rem Script to run lua unit tests.

rem Set the lua path to:
rem   - the lbot dir
rem   - where your test script lives
rem   - where your packages live
rem   . Note the double semicolon includes the standard lua path.
set LUA_PATH=%LUA_PATH%;C:\Dev\repos\LuaBagOfTricks\?.lua;C:\Dev\repos\LuaBagOfTricks\Test\?.lua;C:\Dev\lua\pkg\?.lua;;

rem Run the tests.
lua C:\Dev\repos\LuaBagOfTricks\pnut_runner.lua test_utils test_pnut
rem or like this ->
rem cd C:\Dev\repos\LuaBagOfTricks
rem lua pnut_runner.lua test_pnut
