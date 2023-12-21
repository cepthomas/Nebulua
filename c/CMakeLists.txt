cmake_minimum_required(VERSION 3.5)
project(nebulua)

set(CMAKE_C_STANDARD 99)
set(CMAKE_CXX_STANDARD 11)

# TODO make generic or part of project ../lib
set(LUA "C:/Program Files/Lua")
set(FTIMER "C:/Dev/repos/C/c_fast_timer/source")

# needs -g to debug. TODO clean these up:
set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -g -Wall -Wno-deprecated-declarations -Wno-unused-variable -Wno-unused-but-set-variable -Wno-discarded-qualifiers")

include_directories(
    ${LUA}/include
    ${FTIMER}
    )

# Source files.
add_executable(nebulua
    main.c
    logger.c
    common.c
    luainterop.c
    luainteropwork.c
    ${FTIMER}/private/ftimer.c
    ${FTIMER}/private/stopwatch.c
    )

# Libs.
target_link_libraries(nebulua
    ${LUA}/lua54.dll
    libwinmm.a
    )
