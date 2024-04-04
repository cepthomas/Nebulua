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

    #pragma region Fields
    private:
        // The lua thread.
        lua_State* _l = nullptr;

        // The LUA_PATH.
        List<String^>^ _lpath;
    #pragma endregion

    #pragma region Properties
    public:
        /// <summary>If an API or lua function failed this contains info.</summary>
        property String^ Error;

        /// <summary>What's in the script.</summary>
        property Dictionary<int, String^>^ SectionInfo;

        /// <summary>Unique opaque id.</summary>
        property long Id { long get() { return (long)_l; }}
    #pragma endregion

    #pragma region Lifecycle
    public:
        /// <summary>Initialize everything.</summary>
        /// <param name="lpath">LUA_PATH components</param>
        Api(List<String^>^ lpath);

        /// <summary>Clean up resources.</summary>
        ~Api();
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
        NebStatus RcvNote(int chan_hnd, int note_num, double volume);

        /// <summary>Called when input arrives.</summary>
        /// <param name="chan_hnd">Input channel handle</param>
        /// <param name="controller">Specific controller id 0 => 127</param>
        /// <param name="value">Payload 0 => 127</param>
        /// <returns>Neb Status</returns>
        NebStatus RcvController(int chan_hnd, int controller, int value);
    #pragma endregion

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

    #pragma region Script callbacks (events)
    /// <summary>Common elements.</summary>
    public ref class BaseArgs : public EventArgs
    {
    public:
        property long Id;       // unique/opaque
        property int Ret;       // handler return value
    };

    /// <summary>Script creates a channel.</summary>
    public ref class CreateChannelArgs : public BaseArgs
    {
    public:
        property String^ DevName;
        property int ChanNum;
        property bool IsOutput; // else input
        property int Patch;     // output only
    };

    /// <summary>Script wants to send a midi event.</summary>
    public ref class SendArgs : public BaseArgs
    {
    public:
        property int ChanHnd;
        property bool IsNote;   // else controller
        property int What;      // note number or controller id
        property int Value;     // note velocity or controller payload
    };

    /// <summary>Script has something to say to host.</summary>
    public ref class PropertyArgs : public BaseArgs
    {
    public:
        property int Bpm;       // Tempo - optional
    };

    /// <summary>Script wants to log something.</summary>
    public ref class LogArgs : public BaseArgs
    {
    public:
        property int LogLevel;
        property String^ Msg;
    };

    public ref class NotifIer
    {
    #pragma region Properties
    public:
        /// <summary>The singleton instance. TODO1 prefer non-singleton.</summary>
        static property Interop::NotifIer^ Instance
        {
            Interop::NotifIer^ get()
            {
                if (_instance == nullptr) { _instance = gcnew Interop::NotifIer(); }
                return _instance;
            }
        }
    #pragma endregion

    #pragma region Lifecycle
    private:
        /// <summary>Prevent direct instantiation.</summary>
        /// <param name="lpath">LUA_PATH components</param>
        Interop::NotifIer() { }

        /// <summary>The singleton instance.</summary>
        static Interop::NotifIer^ _instance;
    #pragma endregion

    #pragma region Event hooks
    public:
        event EventHandler<CreateChannelArgs^>^ CreateChannel;
        void NotifyCreateChannel(CreateChannelArgs^ args) { CreateChannel(this, args); }

        event EventHandler<SendArgs^>^ Send;
        void NotifySend(SendArgs^ args) { Send(this, args); }

        event EventHandler<LogArgs^>^ Log;
        void NotifyLog(LogArgs^ args) { Log(this, args); }

        event EventHandler<PropertyArgs^>^ PropertyChange;
        void NotifyPropertyChange(PropertyArgs^ args) { PropertyChange(this, args); }
    #pragma endregion
    };
    #pragma endregion
}
