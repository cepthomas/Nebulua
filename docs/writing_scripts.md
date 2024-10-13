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

Scripts need this section.

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
function neb.process_comp()
```
If it's a static composition call this in setup();


```lua
function neb.process_step(tick)
```
If it's a static composition call this in step(tick);
- tick: current tick.


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

