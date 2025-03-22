///// Warning - this file is created by gen_interop.lua - do not edit. /////

#pragma once
#include "InteropCore.h"

using namespace System;
using namespace System::Collections::Generic;

namespace Script
{

//============= C => C# callback payload .h =============//

//--------------------------------------------------------//
public ref class CreateOutputChannelArgs : public EventArgs
{
public:
    /// <summary>Midi device name</summary>
    property String^ dev_name;
    /// <summary>Midi channel number 1 => 16</summary>
    property int chan_num;
    /// <summary>Midi patch number 0 => 127</summary>
    property int patch;
    /// <summary>Channel handle or 0 if invalid</summary>
    property int ret;
    /// <summary>Constructor.</summary>
    CreateOutputChannelArgs(const char* dev_name, int chan_num, int patch)
    {
        this->dev_name = gcnew String(dev_name);
        this->chan_num = chan_num;
        this->patch = patch;
    }
};

//--------------------------------------------------------//
public ref class CreateInputChannelArgs : public EventArgs
{
public:
    /// <summary>Midi device name</summary>
    property String^ dev_name;
    /// <summary>Midi channel number 1 => 16</summary>
    property int chan_num;
    /// <summary>Channel handle or 0 if invalid</summary>
    property int ret;
    /// <summary>Constructor.</summary>
    CreateInputChannelArgs(const char* dev_name, int chan_num)
    {
        this->dev_name = gcnew String(dev_name);
        this->chan_num = chan_num;
    }
};

//--------------------------------------------------------//
public ref class LogArgs : public EventArgs
{
public:
    /// <summary>Log level</summary>
    property int level;
    /// <summary>Log message</summary>
    property String^ msg;
    /// <summary>Unused</summary>
    property int ret;
    /// <summary>Constructor.</summary>
    LogArgs(int level, const char* msg)
    {
        this->level = level;
        this->msg = gcnew String(msg);
    }
};

//--------------------------------------------------------//
public ref class SetTempoArgs : public EventArgs
{
public:
    /// <summary>BPM 40 => 240</summary>
    property int bpm;
    /// <summary>Unused</summary>
    property int ret;
    /// <summary>Constructor.</summary>
    SetTempoArgs(int bpm)
    {
        this->bpm = bpm;
    }
};

//--------------------------------------------------------//
public ref class SendNoteArgs : public EventArgs
{
public:
    /// <summary>Output channel handle</summary>
    property int chan_hnd;
    /// <summary>Note number</summary>
    property int note_num;
    /// <summary>Volume 0.0 => 1.0</summary>
    property double volume;
    /// <summary>Unused</summary>
    property int ret;
    /// <summary>Constructor.</summary>
    SendNoteArgs(int chan_hnd, int note_num, double volume)
    {
        this->chan_hnd = chan_hnd;
        this->note_num = note_num;
        this->volume = volume;
    }
};

//--------------------------------------------------------//
public ref class SendControllerArgs : public EventArgs
{
public:
    /// <summary>Output channel handle</summary>
    property int chan_hnd;
    /// <summary>Specific controller 0 => 127</summary>
    property int controller;
    /// <summary>Payload 0 => 127</summary>
    property int value;
    /// <summary>Unused</summary>
    property int ret;
    /// <summary>Constructor.</summary>
    SendControllerArgs(int chan_hnd, int controller, int value)
    {
        this->chan_hnd = chan_hnd;
        this->controller = controller;
        this->value = value;
    }
};


//----------------------------------------------------//
public ref class Interop : InteropCore::Core
{

//============= C# => C functions .h =============//
public:

    /// <summary>Setup</summary>
    /// <returns>Script return</returns>
    String^ Setup();

    /// <summary>Step</summary>
    /// <param name="tick">Current tick 0 => N</param>
    /// <returns>Script return</returns>
    int Step(int tick);

    /// <summary>RcvNote</summary>
    /// <param name="chan_hnd">Input channel handle</param>
    /// <param name="note_num">Note number 0 => 127</param>
    /// <param name="volume">Volume 0.0 => 1.0</param>
    /// <returns>Script return</returns>
    int RcvNote(int chan_hnd, int note_num, double volume);

    /// <summary>RcvController</summary>
    /// <param name="chan_hnd">Input channel handle</param>
    /// <param name="controller">Specific controller id 0 => 127</param>
    /// <param name="value">Payload 0 => 127</param>
    /// <returns>Script return</returns>
    int RcvController(int chan_hnd, int controller, int value);

    /// <summary>NebCommand</summary>
    /// <param name="cmd">Specific command</param>
    /// <param name="arg">Optional argument</param>
    /// <returns>Script return</returns>
    String^ NebCommand(String^ cmd, String^ arg);

//============= C => C# callback functions =============//
public:
    static event EventHandler<CreateOutputChannelArgs^>^ CreateOutputChannel;
    static void Notify(CreateOutputChannelArgs^ args) { CreateOutputChannel(nullptr, args); }

    static event EventHandler<CreateInputChannelArgs^>^ CreateInputChannel;
    static void Notify(CreateInputChannelArgs^ args) { CreateInputChannel(nullptr, args); }

    static event EventHandler<LogArgs^>^ Log;
    static void Notify(LogArgs^ args) { Log(nullptr, args); }

    static event EventHandler<SetTempoArgs^>^ SetTempo;
    static void Notify(SetTempoArgs^ args) { SetTempo(nullptr, args); }

    static event EventHandler<SendNoteArgs^>^ SendNote;
    static void Notify(SendNoteArgs^ args) { SendNote(nullptr, args); }

    static event EventHandler<SendControllerArgs^>^ SendController;
    static void Notify(SendControllerArgs^ args) { SendController(nullptr, args); }


//============= Infrastructure .h =============//
public:
    /// <summary>Initialize and execute.</summary>
    /// <param name="scriptFn">The script to load.</param>
    /// <param name="luaPath">LUA_PATH components</param>
    void Run(String^ scriptFn, String^ luaPath);
};

}
