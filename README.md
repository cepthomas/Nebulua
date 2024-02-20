# Nebulua TODO2 update all + parts of Nebulator\README.md

- An experimental version of Nebulator using Lua as the script flavor.
- Windows only.
- Windows 64 bit. Build with VS or mingw64.  I'm using 64 bit Lua 5.4.2 from https://luabinaries.sourceforge.net/download.html.

- Test code is Windows 64 bit build using CMake. Your PATH must include `...\mingw64\bin`. <<<<<?????

- Uses [C code conventions](https://github.com/cepthomas/c_bag_of_tricks/blob/master/conventions.md).


## timing
Midi DeltaTicksPerQuarterNote aka sub parts per beat/qtr_note = 8 = 32nd note resolution
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


## Glossary
int chan_num
int dev_index - internal index into device table
char* dev_name - from system
not: int dev_id - 
    int chan_hnd (dev_index << 8 | chan_num)
    int controller (name - midi_defs)
    int value (controller payload)
    int note_num (0-127)
    double volume (0.0-1.0) 0 means note off
    int velocity (0-127) 0 means note off
    int bar (absolute)
    int beat (in bar)
    int sub (in beat) - "musical"
    int tick (absolute order - maybe in sequence/section/composition) - same size as sub

Script defs:
   BAR is 0->N, BEAT is 0->neb.BEATS_PER_BAR-1, SUB is 0->neb.SUBS_PER_BEAT-1
   WHAT_TO_PLAY is a string (see neb.get_notes_from_string(s)) or integer or function.
   BAR_TIME is a string of "BAR.BEAT.SUB" e.g. "1.2.3" or "1.2" or "1".
   VOLUME 0->9



## sequences
-- Graphical format:
-- "|7-------|" is one beat with 8 subs
-- note velocity is 1-9 (map) or - which means sustained
-- note/chord, velocity/volume
-- List format:
-- times are beat.sub where beat is 0-N sub is 0-7
-- note/chord, velocity/volume is 0.0 to 1.0, duration is 0.1 to N.7

## API



## Files
- `source_code` folder:
    - exec.c/h - Does all the top-level work.
    - etc...
- `lua_lib` folder:
- `lua_code` folder:
    - nebulua.lua - Lua script for a simplistic multithreaded coroutine application. Uses luatoc.
    - utils.lua - Used by demoapp.lua.
- `test_code` folder:
    - lua source code for this application...
- `examples` folder:
    - lua...
- `mingw` folder:
- `app` and `app\test` folder:

