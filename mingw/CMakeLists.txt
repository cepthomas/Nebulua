# Build the nebulua app or its unit tests. TODO2 do something with this? Maybe remove or embed in VS.

cmake_minimum_required(VERSION 3.5)

project(nebulua)

if(NOT CMAKE_SIZEOF_VOID_P EQUAL 8)
    message(FATAL_ERROR "Project uses 64 bit Lua")
endif()

set(CMAKE_C_STANDARD 99)
set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -g -Wall")
set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -g -Wall")

set(CBOT "C:/Dev/repos/C/c_bag_of_tricks")
set(LBOT "C:/Dev/repos/Lua/LuaBagOfTricks")


# Header files.
if(DO_TEST)
    include_directories(
        ../app
        ../test
        ../lua_lib/include
        ${LBOT}/app
        ${CBOT}/app
        ${CBOT}/pnut
        )

    add_executable(nebulua
        main.cpp
        ../app/nebcommon.c
        ../app/devmgr.c
        ../test/test_nebcommon.cpp
        ../test/test_devmgr.cpp
        ${CBOT}/pnut/pnut.cpp
        ${CBOT}/app/private/logger.c
        ${LBOT}/app/luaex.c
        ${LBOT}/app/luautils.c
        )
else()
    include_directories(
        ../app
        ../lua_lib/include
        ${LBOT}/app
        ${CBOT}/app
        )

    add_executable(nebulua
        main.c
        ../app/exec.c
        ../app/nebcommon.c
        ../app/devmgr.c
        ../app/luainterop.c
        ../app/luainteropwork.c
        ${CBOT}/app/private/logger.c
        ${CBOT}/app/private/ftimer.c
        ${CBOT}/app/private/stopwatch.c
        ${LBOT}/app/luaex.c
        ${LBOT}/app/luautils.c
        )
endif()

# Libs.
target_link_libraries(nebulua
    ${PROJECT_SOURCE_DIR}/../lua_lib/lua54.dll
    libwinmm.a
    )
