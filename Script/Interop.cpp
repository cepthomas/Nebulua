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
int Interop::RcvNote(int chan_hnd, int note_num, double volume)
{
    SCOPE();
    int ret = luainterop_RcvNote(_l, chan_hnd, note_num, volume);
    EvalLuaInteropStatus(luainterop_Error(), "RcvNote()");
    return ret; 
}

//--------------------------------------------------------//
int Interop::RcvController(int chan_hnd, int controller, int value)
{
    SCOPE();
    int ret = luainterop_RcvController(_l, chan_hnd, controller, value);
    EvalLuaInteropStatus(luainterop_Error(), "RcvController()");
    return ret; 
}


//============= C => C# callback functions .cpp =============//


//--------------------------------------------------------//

int luainteropcb_CreateOutputChannel(lua_State* l, const char* dev_name, int chan_num, int patch)
{
    SCOPE();
    CreateOutputChannelArgs^ args = gcnew CreateOutputChannelArgs(dev_name, chan_num, patch);
    Interop::Notify(args);
    return args->ret;
}


//--------------------------------------------------------//

int luainteropcb_CreateInputChannel(lua_State* l, const char* dev_name, int chan_num)
{
    SCOPE();
    CreateInputChannelArgs^ args = gcnew CreateInputChannelArgs(dev_name, chan_num);
    Interop::Notify(args);
    return args->ret;
}


//--------------------------------------------------------//

int luainteropcb_SendNote(lua_State* l, int chan_hnd, int note_num, double volume)
{
    SCOPE();
    SendNoteArgs^ args = gcnew SendNoteArgs(chan_hnd, note_num, volume);
    Interop::Notify(args);
    return args->ret;
}


//--------------------------------------------------------//

int luainteropcb_SendController(lua_State* l, int chan_hnd, int controller, int value)
{
    SCOPE();
    SendControllerArgs^ args = gcnew SendControllerArgs(chan_hnd, controller, value);
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
