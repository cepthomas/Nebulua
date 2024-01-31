# Build the nebulua app or its unit tests.

cmake_minimum_required(VERSION 3.5)

project(nebulua)

# This is a Windows 64 bit build. PATH must include \your\path\mingw64\bin.
if(NOT CMAKE_SIZEOF_VOID_P EQUAL 8)
    message(FATAL_ERROR "Project must be built for 64 bit")
endif()

set(CMAKE_C_STANDARD 99)
set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -g -Wall")
set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -g -Wall")

set(LUA  "C:/Lua")
set(CBOT "C:/Dev/repos/C/c_bag_of_tricks")


# Header files.
if(DO_TEST)

    include_directories(
        ../source
        ../test
        ${LUA}/include
        ${CBOT}/source
        ${CBOT}/pnut
        )

    add_executable(nebulua
        main.cpp
        ../source/nebcommon.c
        ../source/devmgr.c
        ../test/test_nebcommon.cpp
        ../test/test_devmgr.cpp
        ${CBOT}/pnut/pnut.cpp
        ${CBOT}/source/private/logger.c
        )

else()

    include_directories(
        ../source
        ${LUA}/include
        ${CBOT}/source
        )

    add_executable(nebulua
        main.c
        ../source/exec.c
        ../source/nebcommon.c
        ../source/devmgr.c
        ../source/luainterop.c
        ../source/luainteropwork.c
        ../source/luaex.c
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
