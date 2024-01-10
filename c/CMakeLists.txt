cmake_minimum_required(VERSION 3.5)
project(nebulua)

set(CMAKE_C_STANDARD 99)
set(CMAKE_CXX_STANDARD 11)


# if(NOT DEFINED DO_TEST)
#     message(FATAL_ERROR "No target selected")
# else()
#     message(STATUS "DO_TEST set to ${DO_TEST}")
# endif()


# TODO2 fix these paths.
set(LUA "C:/Program Files/Lua")
set(LBOT_C "C:/Dev/repos/Lua/LuaBagOfTricks/c")
set(FTIMER "C:/Dev/repos/C/c_fast_timer/source")
set(CBOT "C:/Dev/repos/C/c_bag_of_tricks")

# needs -g to debug. TODO2 clean these up:
set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -g -Wall \
    -Wno-unused-function -Wno-uninitialized \
    -Wno-deprecated-declarations -Wno-unused-variable -Wno-unused-but-set-variable -Wno-discarded-qualifiers")

# Header files.
if(DEFINED DO_TEST)
    
    include_directories(
        .
        ..
        ${LUA}/include
        ${CBOT}/source
        ${CBOT}/pnut
        )

    add_executable(nebulua
        test/main.cpp
        test/test_cli.cpp
        ./private/cli.c
        ${CBOT}/pnut/pnut.cpp
        ${CBOT}/source/private/logger.c
        )

else()
    include_directories(
        .
        ${LUA}/include
        ${LBOT_C}
        ${FTIMER}
        ${CBOT}/source
        )

    add_executable(nebulua
        main.c
        ./private/nebcommon.c
        ./private/cli.c
        ./private/devmgr.c
        ./private/luainterop.c
        ./private/luainteropwork.c
        ${CBOT}/source/private/logger.c
        ${LBOT_C}/private/diag.c
        ${LBOT_C}/private/luaex.c
        ${FTIMER}/private/ftimer.c
        ${FTIMER}/private/stopwatch.c
        ${FTIMER}/private/timeanalyzer.c
        )
endif()


# Libs.
target_link_libraries(nebulua
    ${LUA}/Win64/lua54.dll
    libwinmm.a
    )
