
echo off

if not exist build mkdir build
pushd build
:: Build the app.
cmake -G "MinGW Makefiles" ..
make
popd
