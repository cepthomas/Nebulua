# Nebulua TODO1 update all
- A comand line version of https://github.com/cepthomas/Nebulator.git using Lua as the script flavor.
- Since the built-in Windows GM player sounds terrible, there are a couple of options for playing midi locally:
  - Replace it with [virtualmidisynth](https://coolsoft.altervista.org/en/virtualmidisynth) and your favorite soundfont.
  - If you are using a DAW for sound generation, you can use a virtual midi loopback like [loopMIDI](http://www.tobias-erichsen.de/software/loopmidi.html) to send to it.
- While the primary intent is to generate music-by-code, runtime interaction is also supported using midi or OSC inputs.
- It's called Nebulator after a MarkS C++ noisemaker called Nebula which allowed manipulation of synth parameters using code.
- Requires VS2022 and .NET6.

![logo](marks.jpg)

## Glossary
int chan_num - 
int dev_index - internal index into device table
char* dev_name - from Windows system
not: int dev_id - 
    int chan_hnd (16bit: dev_index << 8 | chan_num)
    int controller (name - midi_defs)
    int value (controller payload)
    int note_num (0-127)
    double volume (0.0-1.0) 0 means note off
    int velocity (0-127) 0 means note off
    int bar (absolute)
    int beat (in bar)
    int sub (in beat) - "musical"
    int subs ??
    int tick (absolute time - maybe in sequence/section/composition) - same size as sub

Script defs:
   BAR is 0->N, BEAT is 0->neb.BEATS_PER_BAR-1, SUB is 0->neb.SUBS_PER_BEAT-1
   WHAT_TO_PLAY is a string (see neb.get_notes_from_string(s)) or integer or function.
   BAR_TIME is a string of "BAR.BEAT.SUB" e.g. "1.2.3" or "1.2" or "1".
   VOLUME 0->9

## Timing
Midi DeltaTicksPerQuarterNote aka sub parts per beat/qtr_note = 8 = 32nd note resolution
Gives 32nd note resolution. aka tick
Fast timer resolution set to 1 msec.
int bpm = 40 -> 188 msec period.
int bpm = 240 -> 31 msec period.

Uses the `BarTime` class. A shorthand is provided to specify a time using `mytime = BarTime(1:2:3)` => bar:beat:sub.
Also R/W as ticks.

Nebulua doesn't care about measures, that's up to you.

Each section is 8 beats.
Each sequence is 4 sections => 32 beats.
A 4 minute song at 80bpm is 320 beats => 10 sequences => 40 sequences.

If each sequence has average 8 notes => total of 320 notes per instrument.
A 4 minute song at 80bpm is 320 beats => 10 sequences => 40 sequences => 320 notes.
A "typical" song would have about 4000 on/off events.

## Error handling
- lua-C host does not call luaL_error(). Only call luaL_error() in code that is called from the lua side. C side needs to handle function returns manually via status codes, error msgs, etc.
- Discuss the status return codes. C-Lua side uses the standard LUA_XXX codes. The API layer translates them to application-specific enum NebStatus.
- Fatal errors are things the user needs to fix before continuing e.g. script syntax errors, ... They are logged, written to the CLI, and then exits.

# Writing Scripts

Refer to this example script:
- example.neb: Source file showing example of static sequence and loop definitions, and creating notes by script functions.

Scripts can also be dynamic:
- airport.neb: A take on Eno's Music for Airports - adapted from [this](https://github.com/teropa/musicforairports.js).

Use your favorite external text editor. The application will watch for changes you make and indicate that recompile
is needed. I use Sublime - you can associate .neb files with C# for pretty-close syntax coloring.

## Define Midi IO

```lua
-- Devices
local midi_in = "loopMIDI Port"
local midi_out = "Microsoft GS Wavetable Synth"

-- Channels
local hnd_keys  = neb.create_output_channel(midi_out, 1, inst.AcousticGrandPiano)
local hnd_bass  = neb.create_output_channel(midi_out, 2, inst.AcousticBass)
local hnd_synth = neb.create_output_channel(midi_out, 3, inst.Lead1Square)
local hnd_drums = neb.create_output_channel(midi_out, 10, kit.Jazz)
local hnd_inp1  = neb.create_input_channel(midi_in, 2)
```

## Musical Notes/Chords/Scales

- Scales and chords are specified by strings like `"1 4 6 b13"`.
- There are many builtin defined in music_defs.lua.
- Users can add their own by using `create_definition("FOO", "1 4 6 b13")`.

Notes (single) and note groups (chords, scales) are referenced in several ways:
- "F4" - Named note with octave.
- "F4.m7" - Named chord in the key of middle F.
- "F4.Aeolian" - Named scale in the key of middle F.
- "F4.FOO" - Custom chord or scale created with `CreateNotes()`.
- SideStick - Drum name from the definitions.
- 57 - Simple midi note number.

```lua
-- Get some stock chords and scales.
local alg_scale = md.get_notes("G3.Algerian")
local chord_notes = md.get_notes("C4.o7")

-- Create custom scale.
md.create_definition("MY_SCALE", "1 +3 4 -b7")
local my_scale_notes = md.get_notes("B4.MY_SCALE")
```


## Composition
A composition is comprised of one or more Sections each of which has one or more Sequences of notes.
You first create your Sequences like this:

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

Graphical format:
"|7-------|" is one beat with 8 subs
note velocity is 1-9 (map) or - which means sustained
note/chord, velocity/volume

- Notes are `{ pattern, note or chord or drum, volume, duration }`.
- Pattern: describes a sequence of notes, kind of like a piano roll. `1 to 9` (volume) starts a note which is held 
  for subsequent `-`. The note is ended with any other character than `-`. `|`, `.` and ` ` are ignored, 
  used for visual assist only. These are particularly useful for drum patterns.


```
function seq_func()
{
    int notenum = Random(0, scaleNotes.Count());
    SendNote("synth", scaleNotes[notenum], 0.7, 0.5);
}
```

Then you group Sequences into Sections, typically things like verse, chorus, bridge, etc.
```lua
sections =
{
    beginning =
    {
        { hnd_keys,  keys_verse,  keys_verse,  keys_verse,  keys_verse },
        { hnd_drums, drums_verse, drums_verse, drums_verse, drums_verse },
        { hnd_bass,  bass_verse,  bass_verse,  bass_verse,  bass_verse }
    },
    middle = ...,
    ending = ...
}
```

## Script Functions

Call these from inside your script.
Enable by:
```lua
local neb = require("nebulua")
local md  = require("midi_defs")
local bt  = require("bar_time")
local ut  = require("utils")
```

```lua
function create_input_channel(dev_name, chan_num)
```
Registers an input midi channel.
- dev_name: The system name.
- chan_num: Specific channel number.
- returns: A channel handle to use in subsequent functions.

```lua
function create_output_channel(dev_name, chan_num, patch)
```
Registers an output midi channel.
- dev_name: The system name.
- chan_num: Specific channel number.
- patch: Send this patch number.
- returns: A channel handle to use in subsequent functions.

```lua
function send_note(chan_hnd, note_num, volume, dur)
```
Send a note on/off immediately. Adds a note off if dur is specified.
- chan_hnd: The channel handle to send it on.
- note_num: Which.
- volume: Note volume. 0.0 => 1.0. 0.0 means note off.
- dur: How long it lasts in subbeats.

```lua
function send_controller(chan_hnd, controller, value)
```
Send a controller immediately. Useful for things like panning and bank select.
- chan_hnd: The channel handle to send it on.
- controller: Which.
- value: What.

```lua
function log_error(msg)
function log_info(msg)
function log_debug(msg)
function log_trace(msg)
```
Log to the application log.
- msg: Text.

```lua
function set_tempo(bpm)
```
Change the composition tempo.
- bpm: New tempo.


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
- note_num: Note number 0 => 127.
- volume: Volume 0.0 => 1.0.
- returns: Nebulua status.

```lua
function rcv_control(chan_hnd, controller, value)
```
Called when input controller arrives.
- chan_hnd: Input channel handle.
- controller: Specific controller id 0 => 127.
- value: Payload 0 => 127.
- returns: Nebulua status.


## Utilities

```lua
function create_definition(name, intervals)
```
Define a group of notes for use as a note, or in a chord or scale.
Like "MY_SCALE", "1 +3 4 -b7"
- name: Reference name.
- intervals: String of note definitions.


```lua
function M.get_notes_from_string(nstr)
```
Parse note or notes from input value.
- nstr:  Could look like:
    - F4 - named note
    - Bb2.dim7 - named key.chord
    - E#5.major - named key.scale
    - A3.MY_SCALE - user defined key.chord-or-scale
- returns: Array of notes or empty if invalid.


# Design/build

- Describe arch, files, how to build this.
- Api - Translate between internal LUA_XXX status and client facing NEB_XXX status.
- Uses 64 bit Lua 5.4.2 from https://luabinaries.sourceforge.net/download.html.
- Windows 64 bit. Build with VS2022 and .NET8.
- Uses [C code conventions](https://github.com/cepthomas/c_bag_of_tricks/blob/master/conventions.md).
- To build/test pull https://github.com/cepthomas/LuaBagOfTricks.git and add an env var 'LBOT' that
  points to it.

## Files

```
Nebulua - C# project main app
|   App.cs
|   Cli.cs
|   Common.cs
|   Midi.cs
|   Program.cs
|   State.cs
|   gen_interop.cmd, interop_spec.lua - generates interop wrapper code
+---interop - C++/CLI project to embed lua
|       Api.cpp/h
|       luainterop.c/h - generated wrapper code
|       luainteropwork.cpp - glue
+---examples
|       airport.lua
|       example.lua
+---lib - C# utilities and lua library
+---lua_code - lua scripts for application
\---test - various test code projects
```

# External Components

This application uses these FOSS components:
- [NAudio](https://github.com/naudio/NAudio) (Microsoft Public License).
- Application icon: [Charlotte Schmidt](http://pattedemouche.free.fr/) (Copyright © 2009 of Charlotte Schmidt).
