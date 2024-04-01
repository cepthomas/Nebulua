
//using Nebulua;

namespace Interop
{
#pragma warning disable CA1822 // Mark members as static

    /// <summary>Nebulua status. App errors start after internal lua errors so they can be handled consistently.</summary>
    public enum NebStatus
    {
        Ok = 0, InternalError = 10,
        BadCliArg = 11, BadLuaArg = 12, SyntaxError = 13, ApiError = 16, RunError = 17, FileError = 18,
        BadMidiCfg = 20, MidiTx = 21, MidiRx = 22
    };

    public class Api
    {
        /// <summary>If a function failed this contains info.</summary>
        public string Error = "";

        /// <summary>What's in the script.</summary>
        public Dictionary<int, string> SectionInfo = [];

        #region Lifecycle
        /// <summary>Prevent client instantiation.</summary>
        Api() { }

        /// <summary>
        /// Initialize everything.
        /// </summary>
        /// <returns>Status</returns>
        public NebStatus Init(List<string> lpath)
        {
            return NebStatus.Ok;
        }
        #endregion

        #region Singleton support
        /// <summary>The singleton instance.</summary>
        public static Api Instance
        {
            get
            {
                _instance ??= new Api();
                return _instance;
            }
        }

        /// <summary>The singleton instance.</summary>
        static Api? _instance;
        #endregion

        #region Run script - Call lua functions from host
        public NebStatus OpenScript(string fn)
        {
            return NebStatus.Ok;
        }

        public NebStatus Step(int tick)
        {
            return NebStatus.Ok;
        }

        public NebStatus InputNote(int chan_hnd, int note_num, double volume)
        {
            return NebStatus.Ok;
        }

        public NebStatus InputController(int chan_hnd, int controller, int value)
        {
            return NebStatus.Ok;
        }
        #endregion

        #region Events
        public event EventHandler<CreateChannelEventArgs>? CreateChannelEvent;
        public void NotifyCreateChannel(CreateChannelEventArgs args) { CreateChannelEvent?.Invoke(this, args); }

        public event EventHandler<SendEventArgs>? SendEvent;
        public void NotifySend(SendEventArgs args) { SendEvent?.Invoke(this, args); }

        public event EventHandler<LogEventArgs>? LogEvent;
        public void NotifyLogEvent(LogEventArgs args) { LogEvent?.Invoke(this, args); }

        public event EventHandler<ScriptEventArgs>? ScriptEvent;
        public void NotifyScriptEvent(ScriptEventArgs args) { ScriptEvent?.Invoke(this, args); }
        #endregion
    };

    #region Event args
    public class CreateChannelEventArgs : EventArgs
    {
        public string? DevName;
        public int ChanNum;
        public bool IsOutput; // else input
        public int Patch;     // output only
        public int Ret;       // handler return value
    };

    public class SendEventArgs : EventArgs
    {
        public int ChanHnd;
        public bool IsNote;   // else controller
        public int What;      // note number or controller id
        public int Value;     // note velocity or controller payload
        public int Ret;       // handler return value
    };

    public class LogEventArgs : EventArgs
    {
        public int LogLevel;
        public string? Msg;
        public int Ret;       // handler return value
    };

    public class ScriptEventArgs : EventArgs
    {
        public int Bpm;
        public int Ret;       // handler return value
    };
    #endregion
    
}
