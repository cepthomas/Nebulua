
# Nebulua TODO1 update all

TODO2 Copy some doc from Nebulator\README.md

- An experimental version of Nebulator using Lua as the script flavor.
- Windows UTF only.
- Build with VS or mingw.

- For conventions see [c_modular](https://github.com/cepthomas/c_modular/blob/master/README.md).


## timing
DeltaTicksPerQuarterNote aka subbeats per beat = 8
Gives 32nd note resolution.
Fast timer resolution set to 1 msec.
int bpm = 40 -> 188 msec period.
int bpm = 240 -> 31 msec period.


## field types:
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


## sequences
-- Graphical format:
-- "|7-------|" is one beat with 8 subbeats
-- note velocity is 1-9 (map) or - which means sustained
-- note/chord, velocity/volume
-- List format:
-- times are beat.subbeat where beat is 0-N subbeat is 0-7
-- note/chord, velocity/volume is 0.0 to 1.0, duration is 0.1 to N.7


## Files
- `source` folder:
    - exec.c/h - Does all the top-level work.
    - etc...
- `lua` folder:
    - nebulua.lua - Lua script for a simplistic multithreaded coroutine application. Uses luatoc.
    - utils.lua - Used by demoapp.lua.
- `test_code` folder:
    - lua source code for this application...
- `examples` folder:
    - lua...

