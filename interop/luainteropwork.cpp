#include "lua.hpp"
#include "luainterop.h"
#include "Api.h"


//---------------- Work functions - see luainterop.h/cpp -------------//

#define MIDI_VAL_MAX 127


//--------------------------------------------------------//
int luainteropwork_Log(lua_State* l, int level, const char* msg)
{
    Interop::LogEventArgs^ args = gcnew Interop::LogEventArgs();
    args->LogLevel = level;
    args->Msg = gcnew String(msg);
    Interop::Api::Instance->NotifyLogEvent(args); // do work
    return args->Ret; // status
}

//--------------------------------------------------------//
int luainteropwork_SetTempo(lua_State* l, int bpm)
{
    Interop::ScriptEventArgs^ args = gcnew Interop::ScriptEventArgs();
    args->Bpm = bpm;
    Interop::Api::Instance->NotifyScriptEvent(args); // do work
    return args->Ret; // status
}

//--------------------------------------------------------//
int luainteropwork_CreateInputChannel(lua_State* l, const char* dev_name, int chan_num)
{
    Interop::CreateChannelEventArgs^ args = gcnew Interop::CreateChannelEventArgs();
    args->DevName = gcnew String(dev_name);
    args->ChanNum = chan_num;
    args->IsOutput = false;
    args->Patch = 0;
    Interop::Api::Instance->NotifyCreateChannelEvent(args); // do work
    return args->Ret; // chan_hnd
}

//--------------------------------------------------------//
int luainteropwork_CreateOutputChannel(lua_State* l, const char* dev_name, int chan_num, int patch)
{
    Interop::CreateChannelEventArgs^ args = gcnew Interop::CreateChannelEventArgs();
    args->DevName = gcnew String(dev_name);
    args->ChanNum = chan_num;
    args->IsOutput = true;
    args->Patch = patch;
    Interop::Api::Instance->NotifyCreateChannelEvent(args); // do work
    return args->Ret; // chan_hnd
}

//--------------------------------------------------------//
int luainteropwork_SendNote(lua_State* l, int chan_hnd, int note_num, double volume)
{
    Interop::SendEventArgs^ args = gcnew Interop::SendEventArgs();
    args->IsNote = true;
    args->ChanHnd = chan_hnd;
    args->What = note_num;
    //args->Value = volume;
    args->Value = int(volume * MIDI_VAL_MAX); // convert TODO2 prefer in client?
    Interop::Api::Instance->NotifySendEvent(args); // do work
    return args->Ret; // status
}

//--------------------------------------------------------//
int luainteropwork_SendController(lua_State* l, int chan_hnd, int controller, int value)
{
    Interop::SendEventArgs^ args = gcnew Interop::SendEventArgs();
    args->IsNote = false;
    args->ChanHnd = chan_hnd;
    args->What = controller;
    args->Value = value;
    Interop::Api::Instance->NotifySendEvent(args); // do work
    return args->Ret; // status
}
