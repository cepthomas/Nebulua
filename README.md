# Nebulua
- A comand line version of https://github.com/cepthomas/Nebulator.git using Lua as the script flavor.
- Since the built-in Windows GM player sounds terrible, there are a couple of options for playing midi locally:
  - Replace it with [virtualmidisynth](https://coolsoft.altervista.org/en/virtualmidisynth) and your favorite soundfont.
  - If you are using a DAW for sound generation, you can use a virtual midi loopback like [loopMIDI](http://www.tobias-erichsen.de/software/loopmidi.html) to send to it.
- While the primary intent is to generate music-by-code, runtime interaction is also supported using midi or OSC inputs.
- It's called Nebulator after a MarkS C++ noisemaker called Nebula which allowed manipulation of synth parameters using code.

![logo](marks.jpg)

## Glossary

Name       | Type   | Description                                     |
-------    | ------ | ---------------------------                     |
chan_num   | int    | Midi channel number                             |
dev_index  | int    | Index into windows midi device table            |
dev_name   | char*  | from windows midi device table                  |
chan_hnd   | int    | internal opaque handle for channel id           |
controller | int    | from midi_defs.lua                              |
value      | int    | controller payload                              |
note_num   | int    | 0 => 127                                        |
volume     | double | 0.0 => 1.0, 0 means note off                    |
velocity   | int    | 0 => 127, 0 means note off                      |
bar        | int    | 0 => N, absolute                                |
beat       | int    | 0 => BEATS_PER_BAR-1, in bar                    |
sub        | int    | 0 => SUBS_PER_BEAT-1, in beat, "musical"        |
tick       | int    | absolute time, see ##Timing, same length as sub |


Nebulua doesn't care about measures, that's up to you.

## Timing

- Midi DeltaTicksPerQuarterNote aka sub parts per beat/qtr_note = 8 = 32nd note resolution.
- Fast timer resolution set to 1 msec giving: bpm = 40 -> 188 msec period, and bpm = 240 -> 31 msec period..
- Each section is 8 beats. Each sequence is 4 sections => 32 beats. Therefore a 4 minute song at 80bpm is 320 beats => 10 sequences => 40 sequences.
- If each sequence has average 8 notes => total of 320 notes per instrument. A 4 minute song at 80bpm is 320 beats => 10 sequences => 40 sequences => 320 notes. A "typical" song would have about 4000 on/off events.
- See also the `BarTime` class below.

## Standard Note Syntax

- Scales and chords are specified by strings like `"1 4 6 b13"`.
- There are many builtin defined in music_defs.lua.
- Users can add their own by using `create_definition("FOO", "1 4 6 b13")`.

Notes (single) and note groups (chords, scales) are specified in several ways:

Form         | Description                                              |
-------      | ---------------------------                              |
"F4"         | Named note with octave                                   |
"F4.m7"      | Named chord in the key of middle F                       |
"F4.Aeolian" | Named scale in the key of middle F                       |
"F4.FOO"     | Custom chord or scale created with `create_definition()` |
SideStick    | Drum name from the definitions                           |
57           | Simple midi note number                                  |

## Error handling

Almost all errors are considered fatal - things the user needs to fix before continuing e.g. script syntax errors.
They are logged, written to the CLI, and then the application exits.

Lua functions defined in C do not call `luaL_error()``. Only call `luaL_error()`` in code that is called from the lua side. C side needs to handle function returns manually via status codes, error msgs, etc.

Lua error codes are handled in the interop layer and translated to more meaningful Nebulua-specific codes (enum NebStatus).

# Writing Scripts

Use your favorite external text editor, preferably with lua syntax highlighting and autocomplete.

Refer to this [example script](examples/example.lua) showing example of static sequence and loop definitions, and creating notes by script functions.

Scripts can also be [dynamic](examples/airport.neb), a take on Eno's Music for Airports.


## Composition
A composition is comprised of one or more `section`s, each of which has one or more `sequence`s of notes.
You first create your sequences like this:

```lua
local example_seq =
{
    -- | beat 1 | beat 2 |........|........|........|........|........|........|,  WHAT_TO_PLAY
    { "|M-------|--      |        |        |7-------|--      |        |        |", "G4.m7" },
    { "|7-------|--      |        |        |7-------|--      |        |        |",  84 },
    { "|        |        |        |5---    |        |        |        |5-8---  |", "D6" },
    { "|        |        |        |5---    |        |        |        |5-8---  |",  seq_func }
},
```

- `|7-------|` is one beat with 8 subs.
- `1 to 9` (volume) starts a note which is held for subsequent `-`. The note is ended with any other character than `-`.
- `|`, `.` and ` ` are ignored, used for visual assist only. These are particularly useful for drum patterns.
- `WHAT_TO_PLAY` is a standard string, integer, or function.
- Pattern: describes a sequence of notes, kind of like a piano roll. 

Then you group sequences into sections, typically things like verse, chorus, bridge, etc.
```lua
sections =
{
    beginning =
    {
        { hnd_keys,  keys_verse,  keys_verse,  keys_verse,  keys_verse },
        { hnd_drums, drums_verse, drums_verse, drums_verse, drums_verse },
        { hnd_bass,  bass_verse,  bass_verse,  bass_verse,  bass_verse }
    },
    middle = { ... },
    ending = { ... }
}
```

## BarTime

To make it easier to manipulate and translate time, a lua class is provided.

Construction with several overloads.
```lua
BarTime(tick)
BarTime(bar, beat, sub)
BarTime(str)
```
- tick: construct from tick
- bar, beat, sub: construct from explicit parts
- str: construct from string likw "1:2:3" or "1:2" or "1"
- return: BarTime object

Properties.
- get_tick(): Get the tick.
- get_bar(): Get the bar number.
- get_beat(): Get the beat number in the bar.
- get_sub(): Get the sub in the beat.

A BarTime object supports standard comparison operators and tostring().


## Script Functions

Call these from your script. They are defined in `nebulua.lua`.

Imports.
```lua
local neb = require("nebulua") -- lua api
local md  = require("midi_defs") -- GM midi instrument definitions
local bt  = require("bar_time") -- time utility
local ut  = require("utils") -- misc utilities
```

Register an output midi channel.
```lua
function create_output_channel(dev_name, chan_num, patch)
```
- dev_name: The system name.
- chan_num: Specific channel number.
- patch: Send this patch number.
- return: A channel handle to use in subsequent functions.


Registers an input midi channel.
```lua
function create_input_channel(dev_name, chan_num)
```
- dev_name: The system name.
- chan_num: Specific channel number.
- return: A channel handle to use in subsequent functions.


Send a note on/off immediately. Adds a note off if dur is specified.
```lua
function send_note(chan_hnd, note_num, volume, dur)
```
- chan_hnd: The channel handle to send it on.
- note_num: Which.
- volume: Note volume. 0.0 => 1.0. 0.0 means note off.
- dur: How long it lasts in subbeats.


Send a controller immediately. Useful for things like panning and bank select.
```lua
function send_controller(chan_hnd, controller, value)
```
- chan_hnd: The channel handle to send it on.
- controller: Which.
- value: What.


Log to the application log.
```lua
function log_error(msg)
function log_info(msg)
function log_debug(msg)
function log_trace(msg)
```
- msg: Text.


Change the play tempo.
```lua
function set_tempo(bpm)
```
- bpm: New tempo.

If a static composition call this in setup();
```lua
function init(sections)
```
- sections: The composition collection.


If a static composition call this in step(tick);
```lua
function process_step(tick)
```
- tick: current tick.


## Script Callbacks

These are called by the system for overriding in the user script.

Called once to initialize your script stuff. Required.
```lua
function setup()
```


Called every subbeat/tick. Required.
```lua
function step(tick)
```
- tick: current tick.


Called when input note arrives. Optional.
```lua
function rcv_note(chan_hnd, note_num, volume)
```
- chan_hnd: Input channel handle.
- note_num: Note number 0 => 127.
- volume: Volume 0.0 => 1.0.
- return: Nebulua status.


Called when input controller arrives.
```lua
function rcv_control(chan_hnd, controller, value)
```
- chan_hnd: Input channel handle.
- controller: Specific controller id 0 => 127.
- value: Payload 0 => 127.
- return: Nebulua status.


## Utilities

Some helpers are found in music_defs.lua.

Parse note or notes from input value.
```lua
function M.get_notes_from_string(nstr)
```
- nstr: see section [Standard Note Syntax](Standard Note Syntax).
- return: Array of notes or empty if invalid.


Define a group of notes for use as a chord or scale. Then it can be used by get_notes_from_string().
```lua
function create_definition(name, intervals)
```
- name: Reference name.
- intervals: String of note definitions. Like `"1 +3 4 -b7"`.


# Design/build/tech-notes TODO2

- Describe arch, files, how to build this.
- Api - Translate between internal LUA_XXX status and client facing NEB_XXX status.
- Windows 64 bit. Build with VS2022 and .NET8.
- Uses 64 bit Lua 5.4.2 from [here](https://luabinaries.sourceforge.net/download.html).
- Uses [C code conventions](https://github.com/cepthomas/c_bag_of_tricks/blob/master/conventions.md).
- To build/test pull https://github.com/cepthomas/LuaBagOfTricks.git and add an env var 'LBOT' that
  points to it.

## Files

```
Nebulua - C# project main app
|   *.cs
|   gen_interop.cmd, interop_spec.lua - generates interop wrapper code
+---interop - C++/CLI project to embed lua
|       Api.cpp/h
|       luainterop.c/h - generated wrapper code
|       luainteropwork.cpp - glue
+---lua_code - lua modules for application
+---lib - C# utilities and lua include
+---examples
|       airport.lua
|       example.lua
\---test - various test code projects
```

# External Components

This application uses these FOSS components:
- [NAudio](https://github.com/naudio/NAudio) (Microsoft Public License).
- Application icon: [Charlotte Schmidt](http://pattedemouche.free.fr/) (Copyright © 2009 of Charlotte Schmidt).
