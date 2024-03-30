#pragma once

using namespace System;
using namespace System::Collections::Generic;


namespace Interop
{
    #pragma region Forward references.
    ref class CreateChannelEventArgs;
    ref class SendEventArgs;
    ref class ScriptEventArgs;
    ref class LogEventArgs;
    #pragma endregion

    public ref class Api
    {
    #pragma region Properties
    public:
        /// <summary>If an API or lua function failed this contains info.</summary>
        property String^ Error;

        /// <summary>What's in the script.</summary>
        property Dictionary<int, String^>^ SectionInfo;

        /// <summary>The singleton instance. TODO2 prefer non-singleton.</summary>
        static property Interop::Api^ Instance
        {
            Interop::Api^ get()
            {
                if (_instance == nullptr) { _instance = gcnew Interop::Api(); }
                return _instance;
            }
        }
    #pragma endregion

    #pragma region Lifecycle
    public:
        /// <summary>Initialize everything.</summary>
        /// <param name="lpath">LUA_PATH components</param>
        /// <returns>Neb Status</returns>
        int Init(List<String^>^ lpath);

    private:
        /// <summary>Prevent instantiation.</summary>
        /// <param name="lpath">LUA_PATH components</param>
        /// <returns>Neb Status</returns>
        Interop::Api();

        /// <summary>Clean up resources.</summary>
        ~Api();

        /// <summary>The singleton instance.</summary>
        static Interop::Api^ _instance;
    #pragma endregion

    #pragma region Run script - Call lua functions from host
    public:
        /// <summary>Load and process.</summary>
        /// <param name="fn">Full file path</param>
        /// <returns>Neb Status</returns>
        int OpenScript(String^ fn);

        /// <summary>Called every fast timer increment aka tick.</summary>
        /// <param name="tick">Current tick 0 => N</param>
        /// <returns>Neb Status</returns>
        int Step(int tick);

        /// <summary>Called when input arrives.</summary>
        /// <param name="chan_hnd">Input channel handle</param>
        /// <param name="note_num">Note number 0 => 127</param>
        /// <param name="volume">Volume 0.0 => 1.0</param>
        /// <returns>Neb Status</returns>
        int InputNote(int chan_hnd, int note_num, double volume);

        /// <summary>Called when input arrives.</summary>
        /// <param name="chan_hnd">Input channel handle</param>
        /// <param name="controller">Specific controller id 0 => 127</param>
        /// <param name="value">Payload 0 => 127</param>
        /// <returns>Neb Status</returns>
        int InputController(int chan_hnd, int controller, int value);
    #pragma endregion

    #pragma region Events
    public:
        event EventHandler<CreateChannelEventArgs^>^ CreateChannelEvent;
        void NotifyCreateChannelEvent(CreateChannelEventArgs^ args) { CreateChannelEvent(this, args); }

        event EventHandler<SendEventArgs^>^ SendEvent;
        void NotifySendEvent(SendEventArgs^ args) { SendEvent(this, args); }

        event EventHandler<LogEventArgs^>^ LogEvent;
        void NotifyLogEvent(LogEventArgs^ args) { LogEvent(this, args); }

        event EventHandler<ScriptEventArgs^>^ ScriptEvent;
        void NotifyScriptEvent(ScriptEventArgs^ args) { ScriptEvent(this, args); }
    #pragma endregion
    };

    #pragma region Event args
    /// <summary>Script creates a channel.</summary>
    public ref class CreateChannelEventArgs : public EventArgs
    {
    public:
        property String^ DevName;
        property int ChanNum;
        property bool IsOutput; // else input
        property int Patch;     // output only
        property int Ret;       // handler return value
    };

    /// <summary>Script wants to send a midi event.</summary>
    public ref class SendEventArgs : public EventArgs
    {
    public:
        property int ChanHnd;
        property bool IsNote;   // else controller
        property int What;      // note number or controller id
        property int Value;     // note velocity or controller payload
        property int Ret;       // handler return value
    };

    /// <summary>Script has something to say to host.</summary>
    public ref class ScriptEventArgs : public EventArgs
    {
    public:
        property int Bpm;       // Tempo - optional
        property int Ret;       // handler return value
    };

    /// <summary>Script wants to log something.</summary>
    public ref class LogEventArgs : public EventArgs
    {
    public:
        property int LogLevel;
        property String^ Msg;
        property int Ret;       // handler return value
    };
    #pragma endregion
}
