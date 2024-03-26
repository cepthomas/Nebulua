#include "lua.hpp"
#include "nebcommon.h"
#include "luainterop.h"
#include "Api.h"


//---------------- Work functions - see luainterop.h/cpp -------------//

//--------------------------------------------------------//
int luainteropwork_Log(int level, const char* msg)
{
    Interop::LogEventArgs^ args = gcnew Interop::LogEventArgs();
    args->LogLevel = level;
    args->Msg = gcnew String(msg);
    Interop::Api::Instance->NotifyLogEvent(args);
    return args->Ret; // status
}

//--------------------------------------------------------//
int luainteropwork_SetTempo(int bpm)
{
    Interop::ScriptEventArgs^ args = gcnew Interop::ScriptEventArgs();
    args->Bpm = bpm;
    Interop::Api::Instance->NotifyScriptEvent(args);
    return args->Ret; // status
}

//--------------------------------------------------------//
int luainteropwork_CreateInputChannel(const char* dev_name, int chan_num)
{
    Interop::CreateChannelEventArgs^ args = gcnew Interop::CreateChannelEventArgs();
    args->DevName = gcnew String(dev_name);
    args->ChanNum = chan_num;
    args->IsOutput = false;
    args->Patch = 0;
    Interop::Api::Instance->NotifyCreateChannelEvent(args);
    return args->Ret; // chan_hnd;
}

//--------------------------------------------------------//
int luainteropwork_CreateOutputChannel(const char* dev_name, int chan_num, int patch)
{
    Interop::CreateChannelEventArgs^ args = gcnew Interop::CreateChannelEventArgs();
    args->DevName = gcnew String(dev_name);
    args->ChanNum = chan_num;
    args->IsOutput = true;
    args->Patch = patch;
    Interop::Api::Instance->NotifyCreateChannelEvent(args);
    return args->Ret; // chan_hnd;
}

//--------------------------------------------------------//
int luainteropwork_SendNote(int chan_hnd, int note_num, double volume)
{
    Interop::SendEventArgs^ args = gcnew Interop::SendEventArgs();
    args->IsNote = true;
    args->ChanHnd = chan_hnd;
    args->What = note_num;
    args->Value = int(volume * MIDI_VAL_MAX); // convert
    Interop::Api::Instance->NotifySendEvent(args);
    return args->Ret; // stat;
}

//--------------------------------------------------------//
int luainteropwork_SendController(int chan_hnd, int controller, int value)
{
    Interop::SendEventArgs^ args = gcnew Interop::SendEventArgs();
    args->IsNote = false;
    args->ChanHnd = chan_hnd;
    args->What = controller;
    args->Value = value;
    Interop::Api::Instance->NotifySendEvent(args);
    return args->Ret; // stat;
}
