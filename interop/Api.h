#pragma once

using namespace System;
using namespace System::Collections::Generic;


namespace Interop
{
    ref class CreateChannelEventArgs;
    ref class SendEventArgs;
    ref class ScriptEventArgs;
    ref class LogEventArgs;

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
            //private:
            //        void set(Interop::Api^ e) { }
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

        /// <summary>Initialize everything.</summary>
        /// <param name="lpath">LUA_PATH components</param>
        /// <returns>Neb Status</returns>
        //Interop::Api(List<String^>^ lpath);

    private:
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

    private:
        // The main_lua thread. This pointless struct decl makes a warning go away per https://github.com/openssl/openssl/issues/6166.
       // struct lua_State {};
       // lua_State* _l;

        // Translate between internal LUA_XXX status and client facing NEB_XXX status.
        int MapStatus(int lua_status);

        // Arg check.
        int DoCheck();
    };

#pragma region Event args
    public ref class CreateChannelEventArgs : public EventArgs//TODO1 put these below or somewhere else...
    {
    public:
        property String^ DevName;
        property int ChanNum;
        property bool IsOutput; // else input
        property int Patch;     // output only
        property int Ret;       // handler return value
    };

    public ref class SendEventArgs : public EventArgs
    {
    public:
        property int ChanHnd;
        property bool IsNote;   // else controller
        property int What;      // note number or controller id
        property int Value;     // note velocity or controller payload
        property int Ret;       // handler return value
    };

    public ref class ScriptEventArgs : public EventArgs
    {
    public:
        property int Bpm;
        property int Ret;       // handler return value
    };

    public ref class LogEventArgs : public EventArgs
    {
    public:
        property int LogLevel;
        property String^ Msg;
        property int Ret;       // handler return value
    };
#pragma endregion
}
