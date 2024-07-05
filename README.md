# Nebulua

A simplified version of [Nebulator](https://github.com/cepthomas/Nebulator.git) using Lua as the script flavor.
While the primary intent is to generate music-by-code, runtime interaction is also supported using midi inputs.
There's also a simple comand line version.

It's called Nebulator after a MarkS C++ noisemaker called Nebula which manipulated synth parameters via code.

![logo](marks.png)

Since the built-in Windows GM player sounds terrible, there are a couple of options for playing midi locally:
- Replace it with [virtualmidisynth](https://coolsoft.altervista.org/en/virtualmidisynth) and your favorite soundfont.
- If you are using a DAW for sound generation, you can use a virtual midi loopback like
    [loopMIDI](http://www.tobias-erichsen.de/software/loopmidi.html) to send to it.


## Glossary

- Since this is code, everything is 0-based, not 1-based like music.
- Nebulua doesn't care about measures, that's up to you.

Name       | Type   | Description                                     |
-------    | ------ | ---------------------------                     |
chan_num   | int    | midi channel number                             |
dev_index  | int    | index into windows midi device table            |
dev_name   | char*  | from windows midi device table                  |
chan_hnd   | int    | internal opaque handle for channel id           |
controller | int    | from midi_defs.lua                              |
value      | int    | controller payload                              |
note_num   | int    | 0 -> 127                                        |
volume     | double | 0.0 -> 1.0, 0 means note off                    |
velocity   | int    | 0 -> 127, 0 means note off                      |
bar        | int    | 0 -> N, absolute                                |
beat       | int    | 0 -> 3, in bar, quarter note                    |
sub        | int    | 0 -> 7, in beat, "musical"                      |
tick       | int    | absolute time, see ##Timing, same length as sub |


## Time

- Midi DeltaTicksPerQuarterNote aka subs per beat is fixed at 8. This provides 32nd note resolution which
  should be more than adequate.
- The fast timer resolution is fixed at 1 msec giving a usable range of bpm of 40 (188 msec period)
  to 240 (31 msec period).
- Each sequence is typically 8 beats. Each section is typically 4 sequences -> 32 beats. A 4 minute song at
  80bpm is 320 beats -> 10 sections -> 40 sequences. If each sequence has an average of 8 notes for a total
  of 320 notes per instrument. A "typical" song with 6 instruments would then have about 4000 on/off events.
- To make the script translation between bar-beat-sub and ticks, see the [BarTime](#bartime) class below.

## Standard Note Syntax

- Scales and chords are specified by strings like `"1 4 6 b13"`.
- There are many builtin scales and chords defined [here](lua_code/music_defs.lua).
- Users can add their own by using `function create_definition("FOO", "1 4 6 b13")`.

Notes, chords, and scales can be specified in several ways:

Form              | Description                                              |
-------           | ---------------------------                              |
"F4"              | Named note with octave                                   |
"F4.m7"           | Named chord in the key of middle F                       |
"F4.Aeolian"      | Named scale in the key of middle F                       |
"F4.FOO"          | Custom chord or scale created with `create_definition()` |
inst.SideStick    | Drum name from the definitions                           |
57                | Simple midi note number                                  |


# Writing Scripts

Use your favorite external text editor, preferably with lua syntax highlighting and autocomplete.

Refer to this [composition](examples/example.lua) showing example of static sequence and loop definitions, 
and creating notes by script functions.

Scripts can also be [dynamic](examples/airport.lua), a take on Eno's Music for Airports. These have no canned sequences.

Almost all errors are considered fatal as they are usually things the user needs to fix before continuing such as
script syntax errors. They are logged, written to the CLI, and then the application exits.

## Composition
A composition is comprised of one or more sections, each of which has one or more sequences of notes.
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

```lua
BarTime(tick)
BarTime(bar, beat, sub)
BarTime(str)
```
Construction with several overloads:
- tick: construct from absolute tick
- bar, beat, sub: construct from explicit parts
- str: construct from string like "1:2:3" or "1:2" or "1"
- return: BarTime object

Properties:
- get_tick(): Get the tick.
- get_bar(): Get the bar number.
- get_beat(): Get the beat number in the bar.
- get_sub(): Get the sub in the beat.

Supports standard comparison operators and tostring().


## Imports

```lua
local neb = require("nebulua") -- lua api
local mid = require("midi_defs") -- GM midi instrument definitions
local mus = require("music_defs") -- chords, scales, etc
local bt  = require("bar_time") -- time utility
local ut  = require("utils") -- misc utilities
```

## Script Functions

Call these from your script.


```lua
function neb.create_output_channel(dev_name, chan_num, patch)
```
Register an output midi channel.
- dev_name: The system name.
- chan_num: Specific channel number.
- patch: Send this patch number.
- return: A channel handle to use in subsequent functions.


```lua
function neb.create_input_channel(dev_name, chan_num)
```
Register an input midi channel.
- dev_name: The system name.
- chan_num: Specific channel number.
- return: A channel handle to use in subsequent functions.


```lua
function neb.send_note(chan_hnd, note_num, volume, dur)
```
Send a note on/off immediately. Adds a note off if dur is specified and tick clock is running.
- chan_hnd: The channel handle to send it on.
- note_num: Which.
- volume: Note volume. 0.0 -> 1.0. 0.0 means note off.
- dur: How long it lasts in subbeats. Optional.


```lua
function neb.send_controller(chan_hnd, controller, value)
```
Send a controller immediately. Useful for things like panning and bank select.
- chan_hnd: The channel handle to send it on.
- controller: Which.
- value: What.


```lua
function neb.set_volume(chan_hnd, volume)
```
Set master volume for the channel.
- chan_hnd: The channel handle to set.
- volume: Master volume. 0.0 -> 1.0.


```lua
function neb.log_error(msg)
function neb.log_info(msg)
function neb.log_debug(msg)
function neb.log_trace(msg)
```
Log to the application log. Several flavors.
- msg: Text.


```lua
function neb.set_tempo(bpm)
```
Change the play tempo.
- bpm: New tempo.

```lua
function neb.process_comp(sections)
```
If it's a static composition call this in setup();
- sections: The composition collection.


```lua
function neb.process_step(tick)
```
If it's a static composition call this in step(tick);
- tick: current tick.


## Script Callbacks

These are called by the system for overriding in the user script.

```lua
function setup()
```
Called once to initialize your script stuff. Required.


```lua
function step(tick)
```
Called every subbeat/tick. Required.
- tick: current tick.


```lua
function rcv_note(chan_hnd, note_num, volume)
```
Called when input note arrives. Optional.
- chan_hnd: Input channel handle.
- note_num: Note number 0 -> 127.
- volume: Volume 0.0 -> 1.0.
- return: Nebulua status.


```lua
function rcv_control(chan_hnd, controller, value)
```
Called when input controller arrives.
- chan_hnd: Input channel handle.
- controller: Specific controller id 0 -> 127.
- value: Payload 0 -> 127.
- return: Nebulua status.


## Utilities

Some helpers are found in `music_defs.lua`.

```lua
function M.get_notes_from_string(nstr)
```
Parse note or notes from input value.
- nstr: see section [Standard Note Syntax](#standard-note-syntax).
- return: array of notes or empty if invalid.


```lua
function create_definition(name, intervals)
```
Define a group of notes for use as a chord or scale. Then it can be used by get_notes_from_string().
- name: reference name.
- intervals: string of note definitions. Like `"1 +3 4 -b7"`.


# Tech Notes

- Consists of two .NET applications (Command line and WinForms) and a C++/CLI interop component that interfaces to the
  lua library.
- The interop API translate between internal LUA_XXX status and user-facing enum NebStatus. API doesn't throw
  anything.
- Windows 64 bit only. Build it with VS2022 and .NET8.
- Uses 64 bit Lua 5.4.2 from [here](https://luabinaries.sourceforge.net/download.html).
- Uses [C code conventions](https://github.com/cepthomas/c_bag_of_tricks/blob/master/conventions.md).
- There are 3 threads:
    - main which does cli
    - midi in events callback
    - timer periodic events callback
- The shared resource that requires synchronization is a singleton `Api`. It is protected by a 
  `CRITICAL_SECTION`. Thread access to the UI is protected by `InvokeIfRequired()`.
- Almost all errors are considered fatal as they are usually things the user needs to fix before continuing
  such as script syntax errors. They are logged, written to the CLI, and then the application exits.
- Lua functions defined in C do not call `luaL_error()`. Only call `luaL_error()` in code that is called from
  the lua side. C side needs to handle function returns manually via status codes, error msgs, etc.

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
