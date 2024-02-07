# Build the nebulua app or its unit tests.

cmake_minimum_required(VERSION 3.5)

project(nebulua)

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
        ../source_code
        ../test_code
        ${LUA}/include
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
        )

else()

    include_directories(
        ../source_code
        ${LUA}/include
        ${CBOT}/source_code
        )

    add_executable(nebulua
        main.c
        ../source_code/exec.c
        ../source_code/nebcommon.c
        ../source_code/devmgr.c
        ../source_code/luainterop.c
        ../source_code/luainteropwork.c
        ../source_code/luaex.c
        ${CBOT}/source_code/private/logger.c
        # ${CBOT}/source_code/private/cli.c
        ${CBOT}/source_code/private/ftimer.c
        ${CBOT}/source_code/private/stopwatch.c
        )

endif()

# Libs.
target_link_libraries(nebulua
    ${LUA}/Win64/lua54.dll
    libwinmm.a
    )
