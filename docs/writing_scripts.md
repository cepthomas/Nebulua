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
function api.create_output_channel(dev_name, chan_num, patch)
```
Register an output midi channel.

- dev_name: The system name.
- chan_num: Specific channel number.
- patch: Send this patch number or mid.NO_PATCH. Use NO_PATCH if your host manages its own patches.
- return: A channel handle to use in subsequent functions.


```lua
function api.create_input_channel(dev_name, chan_num)
```
Register an input midi channel.

- dev_name: The system name.
- chan_num: Specific channel number.
- return: A channel handle to use in subsequent functions.


```lua
function api.send_note(chan_hnd, note_num, volume, dur)
```
Send a note on/off immediately. Adds a note off if dur is specified and tick clock is running.

- chan_hnd: The channel handle to send it on.
- note_num: Which.
- volume: Note volume. 0.0 -> 1.0. 0.0 means note off.
- dur: How long it lasts in subbeats. Optional.


```lua
function api.send_controller(chan_hnd, controller, value)
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
If it's a static composition call this in setup();


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

