
# Build the nebulua app or its unit tests.
# This is a Windows 64 bit build. PATH must include ...\mingw64\bin.
# TODO2 Need a build/package/release.


cmake_minimum_required(VERSION 3.5)
project(nebulua)

set(CMAKE_C_STANDARD 99)
set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -g -Wall")
set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -g -Wall")

set(LUA  "C:/Lua")
set(CBOT "C:/Dev/repos/C/c_bag_of_tricks")


# Header files.
if(DO_TEST)

    include_directories(
        source
        test
        ${LUA}/include
        ${CBOT}/source
        ${CBOT}/pnut
        )

    add_executable(nebulua
        source/nebcommon.c
        test/main.cpp
        test/test_nebcommon.cpp
        ${CBOT}/pnut/pnut.cpp
        ${CBOT}/source/private/logger.c
        )

else()

    include_directories(
        source
        ${LUA}/include
        ${CBOT}/source
        )

    add_executable(nebulua
        source/main.c
        source/exec.c
        source/nebcommon.c
        source/devmgr.c
        source/luainterop.c
        source/luainteropwork.c
        source/luaex.c
        ${CBOT}/source/private/logger.c
        ${CBOT}/source/private/cli.c
        ${CBOT}/source/private/ftimer.c
        ${CBOT}/source/private/stopwatch.c
        ${CBOT}/source/private/timeanalyzer.c
        )

endif()

# Libs.
target_link_libraries(nebulua
    ${LUA}/Win64/lua54.dll
    libwinmm.a
    )
