# Tech Notes

## Build

- Windows 64 bit only. Build it with VS2022 and .NET8.
- Uses 64 bit Lua 5.4.2 from [here](https://luabinaries.sourceforge.net/download.html).
- Uses [C code conventions](https://github.com/cepthomas/c_bag_of_tricks/blob/master/conventions.md).

`builder.lua` does most of the work to build, test, etc.


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
