#include "lua.hpp"
#include "luainterop.h"
#include "Api.h"


//---------------- Work functions - see luainterop.h/cpp -------------//

#define MIDI_VAL_MAX 127

#pragma warning(disable : 4302 4311) // lua_State* casting


//--------------------------------------------------------//
int luainteropwork_Log(lua_State* l, int level, const char* msg)
{
    Interop::LogArgs^ args = gcnew Interop::LogArgs();
    args->Id = (long)l;
    args->LogLevel = level;
    args->Msg = gcnew String(msg);

    Interop::Api::NotifyLog(args); // do work
    return args->Ret; // status
}

//--------------------------------------------------------//
int luainteropwork_SetTempo(lua_State* l, int bpm)
{
    Interop::PropertyArgs^ args = gcnew Interop::PropertyArgs();
    args->Id = (long)l;
    args->Bpm = bpm;
    Interop::Api::NotifyPropertyChange(args); // do work
    return args->Ret; // status
}

//--------------------------------------------------------//
int luainteropwork_CreateInputChannel(lua_State* l, const char* dev_name, int chan_num)
{
    Interop::CreateChannelArgs^ args = gcnew Interop::CreateChannelArgs();
    args->Id = (long)l;
    args->DevName = gcnew String(dev_name);
    args->ChanNum = chan_num;
    args->IsOutput = false;
    args->Patch = 0;
    Interop::Api::NotifyCreateChannel(args); // do work
    return args->Ret; // chan_hnd
}

//--------------------------------------------------------//
int luainteropwork_CreateOutputChannel(lua_State* l, const char* dev_name, int chan_num, int patch)
{
    Interop::CreateChannelArgs^ args = gcnew Interop::CreateChannelArgs();
    args->Id = (long)l;
    args->DevName = gcnew String(dev_name);
    args->ChanNum = chan_num;
    args->IsOutput = true;
    args->Patch = patch;
    Interop::Api::NotifyCreateChannel(args); // do work
    return args->Ret; // chan_hnd
}

//--------------------------------------------------------//
int luainteropwork_SendNote(lua_State* l, int chan_hnd, int note_num, double volume)
{
    Interop::SendArgs^ args = gcnew Interop::SendArgs();
    args->Id = (long)l;
    args->IsNote = true;
    args->ChanHnd = chan_hnd;
    args->What = note_num;
    args->Value = int(volume * MIDI_VAL_MAX); // convert
    Interop::Api::NotifySend(args); // do work
    return args->Ret; // status
}

//--------------------------------------------------------//
int luainteropwork_SendController(lua_State* l, int chan_hnd, int controller, int value)
{
    Interop::SendArgs^ args = gcnew Interop::SendArgs();
    args->Id = (long)l;
    args->IsNote = false;
    args->ChanHnd = chan_hnd;
    args->What = controller;
    args->Value = value;
    Interop::Api::NotifySend(args); // do work
    return args->Ret; // status
}
