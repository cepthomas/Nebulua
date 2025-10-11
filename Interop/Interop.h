///// Warning - this file is created by do_gen.lua - do not edit. /////

#pragma once
#include "cliex.h"

using namespace System;
using namespace System::Collections::Generic;

//============= interop C => Cpp/CLI callback payload =============//

//--------------------------------------------------------//
public ref class OpenMidiOutputArgs : public EventArgs
{
public:
    /// <summary>Midi device name</summary>
    property String^ dev_name;
    /// <summary>Midi channel number 1 => 16</summary>
    property int chan_num;
    /// <summary>User channel name</summary>
    property String^ chan_name;
    /// <summary>Midi patch number 0 => 127</summary>
    property int patch;
    /// <summary>Channel handle or 0 if invalid</summary>
    property int ret;
    /// <summary>Constructor.</summary>
    OpenMidiOutputArgs(const char* dev_name, int chan_num, const char* chan_name, int patch)
    {
        this->dev_name = gcnew String(dev_name);
        this->chan_num = chan_num;
        this->chan_name = gcnew String(chan_name);
        this->patch = patch;
    }
};

//--------------------------------------------------------//
public ref class OpenMidiInputArgs : public EventArgs
{
public:
    /// <summary>Midi device name</summary>
    property String^ dev_name;
    /// <summary>Midi channel number 1 => 16 or 0 => all</summary>
    property int chan_num;
    /// <summary>User channel name</summary>
    property String^ chan_name;
    /// <summary>Channel handle or 0 if invalid</summary>
    property int ret;
    /// <summary>Constructor.</summary>
    OpenMidiInputArgs(const char* dev_name, int chan_num, const char* chan_name)
    {
        this->dev_name = gcnew String(dev_name);
        this->chan_num = chan_num;
        this->chan_name = gcnew String(chan_name);
    }
};

//--------------------------------------------------------//
public ref class SendMidiNoteArgs : public EventArgs
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
    SendMidiNoteArgs(int chan_hnd, int note_num, double volume)
    {
        this->chan_hnd = chan_hnd;
        this->note_num = note_num;
        this->volume = volume;
    }
};

//--------------------------------------------------------//
public ref class SendMidiControllerArgs : public EventArgs
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
    SendMidiControllerArgs(int chan_hnd, int controller, int value)
    {
        this->chan_hnd = chan_hnd;
        this->controller = controller;
        this->value = value;
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


//----------------------------------------------------//
public ref class Interop : CliEx
{

//============= Cpp/CLI => interop C functions =============//
public:

    /// <summary>Setup</summary>
    /// <returns>Script return</returns>
    String^ Setup();

    /// <summary>Step</summary>
    /// <param name="tick">Current tick 0 => N</param>
    /// <returns>Script return</returns>
    int Step(int tick);

    /// <summary>ReceiveMidiNote</summary>
    /// <param name="chan_hnd">Input channel handle</param>
    /// <param name="note_num">Note number 0 => 127</param>
    /// <param name="volume">Volume 0.0 => 1.0</param>
    /// <returns>Script return</returns>
    int ReceiveMidiNote(int chan_hnd, int note_num, double volume);

    /// <summary>ReceiveMidiController</summary>
    /// <param name="chan_hnd">Input channel handle</param>
    /// <param name="controller">Specific controller id 0 => 127</param>
    /// <param name="value">Payload 0 => 127</param>
    /// <returns>Script return</returns>
    int ReceiveMidiController(int chan_hnd, int controller, int value);

//============= interop C => Cpp/CLI callback functions =============//
public:
    static event EventHandler<OpenMidiOutputArgs^>^ OpenMidiOutput;
    static void Notify(OpenMidiOutputArgs^ args) { OpenMidiOutput(nullptr, args); }

    static event EventHandler<OpenMidiInputArgs^>^ OpenMidiInput;
    static void Notify(OpenMidiInputArgs^ args) { OpenMidiInput(nullptr, args); }

    static event EventHandler<SendMidiNoteArgs^>^ SendMidiNote;
    static void Notify(SendMidiNoteArgs^ args) { SendMidiNote(nullptr, args); }

    static event EventHandler<SendMidiControllerArgs^>^ SendMidiController;
    static void Notify(SendMidiControllerArgs^ args) { SendMidiController(nullptr, args); }

    static event EventHandler<LogArgs^>^ Log;
    static void Notify(LogArgs^ args) { Log(nullptr, args); }

    static event EventHandler<SetTempoArgs^>^ SetTempo;
    static void Notify(SetTempoArgs^ args) { SetTempo(nullptr, args); }


//============= Infrastructure =============//
public:
    /// <summary>Initialize and execute script file.</summary>
    /// <param name="scriptFn">The script to load.</param>
    /// <param name="luaPath">LUA_PATH components</param>
    void RunScript(String^ scriptFn, String^ luaPath);

    /// <summary>Initialize and execute a chunk of lua code.</summary>
    /// <param name="code">The lua code to load.</param>
    /// <param name="name">Lua chunk name</param>
    /// <param name="luaPath">LUA_PATH components</param>
    void RunChunk(String^ code, String^ name, String^ luaPath);
};
