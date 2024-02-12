# Nebulua TODO1 update all + parts of Nebulator\README.md

- An experimental version of Nebulator using Lua as the script flavor.
- Windows only.
- Windows 64 bit. Build with VS or mingw64.
- Test code is Windows 64 bit build using CMake. Your PATH must include `...\mingw64\bin`.

- Uses [C code conventions](https://github.com/cepthomas/c_bag_of_tricks/blob/master/conventions.md).


## timing
Midi DeltaTicksPerQuarterNote aka subbeats per beat/qtr_note = 8 = 32nd note resolution
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
    int subbeat (in beat) - "musical"
    int tick (absolute order - maybe in sequence/section/composition) - delta is subbeat

Script defs:
   BAR is 0->N, BEAT is 0->neb.BEATS_PER_BAR-1, SUBBEAT is 0->neb.SUBBEATS_PER_BEAT-1
   WHAT_TO_PLAY is a string (see neb.get_notes_from_string(s)) or integer or function.
   BAR_TIME is a string of "BAR.BEAT.SUBBEAT" e.g. "1.2.3" or "1.2" or "1".
   VOLUME 0->9



## sequences
-- Graphical format:
-- "|7-------|" is one beat with 8 subbeats
-- note velocity is 1-9 (map) or - which means sustained
-- note/chord, velocity/volume
-- List format:
-- times are beat.subbeat where beat is 0-N subbeat is 0-7
-- note/chord, velocity/volume is 0.0 to 1.0, duration is 0.1 to N.7

## API
-- Script wants to log something.
-- - level Log level
-- - msg Log message
-- return LUA_STATUS
M.log(level, msg)

-- Create an input midi channel.
-- - dev_name Midi device name
-- - chan_num Midi channel number 1-16
-- return Channel handle or 0 if invalid
M.create_input_channel(dev_name, chan_num)

-- Create an output midi channel.
-- - dev_name Midi device name
-- - chan_num Midi channel number 1-16
-- - patch Midi patch number
-- return Channel handle or 0 if invalid
M.create_output_channel(dev_name, chan_num, patch)

-- Script wants to change tempo.
-- - bpm BPM
-- return LUA_STATUS
M.set_tempo(bpm)

-- If volume is 0 note_off else note_on. If dur is 0 send note_on with dur = 0.1 (for drum/hit).
-- - chan_hnd Output channel handle
-- - note_num Note number
-- - volume Volume between 0.0 and 1.0
-- - dur Duration in ??? see spec
-- return LUA_STATUS
M.send_note(chan_hnd, note_num, volume, dur)

-- Send a controller immediately.
-- - chan_hnd Output channel handle
-- - controller Specific controller
-- - value Payload.
-- return LUA_STATUS
M.send_controller(chan_hnd, controller, value)


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

