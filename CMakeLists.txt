
# Build the nebulua app or its unit tests.
# This is a 64 bit build. PATH must include ...\mingw64\bin.

cmake_minimum_required(VERSION 3.5)
project(nebulua)

set(CMAKE_C_STANDARD 99)
set(CMAKE_CXX_STANDARD 11)

# TODO-B make cleaner or into libs:
set(LUA  "C:/Lua")
set(FTMR "C:/Dev/repos/C/c_fast_timer")
set(CBOT "C:/Dev/repos/C/c_bag_of_tricks")
set(EMBL "C:/Dev/repos/C/c_emb_lua")

set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -g -Wall")
# -Wno-unused-function -Wno-uninitialized -Wno-deprecated-declarations -Wno-unused-variable -Wno-unused-but-set-variable -Wno-discarded-qualifiers")

# Header files.
if(DO_TEST)

    include_directories(
        source
        test
        ${LUA}/include
        ${CBOT}/source
        ${CBOT}/pnut
        ${EMBL}/source
        )

    add_executable(nebulua
        source/nebcommon.c
        test/main.cpp
        test/test_nebcommon.cpp
        ${CBOT}/pnut/pnut.cpp
        ${CBOT}/source/private/logger.c
        ${EMBL}/source/private/luautils.c
        )

else()

    include_directories(
        source
        ${LUA}/include
        ${CBOT}/source
        ${EMBL}/source
        ${FTMR}/source
        )

    add_executable(nebulua
        source/main.c
        source/exec.c
        source/nebcommon.c
        source/devmgr.c
        source/luainterop.c
        source/luainteropwork.c
        ${CBOT}/source/private/logger.c
        ${CBOT}/source/private/cli.c
        ${EMBL}/source/private/luautils.c
        ${EMBL}/source/private/luaex.c
        ${FTMR}/source/private/ftimer.c
        ${FTMR}/source/private/stopwatch.c
        ${FTMR}/source/private/timeanalyzer.c
        )

endif()

# Libs. 64 bit build.
target_link_libraries(nebulua
    ${LUA}/Win64/lua54.dll
    libwinmm.a
    )
