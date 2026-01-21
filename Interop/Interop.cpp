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
int Interop::ReceiveNote(int chan_hnd, int note_num, double volume)
{
    int ret = luainterop_ReceiveNote(_l, chan_hnd, note_num, volume);
    if (luainterop_Error() != NULL) { throw(gcnew LuaException(gcnew String(luainterop_Error()), luainterop_Context() == NULL ? "" : gcnew String(luainterop_Context()))); }
    Collect();
    return ret; 
}

//--------------------------------------------------------//
int Interop::ReceiveController(int chan_hnd, int controller, int value)
{
    int ret = luainterop_ReceiveController(_l, chan_hnd, controller, value);
    if (luainterop_Error() != NULL) { throw(gcnew LuaException(gcnew String(luainterop_Error()), luainterop_Context() == NULL ? "" : gcnew String(luainterop_Context()))); }
    Collect();
    return ret; 
}


//============= C/Lua => Cpp/CLI functions =============//


//--------------------------------------------------------//

int luainterop_cb_OpenOutputChannel(lua_State* l, const char* dev_name, int chan_num, const char* chan_name, int patch)
{
    OpenOutputChannelArgs^ args = gcnew OpenOutputChannelArgs(dev_name, chan_num, chan_name, patch);
    Interop::Notify(args);
    return args->ret;
}


//--------------------------------------------------------//

int luainterop_cb_OpenInputChannel(lua_State* l, const char* dev_name, int chan_num, const char* chan_name)
{
    OpenInputChannelArgs^ args = gcnew OpenInputChannelArgs(dev_name, chan_num, chan_name);
    Interop::Notify(args);
    return args->ret;
}


//--------------------------------------------------------//

int luainterop_cb_SendNote(lua_State* l, int chan_hnd, int note_num, double volume)
{
    SendNoteArgs^ args = gcnew SendNoteArgs(chan_hnd, note_num, volume);
    Interop::Notify(args);
    return args->ret;
}


//--------------------------------------------------------//

int luainterop_cb_SendController(lua_State* l, int chan_hnd, int controller, int value)
{
    SendControllerArgs^ args = gcnew SendControllerArgs(chan_hnd, controller, value);
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
