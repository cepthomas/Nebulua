#pragma once

using namespace System;
using namespace System::Collections::Generic;


namespace Interop
{
    /// <summary>Nebulua status. App errors start after internal lua errors so they can be handled consistently.</summary>
    public enum class NebStatus
    {
        Ok = 0, InternalError = 10,
        BadCliArg = 11, BadLuaArg = 12, SyntaxError = 13, ApiError = 16, RunError = 17, FileError = 18,
        BadMidiCfg = 20, MidiTx = 21, MidiRx = 22
    };

    public ref class Api
    {
    #pragma region Properties
    public:
        /// <summary>If an API or lua function failed this contains info.</summary>
        property String^ Error;

        /// <summary>What's in the script.</summary>
        property Dictionary<int, String^>^ SectionInfo;

        // /// <summary>The singleton instance. TODO2 prefer non-singleton.</summary>
        // static property Interop::Api^ Instance
        // {
        //     Interop::Api^ get()
        //     {
        //         if (_instance == nullptr) { _instance = gcnew Interop::Api(); }
        //         return _instance;
        //     }
        // }
    #pragma endregion

    #pragma region Lifecycle
    public:
        /// <summary>Initialize everything.</summary>
        /// <param name="lpath">LUA_PATH components</param>
        /// <returns>Neb Status</returns>
    //    NebStatus Init(List<String^>^ lpath);

 //   private:
        /// <summary>Prevent instantiation.</summary>
        /// <param name="lpath">LUA_PATH components</param>
        Api(List<String^>^ lpath);

        /// <summary>Clean up resources.</summary>
        ~Api();

        /// <summary>The singleton instance.</summary>
     //   static Interop::Api^ _instance;

        // The lua thread.
        lua_State* _l = nullptr;

        // The LUA_PATH.
        List<String^>^ _lpath;

    #pragma endregion

    #pragma region Run script - Call lua functions from host
    public:
        /// <summary>Load and process.</summary>
        /// <param name="fn">Full file path</param>
        /// <returns>Neb Status</returns>
        NebStatus OpenScript(String^ fn);

        /// <summary>Called every fast timer increment aka tick.</summary>
        /// <param name="tick">Current tick 0 => N</param>
        /// <returns>Neb Status</returns>
        NebStatus Step(int tick);

        /// <summary>Called when input arrives.</summary>
        /// <param name="chan_hnd">Input channel handle</param>
        /// <param name="note_num">Note number 0 => 127</param>
        /// <param name="volume">Volume 0.0 => 1.0</param>
        /// <returns>Neb Status</returns>
        NebStatus InputNote(int chan_hnd, int note_num, double volume);

        /// <summary>Called when input arrives.</summary>
        /// <param name="chan_hnd">Input channel handle</param>
        /// <param name="controller">Specific controller id 0 => 127</param>
        /// <param name="value">Payload 0 => 127</param>
        /// <returns>Neb Status</returns>
        NebStatus InputController(int chan_hnd, int controller, int value);
    #pragma endregion

    // #pragma region Events
    // public:
    //     event EventHandler<CreateChannelEventArgs^>^ CreateChannelEvent;
    //     void NotifyCreateChannelEvent(CreateChannelEventArgs^ args) { CreateChannelEvent(this, args); }

    //     event EventHandler<SendEventArgs^>^ SendEvent;
    //     void NotifySendEvent(SendEventArgs^ args) { SendEvent(this, args); }

    //     event EventHandler<LogEventArgs^>^ LogEvent;
    //     void NotifyLogEvent(LogEventArgs^ args) { LogEvent(this, args); }

    //     event EventHandler<ScriptEventArgs^>^ ScriptEvent;
    //     void NotifyScriptEvent(ScriptEventArgs^ args) { ScriptEvent(this, args); }
    // #pragma endregion

    #pragma region Private functions
    private:
        /// <summary>Checks lua status and converts to neb status. Stores an error message if it failed.</summary>
        NebStatus EvalLuaStatus(int stat, String^ msg);

        /// <summary>Convert managed string to unmanaged.</summary>
        const char* ToCString(String^ input);

        /// <summary>Convert unmanaged string to managed.</summary>
        String^ ToCliString(const char* input);
    #pragma endregion
    };

    #pragma region Events

    /// <summary>Base event.</summary>
    public ref class BaseEventArgs : public EventArgs
    {
    public:
        property lua_State* l;
        property int Ret;       // handler return value
    };

    /// <summary>Script creates a channel.</summary>
    public ref class CreateChannelEventArgs : public BaseEventArgs
    {
    public:
        property String^ DevName;
        property int ChanNum;
        property bool IsOutput; // else input
        property int Patch;     // output only
    };

    /// <summary>Script wants to send a midi event.</summary>
    public ref class SendEventArgs : public BaseEventArgs
    {
    public:
        property int ChanHnd;
        property bool IsNote;   // else controller
        property int What;      // note number or controller id
        property int Value;     // note velocity or controller payload
    };

    /// <summary>Script has something to say to host.</summary>
    public ref class ScriptEventArgs : public BaseEventArgs
    {
    public:
        property int Bpm;       // Tempo - optional
    };

    /// <summary>Script wants to log something.</summary>
    public ref class LogEventArgs : public BaseEventArgs
    {
    public:
        property int LogLevel;
        property String^ Msg;
    };

    public ref class EventProc
    {
    #pragma region Properties
    public:
        // /// <summary>If an API or lua function failed this contains info.</summary>
        // property String^ Error;

        // /// <summary>What's in the script.</summary>
        // property Dictionary<int, String^>^ SectionInfo;

        /// <summary>The singleton instance. TODO2 prefer non-singleton.</summary>
        static property Interop::EventProc^ Instance
        {
            Interop::EventProc^ get()
            {
                if (_instance == nullptr) { _instance = gcnew Interop::EventProc(); }
                return _instance;
            }
        }
    #pragma endregion

    #pragma region Lifecycle
    public:
        /// <summary>Initialize everything.</summary>
        /// <param name="lpath">LUA_PATH components</param>
        /// <returns>Neb Status</returns>
        // NebStatus Init(List<String^>^ lpath);

    private:
        /// <summary>Prevent instantiation.</summary>
        /// <param name="lpath">LUA_PATH components</param>
 //       Interop::EventProc();

        /// <summary>Clean up resources.</summary>
 //       ~EventProc();

        /// <summary>The singleton instance.</summary>
        static Interop::EventProc^ _instance;

        // The main lua thread.
     //   lua_State* _l = nullptr;
    #pragma endregion


    #pragma region Event hooks
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

    // #pragma region Private functions
    // private:
    //     /// <summary>Checks lua status and converts to neb status. Stores an error message if it failed.</summary>
    //     NebStatus EvalLuaStatus(int stat, String^ msg);

    //     /// <summary>Convert managed string to unmanaged.</summary>
    //     const char* ToCString(String^ input);

    //     /// <summary>Convert unmanaged string to managed.</summary>
    //     String^ ToCliString(const char* input);
    // #pragma endregion
    };

    #pragma endregion
}
