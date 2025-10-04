///// Warning - this file is created by gen_interop.lua - do not edit. /////

#include <windows.h>
#include "luainterop.h"
#include "Interop.h"

using namespace System;
using namespace System::Collections::Generic;


//============= C# => C functions .cpp =============//

//--------------------------------------------------------//
String^ Interop::Setup()
{
    SCOPE();
    String^ ret = gcnew String(luainterop_Setup(_l));
EvalLuaInteropStatus("TODO1 delete me", "Setup()");
    EvalLuaInteropStatus(luainterop_Error(), "Setup()");
    return ret;
}

//--------------------------------------------------------//
int Interop::Step(int tick)
{
    SCOPE();
    int ret = luainterop_Step(_l, tick);
    EvalLuaInteropStatus(luainterop_Error(), "Step()");
    return ret; 
}

//--------------------------------------------------------//
int Interop::ReceiveMidiNote(int chan_hnd, int note_num, double volume)
{
    SCOPE();
    int ret = luainterop_ReceiveMidiNote(_l, chan_hnd, note_num, volume);
    EvalLuaInteropStatus(luainterop_Error(), "ReceiveMidiNote()");
    return ret; 
}

//--------------------------------------------------------//
int Interop::ReceiveMidiController(int chan_hnd, int controller, int value)
{
    SCOPE();
    int ret = luainterop_ReceiveMidiController(_l, chan_hnd, controller, value);
    EvalLuaInteropStatus(luainterop_Error(), "ReceiveMidiController()");
    return ret; 
}


//============= C => C# callback functions .cpp =============//


//--------------------------------------------------------//

int luainteropcb_OpenMidiOutput(lua_State* l, const char* dev_name, int chan_num, const char* chan_name, int patch)
{
    SCOPE();
    OpenMidiOutputArgs^ args = gcnew OpenMidiOutputArgs(dev_name, chan_num, chan_name, patch);
    Interop::Notify(args);
    return args->ret;
}


//--------------------------------------------------------//

int luainteropcb_OpenMidiInput(lua_State* l, const char* dev_name, int chan_num, const char* chan_name)
{
    SCOPE();
    OpenMidiInputArgs^ args = gcnew OpenMidiInputArgs(dev_name, chan_num, chan_name);
    Interop::Notify(args);
    return args->ret;
}


//--------------------------------------------------------//

int luainteropcb_SendMidiNote(lua_State* l, int chan_hnd, int note_num, double volume)
{
    SCOPE();
    SendMidiNoteArgs^ args = gcnew SendMidiNoteArgs(chan_hnd, note_num, volume);
    Interop::Notify(args);
    return args->ret;
}


//--------------------------------------------------------//

int luainteropcb_SendMidiController(lua_State* l, int chan_hnd, int controller, int value)
{
    SCOPE();
    SendMidiControllerArgs^ args = gcnew SendMidiControllerArgs(chan_hnd, controller, value);
    Interop::Notify(args);
    return args->ret;
}


//--------------------------------------------------------//

int luainteropcb_Log(lua_State* l, int level, const char* msg)
{
    SCOPE();
    LogArgs^ args = gcnew LogArgs(level, msg);
    Interop::Notify(args);
    return args->ret;
}


//--------------------------------------------------------//

int luainteropcb_SetTempo(lua_State* l, int bpm)
{
    SCOPE();
    SetTempoArgs^ args = gcnew SetTempoArgs(bpm);
    Interop::Notify(args);
    return args->ret;
}


//============= Infrastructure .cpp =============//

//--------------------------------------------------------//
void Interop::Run(String^ scriptFn, String^ luaPath)
{
    InitLua(luaPath);
    // Load C host funcs into lua space.
    luainterop_Load(_l);
    // Clean up stack.
    lua_pop(_l, 1);
    OpenScript(scriptFn);
}
