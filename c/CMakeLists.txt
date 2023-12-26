cmake_minimum_required(VERSION 3.5)
project(nebulua)

set(CMAKE_C_STANDARD 99)
set(CMAKE_CXX_STANDARD 11)

# TODO3 make generic or part of project ../lib
set(LUA "C:/Program Files/Lua")
set(C_SRC "C:/Dev/repos/Lua/LuaBagOfTricks/c")
set(FTIMER "C:/Dev/repos/C/c_fast_timer/source")

# needs -g to debug. TODO3 clean these up:
set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -g -Wall -Wno-deprecated-declarations -Wno-unused-variable -Wno-unused-but-set-variable -Wno-discarded-qualifiers")

include_directories(
    ${LUA}/include
    ${C_SRC}
    ${FTIMER}
    )

# Source files.
add_executable(nebulua
    main.c
    common.c
    luainterop.c
    luainteropwork.c
    ${C_SRC}/diag.c
    ${C_SRC}/logger.c
    ${C_SRC}/luaex.c
    ${FTIMER}/private/ftimer.c
    ${FTIMER}/private/stopwatch.c
    )

# Libs.
target_link_libraries(nebulua
    ${LUA}/lua54.dll
    libwinmm.a
    )
