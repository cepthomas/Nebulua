///// Warning - this file is created by do_gen.lua - do not edit. /////

#include <windows.h>
#include "luainterop.h"
#include "Interop.h"

using namespace System;
using namespace System::Collections::Generic;


//============= Cpp/CLI => C/Lua functions =============//

//--------------------------------------------------------//
String^ Interop::Setup()
{
    String^ ret = gcnew String(luainterop_Setup(_l));
    if (luainterop_Error() != NULL) { throw(gcnew LuaException(gcnew String(luainterop_Error()), luainterop_Context() == NULL ? "" : gcnew String(luainterop_Context()))); }
    Collect();
    return ret; 
}

//--------------------------------------------------------//
int Interop::Step(int tick)
{
    int ret = luainterop_Step(_l, tick);
    if (luainterop_Error() != NULL) { throw(gcnew LuaException(gcnew String(luainterop_Error()), luainterop_Context() == NULL ? "" : gcnew String(luainterop_Context()))); }
    Collect();
    return ret; 
}

//--------------------------------------------------------//
int Interop::ReceiveMidiNote(int chan_hnd, int note_num, double volume)
{
    int ret = luainterop_ReceiveMidiNote(_l, chan_hnd, note_num, volume);
    if (luainterop_Error() != NULL) { throw(gcnew LuaException(gcnew String(luainterop_Error()), luainterop_Context() == NULL ? "" : gcnew String(luainterop_Context()))); }
    Collect();
    return ret; 
}

//--------------------------------------------------------//
int Interop::ReceiveMidiController(int chan_hnd, int controller, int value)
{
    int ret = luainterop_ReceiveMidiController(_l, chan_hnd, controller, value);
    if (luainterop_Error() != NULL) { throw(gcnew LuaException(gcnew String(luainterop_Error()), luainterop_Context() == NULL ? "" : gcnew String(luainterop_Context()))); }
    Collect();
    return ret; 
}


//============= C/Lua => Cpp/CLI functions =============//


//--------------------------------------------------------//

int luainterop_cb_OpenMidiOutput(lua_State* l, const char* dev_name, int chan_num, const char* chan_name, int patch)
{
    OpenMidiOutputArgs^ args = gcnew OpenMidiOutputArgs(dev_name, chan_num, chan_name, patch);
    Interop::Notify(args);
    return args->ret;
}


//--------------------------------------------------------//

int luainterop_cb_OpenMidiInput(lua_State* l, const char* dev_name, int chan_num, const char* chan_name)
{
    OpenMidiInputArgs^ args = gcnew OpenMidiInputArgs(dev_name, chan_num, chan_name);
    Interop::Notify(args);
    return args->ret;
}


//--------------------------------------------------------//

int luainterop_cb_SendMidiNote(lua_State* l, int chan_hnd, int note_num, double volume)
{
    SendMidiNoteArgs^ args = gcnew SendMidiNoteArgs(chan_hnd, note_num, volume);
    Interop::Notify(args);
    return args->ret;
}


//--------------------------------------------------------//

int luainterop_cb_SendMidiController(lua_State* l, int chan_hnd, int controller, int value)
{
    SendMidiControllerArgs^ args = gcnew SendMidiControllerArgs(chan_hnd, controller, value);
    Interop::Notify(args);
    return args->ret;
}


//--------------------------------------------------------//

int luainterop_cb_Log(lua_State* l, int level, const char* msg)
{
    LogArgs^ args = gcnew LogArgs(level, msg);
    Interop::Notify(args);
    return args->ret;
}


//--------------------------------------------------------//

int luainterop_cb_SetTempo(lua_State* l, int bpm)
{
    SetTempoArgs^ args = gcnew SetTempoArgs(bpm);
    Interop::Notify(args);
    return args->ret;
}


//============= Infrastructure =============//

//--------------------------------------------------------//
void Interop::RunScript(String^ scriptFn, String^ luaPath)
{
    InitLua(luaPath);
    // Load C host funcs into lua space.
    luainterop_Load(_l);
    // Clean up stack.
    lua_pop(_l, 1);
    OpenScript(scriptFn);
}

//--------------------------------------------------------//
void Interop::RunChunk(String^ code, String^ name, String^ luaPath)
{
    InitLua(luaPath);
    // Load C host funcs into lua space.
    luainterop_Load(_l);
    // Clean up stack.
    lua_pop(_l, 1);
    OpenChunk(code, name);
}
