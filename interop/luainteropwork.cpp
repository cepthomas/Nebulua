#include "lua.hpp"
#include "luainterop.h"
#include "AppInterop.h"

using namespace Nebulua::Interop;

#define MIDI_VAL_MAX 127


// Work functions called by bound lua functions in luainterop.c. TODO1 can go in AppInterop.cpp.


//--------------------------------------------------------//
int luainteropwork_Log(lua_State* l, int level, const char* msg)
{
    LogArgs^ args = gcnew LogArgs();
    args->Sender = MAKE_ID(l);
    args->LogLevel = level;
    args->Message = gcnew String(msg);
    AppInterop::NotifyLog(args); // do work
    return args->Ret; // status
}

//--------------------------------------------------------//
int luainteropwork_SetTempo(lua_State* l, int bpm)
{
    PropertyArgs^ args = gcnew PropertyArgs();
    args->Sender = MAKE_ID(l);
    args->Bpm = bpm;
    AppInterop::NotifyPropertyChange(args); // do work
    return args->Ret; // status
}

//--------------------------------------------------------//
int luainteropwork_CreateInputChannel(lua_State* l, const char* dev_name, int chan_num)
{
    CreateChannelArgs^ args = gcnew CreateChannelArgs();
    args->Sender = MAKE_ID(l);
    args->DevName = gcnew String(dev_name);
    args->ChanNum = chan_num;
    args->IsOutput = false;
    args->Patch = 0;
    AppInterop::NotifyCreateChannel(args); // do work
    return args->Ret; // chan_hnd
}

//--------------------------------------------------------//
int luainteropwork_CreateOutputChannel(lua_State* l, const char* dev_name, int chan_num, int patch)
{
    CreateChannelArgs^ args = gcnew CreateChannelArgs();
    args->Sender = MAKE_ID(l);
    args->DevName = gcnew String(dev_name);
    args->ChanNum = chan_num;
    args->IsOutput = true;
    args->Patch = patch;
    AppInterop::NotifyCreateChannel(args); // do work
    return args->Ret; // chan_hnd
}

//--------------------------------------------------------//
int luainteropwork_SendNote(lua_State* l, int chan_hnd, int note_num, double volume)
{
    SendArgs^ args = gcnew SendArgs();
    args->Sender = MAKE_ID(l);
    args->IsNote = true;
    args->ChanHnd = chan_hnd;
    args->What = note_num;
    args->Value = int(volume * MIDI_VAL_MAX); // convert
    AppInterop::NotifySend(args); // do work
    return args->Ret; // status
}

//--------------------------------------------------------//
int luainteropwork_SendController(lua_State* l, int chan_hnd, int controller, int value)
{
    SendArgs^ args = gcnew SendArgs();
    args->Sender = MAKE_ID(l);
    args->IsNote = false;
    args->ChanHnd = chan_hnd;
    args->What = controller;
    args->Value = value;
    AppInterop::NotifySend(args); // do work
    return args->Ret; // status
}
