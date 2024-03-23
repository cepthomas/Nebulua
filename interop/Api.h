#pragma once

using namespace System;
using namespace System::Collections::Generic;


namespace Interop
{

#pragma region Event args
    public ref class CreateChannelEventArgs : public EventArgs
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

    public ref class MiscInternalEventArgs : public EventArgs // TODO2 klunky name. make into separate events?
    {
    public:
        property int LogLevel;
        property int Bpm;
        property String^ Msg;
        property int Ret;       // handler return value
    };
#pragma endregion


    public ref class Api
    {
    public:
        /// <summary>If a function failed this contains info.</summary>
        property String^ Error;

        /// <summary>What's in the script.</summary>
        property Dictionary<int, String^>^ SectionInfo;

#pragma region Lifecycle
    public:
        /// <summary>Prevent client instantiation.</summary>
        Interop::Api() {}

        /// <summary>Initialize everything.</summary>
        /// <returns>Neb Status</returns>
        int Init();

        /// <summary>Clean up resources.</summary>
        ~Api();
#pragma endregion

#pragma region Singleton support
        /// <summary>The singleton instance.</summary>
        static property Interop::Api^ Instance
        {
            Interop::Api^ get()
            {
                if (_instance == nullptr) { _instance = gcnew Interop::Api(); }
                return _instance;
            }
    private:
            void set(Interop::Api^ e) { }
        }
    private:
        /// <summary>The singleton instance.</summary>
        static Interop::Api^ _instance;
#pragma endregion

#pragma region Run script - Call lua functions from host
    public:
        /// <summary>Load and process.</summary>
        /// <param name="fn">Full path</param>
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
        void NotifyCreateChannel(CreateChannelEventArgs^ args) { CreateChannelEvent(this, args); }

        event EventHandler<SendEventArgs^>^ SendEvent;
        void NotifySend(SendEventArgs^ args) { SendEvent(this, args); }

        event EventHandler<MiscInternalEventArgs^>^ MiscInternalEvent;
        void NotifyMiscInternal(MiscInternalEventArgs^ args) { MiscInternalEvent(this, args); }
#pragma endregion
    };
}
