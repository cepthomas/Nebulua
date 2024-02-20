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
        ../source_code
        ../test_code
        ../lua_lib/include
        ${LBOT}/source_code
        ${CBOT}/source_code
        ${CBOT}/pnut
        )

    add_executable(nebulua
        main.cpp
        ../source_code/nebcommon.c
        ../source_code/devmgr.c
        ../test_code/test_nebcommon.cpp
        ../test_code/test_devmgr.cpp
        ${CBOT}/pnut/pnut.cpp
        ${CBOT}/source_code/private/logger.c
        ${LBOT}/source_code/luaex.c
        ${LBOT}/source_code/luautils.c
        )
else()
    include_directories(
        ../source_code
        ../lua_lib/include
        ${LBOT}/source_code
        ${CBOT}/source_code
        )

    add_executable(nebulua
        main.c
        ../source_code/exec.c
        ../source_code/nebcommon.c
        ../source_code/devmgr.c
        ../source_code/luainterop.c
        ../source_code/luainteropwork.c
        ${CBOT}/source_code/private/logger.c
        ${CBOT}/source_code/private/ftimer.c
        ${CBOT}/source_code/private/stopwatch.c
        ${LBOT}/source_code/luaex.c
        ${LBOT}/source_code/luautils.c
        )
endif()

# Libs.
target_link_libraries(nebulua
    ${PROJECT_SOURCE_DIR}/../lua_lib/lua54.dll
    libwinmm.a
    )
