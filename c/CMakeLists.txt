
# Build the nebulua app or its unit tests.
# This is a 64 bit build. PATH must include ...\mingw64\bin.

cmake_minimum_required(VERSION 3.5)
project(nebulua)

set(CMAKE_C_STANDARD 99)
set(CMAKE_CXX_STANDARD 11)

set(LUA  "C:/Lua")
# set(LBOT "C:/Dev/repos/Lua/LuaBagOfTricks/c")
set(FTMR "C:/Dev/repos/C/c_fast_timer/source")
set(CBOT "C:/Dev/repos/C/c_bag_of_tricks")
set(EMBL "C:/Dev/repos/C/c_emb_lua")

set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -g -Wall")
# -Wno-unused-function -Wno-uninitialized -Wno-deprecated-declarations -Wno-unused-variable -Wno-unused-but-set-variable -Wno-discarded-qualifiers")

# Header files.
if(DEFINED DO_TEST)

    include_directories(
        .
        ..
        ${LUA}/include
        ${CBOT}/source
        ${CBOT}/pnut
        ${EMBL}/source
        )

    add_executable(nebulua
        test/main.cpp
        test/test_cli.cpp
        ./private/cli.c
        ${CBOT}/pnut/pnut.cpp
        ${CBOT}/source/private/logger.c
        ${EMBL}/source/private/luautils.c
        )

else()

    include_directories(
        .
        ${LUA}/include
        ${CBOT}/source
        ${EMBL}/source
        ${LBOT}
        ${FTMR}
        )

    add_executable(nebulua
        main.c
        ./private/nebcommon.c
        ./private/cli.c
        ./private/devmgr.c
        ./private/luainterop.c
        ./private/luainteropwork.c
        ${CBOT}/source/private/logger.c
        ${EMBL}/source/private/luautils.c
        ${EMBL}/source/private/luaex.c
        # ${LBOT}/private/diag.c
        # ${LBOT}/private/luaex.c
        ${FTMR}/private/ftimer.c
        ${FTMR}/private/stopwatch.c
        ${FTMR}/private/timeanalyzer.c
        )

endif()

# Libs. 64 bit build.
target_link_libraries(nebulua
    ${LUA}/Win64/lua54.dll
    libwinmm.a
    )
