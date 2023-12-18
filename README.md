# Nebulua
An experimental version of Nebulator using Lua as the script flavor.


field types:
S = string
N = number
I = integer
F = function
M = map index(0-9)
T = TableEx
V = void
B = boolean
X = bar time(Number?)
E = expression?


---- sequences
-- Graphical format:
-- "|7-------|" is one beat with 8 subbeats
-- note velocity is 1-9 (map) or - which means sustained
-- note/chord, velocity/volume
-- List format:
-- times are beat.subbeat where beat is 0-N subbeat is 0-7
-- note/chord, velocity/volume is 0.0 to 1.0, duration is 0.1 to N.7


Reload:
- https://stackoverflow.com/questions/2812071/what-is-a-way-to-reload-lua-scripts-during-run-time
- https://stackoverflow.com/questions/9369318/hot-swap-code-in-lua




# c_emb_lua xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
A minimal example of a C embedded executable with lua script processor.
It demonstrates:
- Coroutines - one state for the C side and one for the script side.
- Calling script functions with args from the C side.
- Calling C functions with args from the script side.
- Simulated embedded system with a hardware level, CLI for control, and exec loop running everything.
- Rudimentary error handling model - more required.

# Build
- Pure C99 which should compile anywhere, including small embedded systems - basically anywhere you can compile lua.
- A VS Code workspace using mingw and CMake is supplied. Your PATH needs to include mingw.
- Run build.cmd to make the executables.

# Files
- [Conventions](https://github.com/cepthomas/c_bag_of_tricks/blob/master/CONVENTIONS.md).
- [Modular model](https://github.com/cepthomas/c_modular).
- `c` folder:
    - main.c - Entry stub calls exec.
    - exec.c/h - Does all the top-level work.
    - board.c/h - Interface to the (simulated) hardware.
    - common.c/h - Misc time, strings, ...
    - luainterop.c/h - Interfaces to call lua functions from C and to call C functions from lua.
    - luautils.c/h - General purpose tools for probing lua stacks.
- `lua` folder:
    - demoapp.lua - Lua script for a simplistic multithreaded coroutine application. Uses luatoc.
    - utils.lua - Used by demoapp.lua.
- `lua-5.3.5` folder:
    - lua source code for this application. Stock except where marked by `C_EMB_LUA`.

# Licenses
[This repo](https://github.com/cepthomas/c-emb-lua/blob/master/LICENSE)

[Lua](https://github.com/cepthomas/c-emb-lua/blob/master/LUA-LICENSE)
