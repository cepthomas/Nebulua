
# Nebulua

A simplified version of [Nebulator](https://github.com/cepthomas/Nebulator.git) using Lua as the script flavor.
While the primary intent is to generate music-by-code, runtime interaction is also supported using midi inputs.
Windows only.

Currently this is a build-and-run-it-yourself configuration. Eventually an installer may be provided.
Note that running in VS debugger has a very slow startup. Running from the exe or cli is ok.

Building this solution requires a folder named `LBOT` at the top level containing the contents of
  [LuaBagOfTricks](https://github.com/cepthomas/LuaBagOfTricks). This can be done one of several ways:
  - git submodule
  - copy of pertinent parts
  - symlink: `mklink /d this_path\Nebulua\LBOT source_path\LuaBagOfTricks` and `mklink /d this_path\Nebulua\LINT source_path\LuaInterop`

It's called Nebulator after a MarkS C++ noisemaker called Nebula which manipulated synth parameters via code.

![logo](docs/marks.png)

## Usage

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


## The Documentation

- [Definitions](docs/definitions.md)
- [Writing Scripts](docs/writing_scripts.md)
- [Tech Notes](docs/tech_notes.md)
- [Builtin Chords and Scales](docs/music_defs.md)
- [Midi GM Definitions](docs/midi_defs.md)


## External Components

This application uses these FOSS components:
- [NAudio](https://github.com/naudio/NAudio).
