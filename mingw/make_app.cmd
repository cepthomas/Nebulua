
echo off

if not exist build_app mkdir build_app
pushd build_app
:: Build the app.
cmake -G "MinGW Makefiles" ..
make
popd
