# Nebulua TODO1 update all + parts of Nebulator\README.md

- An experimental version of Nebulator using Lua as the script flavor.
- Windows only.
- Windows 64 bit. Build with VS or mingw64. I'm using 64 bit Lua 5.4.2 from https://luabinaries.sourceforge.net/download.html.

- Test code is Windows 64 bit build using CMake. Your PATH must include `...\mingw64\bin`. <<<<<?????

- Uses [C code conventions](https://github.com/cepthomas/c_bag_of_tricks/blob/master/conventions.md).


> For build/test Add to env: LBOT = C:\Dev\repos\Lua\LuaBagOfTricks

- No installer yet, it's a build-it-yerself for now. Eventually a nuget package might be created.

- Since the built-in Windows GM player sounds terrible, there are a couple of options for playing midi locally:
  - Replace it with [virtualmidisynth](https://coolsoft.altervista.org/en/virtualmidisynth) and your favorite soundfont.
  - If you are using a DAW for sound generation, you can use a virtual midi loopback like [loopMIDI](http://www.tobias-erichsen.de/software/loopmidi.html) to send to it.



- Most music software uses piano roll midi editors. This is an alternative - writing scripts to generate sounds.
- C# makes a reasonable scripting language, given that we have the compiler available to us at run time.
- Supports midi and midi-over-OSC.
- While the primary intent is to generate music-by-code, runtime interaction is also supported using midi or OSC inputs.
- It's called Nebulator after a MarkS C++ noisemaker called Nebula which allowed manipulation of synth parameters using code.
- Requires VS2022 and .NET6.

![logo](marks.jpg)

## Glossary
L2C - Lua to C aka script calls host functions.
C2L - C to Lua aka host calls script functions

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


# Writing Scripts - syntax

- Use your favorite external text editor. The application will watch for changes you make and indicate that recompile
  is needed. I use Sublime - you can associate .neb files with C# for pretty-close syntax coloring.

## Musical Notes/Chords/Scales

Scales and chords are specified by strings like `"1 4 6 b13"`.
There are many builtin defined in music_defs.lua.
Users can add their own by using `CreateNotes("FOO", "1 4 6 b13")`.

Notes (single) and note groups (chords, scales) are referenced in several ways:
- "F4" - Named note with octave.
- "F4.m7" - Named chord in the key of middle F.
- "F4.Aeolian" - Named scale in the key of middle F.
- "F4.FOO" - Custom chord or scale created with `CreateNotes()`.
- SideStick - Drum name from the definitions.
- 57 - Simple midi note number.


## Composition
A composition is comprised of one or more Sections each of which has one or more Sequences of notes.
You first create your Sequences like this:
```lua
Sequence CreateSequence(beats, elements[]);
```


Graphical format:
"|7-------|" is one beat with 8 subs
note velocity is 1-9 (map) or - which means sustained
note/chord, velocity/volume
List format:
times are beat.sub where beat is 0-N sub is 0-7
note/chord, velocity/volume is 0.0 to 1.0, duration is 0.1 to N.7



There are several ways to do this. (see `example.neb`.)
A list of notes:
```lua
Sequence seq1 = CreateSequence(8, new()
{
    { 0.0, "F4",  0.7, 0.2 },
    { 0.4, "D#4", 1.1, 0.2 },
    { 1.0, "C4",  0.7, 0.2 },
});
```
- 8 is the number of beats in the sequence.
- Notes are `{ when to play in the sequence, note or chord or drum, volume, duration }`. If duration is omitted, it defaults to 0.1, useful for drum hits.

```lua
Sequence seq2 = CreateSequence(8, new()
{
    { "|7-------|--      |        |        |7-------|--      |        |        |", "G4.m7", 0.9  },
    { "|        |        |        |5---    |        |        |        |5-8---  |", "G4.m6", 0.75 },
});
```
- Notes are `{ pattern, note or chord or drum, volume, duration }`.
- Pattern: describes a sequence of notes, kind of like a piano roll. `1 to 9` (volume) starts a note which is held 
  for subsequent `-`. The note is ended with any other character than `-`. `|`, `.` and ` ` are ignored, 
  used for visual assist only. These are particularly useful for drum patterns.

```lua
Sequence seqAlgo = CreateSequence(4, new()
{
    { 1.2, AlgoFunc, 0.8 },
});

void AlgoFunc()
{
    int notenum = Random(0, scaleNotes.Count());
    SendNote("synth", scaleNotes[notenum], 0.7, 0.5);
}
```
- Notes are `{ when, function, volume }`.

Then you group Sequences into Sections, typically things like verse, chorus, bridge, etc.
```lua
Section sectMiddle = CreateSection(32, "Middle", new()
{
    { "keys",  seqKeysChorus  },
    { "drums", seqDrumsChorus },
    { "bass",  seqBassChorus  },
    { "synth", seqAlgo, seqEmpty, seqAlgo, seqDynamic, seqEmpty }
});
```
- beats: Overall length in beats.
- name: Displayed in time control while playing.
- sequences: 1 to N descriptors of which sequences to play sequentially. They are played in order and repeat to fill the section.



### User Script Functions
These can/must be overridden in the user script.

```lua
public override void Setup();
```
Called once to initialize your script stuff.

```lua
public override void Step();
```
Called every Subbeat/tick.

```lua
public override void InputNote(dev, chnum, note, vel);
```
Called when input note arrives.

- dev: DeviceType.
- chnum: Channel number.
- note: Note number.
- vel: velocity

```lua
public override void InputControl(dev, chnum, ctlid, value);
```
Called when input controller arrives.
- dev: DeviceType.
- chnum: Channel number.
- ctlid: ControllerDef.
- value: Controller value.


### Send Functions
Call these from inside your script.

```lua
void SendNote("chname", note, vol, dur)
```
Send a note immediately. _Respects solo/mute._ Adds a note off to play after dur time.

- chname: Channel name to send it on.
- note: One of the note definitions.
- vol: Note volume. Normalized to 0.0 - 1.0. 0.0 means note off.
- dur: How long it lasts in beats or BarTime object representation.

```lua
void SendNote("chname", note, vol)
```
Send a note on immediately. _Respects solo/mute._ If note on adds/follows note off.

- chname: Channel name to send it on.
- note: One of the note definitions.
- vol: Note volume. Normalized to 0.0 - 1.0.

```lua
void SendController("chname", ctl, val)
```
Send a controller immediately. Useful for things like panning and bank select.

- chname: Channel name to send it on.
- ctl: Controller name from the definitions or const() or simple integer.
- val: Controller value.

### Utilities

```lua
void CreateNotes("name", "parts")
```
Define a group of notes for use as a note, or in a chord or scale.

- name: Reference name.
- note: List of note definitions.

```lua
List<double> GetNotes("scale_or_chord", "key")
```
Get an array of scale or chord notes.

- scale: One of the named scales from ScriptDefinitions.md or defined in `notes`.
- key: Note name and octave.
- returns: Array of notes or empty if invalid.



### Channel
```lua
Channel("chname", "devid", chnum, "patch")
```

Defines an output channel.

- chname: For display in the UI.
- devid: A user-specified device id as entered in the user settings.
- chnum: Channel number to play on.
- patch: Name of the patch.





## Files => _files.txt_

- `source_code` folder:
    - exec.c/h - Does all the top-level work.
    - etc...
- `lib\lua` folder:
- `lua_code` folder:
    - nebulua.lua - Lua script for a simplistic multithreaded coroutine application. Uses luatoc.
    - utils.lua - Used by demoapp.lua.
- `test_code` folder:
    - lua source code for this application...
- `examples` folder:
    - lua...
- `mingw` folder:
- `app` and `app\test` folder:

# Example Script Files
See the Examples directory for material while perusing the docs.

File        | Description
----------- | -----------
example.neb | Source file showing example of static sequence and loop definitions, and creating notes by script functions.
airport.neb | A take on Eno's Music for Airports - adapted from [this](https://github.com/teropa/musicforairports.js).
utils.neb   | Example of a library file for simple functions.
scale.neb   | Example of a library file for playing with a scale.
*.nebp      | Storage for dynamic stuff. This is created and managed by the application and not generally manually edited.
temp\\\*.cs | Generated C# files which are compiled and executed.
example.mp3 | A bit of some generated sound (not music!) using Reaper with good instruments and lots of reverb. I like lots of reverb.
airport.mp3 | Snippet generated by airport.neb and Reaper.


# Design

- Describe arch, files, how to build this.

## Files => _files.txt_

- `source_code` folder:
    - exec.c/h - Does all the top-level work.
    - etc...
- `lib\lua` folder:
- `lua_code` folder:
    - nebulua.lua - Lua script for a simplistic multithreaded coroutine application. Uses luatoc.
    - utils.lua - Used by demoapp.lua.
- `test_code` folder:
    - lua source code for this application...
- `examples` folder:
    - lua...
- `mingw` folder:
- `app` and `app\test` folder:



# External Components

This application uses these FOSS components:
- [NAudio](https://github.com/naudio/NAudio) (Microsoft Public License).
- Application icon: [Charlotte Schmidt](http://pattedemouche.free.fr/) (Copyright © 2009 of Charlotte Schmidt).
