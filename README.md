
# Nebulua

A simplified version of [Nebulator](https://github.com/cepthomas/Nebulator.git) using Lua as the script flavor.
While the primary intent is to generate music-by-code, runtime interaction is also supported using midi inputs.
Windows only.

It's called Nebulator after a MarkS C++ noisemaker called Nebula which manipulated synth parameters via code.

![logo](marks.png)


This application uses these FOSS components:
- [NAudio](https://github.com/naudio/NAudio).

## Usage

Currently this is a build-and-run-it-yourself configuration. Eventually an installer may be provided.
Note that running in VS debugger has a very slow startup. Running from the exe or cli is ok.

Building this solution requires a folder named `LBOT` at the top level containing the contents of
  [LuaBagOfTricks](https://github.com/cepthomas/LuaBagOfTricks). This can be done one of several ways:
  - git submodule
  - copy of pertinent parts
  - symlink: `mklink /d <current_folder>\LBOT <lbot_source_folder>\LuaBagOfTricks`

If you pass it a script file name on the command line it runs as a command line application. If not the UI is started.

The UI does have a terminal which can be used for debugging scripts using
[Lua debugger](https://github.com/cepthomas/LuaBagOfTricks/blob/main/debugex.lua).
See [example](examples/example.lua) for how-to.

Since the built-in Windows GM player sounds terrible, there are a couple of options for playing midi locally:
- Replace it with [virtualmidisynth](https://coolsoft.altervista.org/en/virtualmidisynth) and your favorite soundfont.
  Note that this app has a significant delay handling realtime midi inputs. This will not be a problem if you are just playing a midi file.
- If you are using a DAW for sound generation, you can use a virtual midi loopback like
    [loopMIDI](http://www.tobias-erichsen.de/software/loopmidi.html) to connect to it.


## Example Script Files

See the `examples` directory for material while perusing the docs.

File        | Description
----------- | -----------
example.lua | Source file showing example of static sequence and loop definitions, and creating notes by script functions.
airport.lua | A take on Eno's Music for Airports - adapted from [this](https://github.com/teropa/musicforairports.js).


# Definitions

## Glossary

- Since this is code, everything is 0-based, not 1-based like standard music notation.
- Nebulua doesn't care about measures, that's up to you.

Name       | Type   | Description                                     |
-------    | ------ | ---------------------------                     |
chan_num   | int    | midi channel number 1 -> 16                     |
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

## Standard Note Syntax

- Scales and chords are specified by strings like `"1 4 6 b13"`.
- There are many builtin scales and chords which you can see by clicking '?'.
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

Internally, time is in units of `tick`. This is unwieldy for scripts so helpers are provided.
Note that `tick` is a plain integer so normal algebraic operations (`+` `-` etc) can be performed on them.

All volumes in the script are numbers in the range of 0.0 -> 1.0. These are mapped to standard midi values
downstream. If the script uses out of range values, they are constrained and a warning is issued.


## Imports

Scripts need this section.

```lua
local api = require("script_api") -- lua api
local mid = require("midi_defs") -- GM midi instrument definitions
local mus = require("music_defs") -- chords, scales, etc
local bt  = require("bar_time") -- time utility
local ut  = require("lbot_utils") -- misc utilities
```

## Time

```lua
function bt.bt_to_tick(bar, beat, sub)
```
Create from explicit bar_time parts.

- bar: Bar number 0 - 1000
- beat: Beat number 0 - 3
- sub: Subbeat number 0 - 7
- return: Corresponding tick

```lua
function bt.beats_to_tick(beat, sub)
```
Create from explicit bar_time parts.

- beat: Beat number 0 - 1000
- sub: Subbeat number 0 - 7
- return: Corresponding tick

```lua
function bt.str_to_tick(str)
```
Parse from string like "1.2.3" or "1.2" or "1".

- str: The string
- return: Corresponding tick

```lua
function bt.tick_to_str(tick)
```
Format the value like "1.2.3".

- tick: To format
- return: The string

```lua
function bt.tick_to_bt(tick)
```
Translate tick into bar_time parts.

- tick: To translate
- return: bar, beat, sub

## Script Functions

Call these from your script.


```lua
function api.open_midi_output(dev_name, chan_num, chan_name, _patch)
```
Register an output midi channel.

- dev_name: The system name.
- chan_num: Specific channel number.
- chan_name: Name for channel.
- patch: Send this patch number or `mid.NO_PATCH`. Use NO_PATCH if your host manages its own patches.
- return: A channel handle to use in subsequent functions.


```lua
function api.open_midi_input(dev_name, chan_num, chan_name)
```
Register an input midi channel.

- dev_name: The system name.
- chan_num: Specific channel number.
- chan_name: Name for channel.
- return: A channel handle to use in subsequent functions.


```lua
function api.send_midi_note(chan_hnd, note_num, volume, dur)
```
Send a note on/off immediately. Adds a note off if dur is specified and tick clock is running.

- chan_hnd: The channel handle to send it on.
- note_num: Which.
- volume: Note volume. 0.0 -> 1.0. 0.0 means note off.
- dur: How long it lasts in subbeats. Optional.


```lua
function api.send_midi_controller(chan_hnd, controller, value)
```
Send a controller immediately. Useful for things like panning and bank select.

- chan_hnd: The channel handle to send it on.
- controller: Which.
- value: What.


```lua
function api.set_volume(chan_hnd, volume)
```
Set volume for the channel.

- chan_hnd: The channel handle to set.
- volume: Channel volume. 0.0 -> 1.0.


```lua
function api.log_error(msg)
function api.log_info(msg)
function api.log_debug(msg)
function api.log_trace(msg)
```
Log to the application log. Several flavors.

- msg: Text.


```lua
function api.set_tempo(bpm)
```
Change the play tempo.

- bpm: New tempo.


```lua
function api.process_comp()
```
If it's a static composition call this in setup().

- return: Meta info about the composition for internal use.


```lua
function api.process_step(tick)
```
Call this in step(tick) to process things like note offs.

- tick: current tick.

```lua
function api.parse_sequence_steps(chan_hnd, sequence)
```
Create a dynamic object from a sequence. See [Composition](#markdown-header-composition).

- chan_hnd: Specific channel.
- sequence: The sequence to parse.
- return: An object for use by `send_sequence_steps()`.


```lua
function api.send_sequence_steps(seq_steps, tick)
```
Send the object created in `parse_sequence_steps()`. See [Composition](#markdown-header-composition).

- seq_steps: when to send it, usually current tick.
- tick: when to send it, usually current tick.

## Script Callbacks

These are called by the system for overriding in the script.

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
function receive_midi_note(chan_hnd, note_num, volume)
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
- `1 to 9` (volume level) starts a note which is held for subsequent `-`. The note is ended with any other character than `-`.
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

Sequences can also be loaded dynamically and triggered at arbitrary times in the script.

```lua
local example_seq_steps = api.parse_sequence_steps(hnd_keys, example_seq)

function step(tick)
    local bar, beat, sub = bt.tick_to_bt(tick)

    if bar == 1 and beat == 0 and sub == 0 then
        api.send_sequence_steps(example_seq_steps, tick)
    end

    -- Do this now.
    api.process_step(tick)
end
```

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

## Build

- Windows 64 bit only. Build it with VS2022 and .NET8.
- Uses 64 bit Lua 5.4.2 from [here](https://luabinaries.sourceforge.net/download.html).
- Uses [C code conventions](https://github.com/cepthomas/c_bag_of_tricks/blob/master/conventions.md).


## Design

- All lua code is in modules except for the interop functions such as `step()` which are in global `_G`.
- There are 3 threads:
    - Main which does window event loop and the cli.
    - Midi receive events callback.
    - Timer periodic events callback.
- The shared resource that requires synchronization is a singleton `AppInterop`. It is protected by a 
  `CRITICAL_SECTION`. Thread access to the UI is protected by `InvokeIfRequired()`.
- The app interop translates between internal `LUA_XXX` status and user-facing `enum NebStatus`.
  The interop never throws.

## Call Stack

Going up and down the stacks is a bit convoluted. Here are some examples that help (hopefully).

Host -> lua
```
MmTimer_Callback(double totalElapsed, double periodElapsed)  [in App\Core.cs]
    calls
AppInterop.Step(tick)  [in interop\AppInterop.cpp]
    calls
luainterop_Step(_l, tick)  [in interop\luainterop.c]
    calls
function step(tick)  [in my_lua_script.lua]
```

Lua -> host
```
neb.send_midi_note(hnd_synth, note_num, volume)  [in my_lua_script.lua]
    calls
luainterop_SendMidiNote(lua_State* l, int chan_hnd, int note_num, double volume)  [in interop\luainterop.c]
    calls
 AppInterop::NotifySend(args)  [in interop\AppInterop.cpp]
    calls
Interop_Send(object? _, SendArgs e)  [in App\Core.cs]
    calls driver...
```

## Error Model

- Almost all errors are considered fatal as they are usually things the user needs to fix before continuing,
  such as script syntax errors. They are logged and then the application exits.
- Lua functions defined in C do not call `luaL_error()`. Only call `luaL_error()` in code that is called from
  the lua side. C side needs to handle function returns manually via status codes, error msgs, etc.
- The C# application side uses custom exceptions to harmonize the heterogenous nature of the C#/C++/C/Lua stack.
- The errors originating in thread callbacks can't throw so use another mechanism: `CallbackError()`.

## Files

Source dir:
```
Nebulua
|   - standard C# project for the main app
|   README.md - hello!
|   *.cs
|   etc...
+---Interop - .NET binding to C/Lua - see below
+---lua - lua modules for application
|       bar_time.lua
|       midi_defs.lua
|       music_defs.lua
|       script_api.lua
|       step_types.lua
+---LBOT - LuaBagOfTricks modules for application
+---LINT - LuaInterop modules for building interop
+---examples
|       airport.lua
|       example.lua
+---docs - *.md
+---lib - .NET dependencies
\---test - various test code projects
```


## Updating Interop

The Lua script interop should not need to be rebuilt after the api is finalized.
If a change is required, do this:
- Go to the `Interop` folder.
- Edit `interop_spec.lua` with new changes.
- Execute `gen_interop.cmd`. This generates the code files to support the interop.
- Open `Nebulua.sln` and rebuild all.
