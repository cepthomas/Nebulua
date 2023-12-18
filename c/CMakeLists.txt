cmake_minimum_required(VERSION 3.5)
project(nebulua)

set(CMAKE_C_STANDARD 99)
set(CMAKE_CXX_STANDARD 11)

set(LUA "C:/Program Files/Lua") # TODO make generic or part of project ../lib

# needs -g to debug.
set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -g -Wall -Wno-deprecated-declarations -Wno-unused-variable -Wno-unused-but-set-variable")

include_directories(${LUA}/include)

# Source files.
add_executable(nebulua
    main.c
    luainterop.c
    luainteropwork.c
    )

# Libs.
# set(LUA_LIB ../lib/lua54.dll)
set(LUA_LIB ${LUA}/lua54.dll)
# C:\Dev\repos\Lua\NebuluaC\lib

target_link_libraries(nebulua PRIVATE "${LUA_LIB}")


# C:\Dev\repos\Lua\NebuluaC\c\CMakeLists.txt
# # I assume you want to link to a library called foo, its filename is usually something link foo.dll or libfoo.so.
# # 1. Find the library
# # You have to find the library. This is a good idea, even if you know the path to your library. CMake will error out if the library vanished or got a new name. This helps to spot error early and to make it clear to the user (may yourself) what causes a problem.
# # To find a library foo and store the path in FOO_LIB use
# find_library(FOO_LIB foo)
# # CMake will figure out itself how the actual file name is. It checks the usual places like /usr/lib, /usr/lib64 and the paths in PATH.
# # You already know the location of your library. Add it to the CMAKE_PREFIX_PATH when you call CMake, then CMake will look for your library in the passed paths, too.
# # Sometimes you need to add hints or path suffixes, see the documentation for details: https://cmake.org/cmake/help/latest/command/find_library.html
# # 2. Link the library From 1. you have the full library name in FOO_LIB. You use this to link the library to your target GLBall as in
# target_link_libraries(GLBall PRIVATE "${FOO_LIB}")
# # You should add PRIVATE, PUBLIC, or INTERFACE after the target, cf. the documentation: https://cmake.org/cmake/help/latest/command/target_link_libraries.html
# # If you don't add one of these visibility specifiers, it will either behave like PRIVATE or PUBLIC, depending on the CMake version and the policies set.
# # 3. Add includes (This step might be not mandatory.)
# # If you also want to include header files, use find_path similar to find_library and search for a header file. Then add the include directory with target_include_directories similar to target_link_libraries.
# # Documentation: https://cmake.org/cmake/help/latest/command/find_path.html and https://cmake.org/cmake/help/latest/command/target_include_directories.html
# # If available for the external software, you can replace find_library and find_path by find_package.


# # Let's say you have an executable like:
# add_executable(GLBall GLBall.cpp)
# # If the external library has headers, give the path to its include folder:
# target_include_directories(GLBall PUBLIC "/path/to/include")
# # Add the library directory path:
# target_link_directories(GLBall PUBLIC "/path/to/lib/directory")
# # Finally, link the library name
# target_link_libraries(GLBall mylib)
# # Note that the prefix and extension of the library file are removed:
# # libmylib.a ➜ mylib
# # mylib.so ➜ mylib