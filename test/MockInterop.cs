
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

        /// <summary>Unique opaque id.</summary>
        public long Id { get { return _l; } }
        static long _l = 0;

        #region Lifecycle
        public Api(List<string> lpath)
        {
            _l++;
        }

        ///// <summary>
        ///// Initialize everything.
        ///// </summary>
        ///// <returns>Status</returns>
        //public NebStatus Init(List<string> lpath)
        //{
        //    return NebStatus.Ok;
        //}
        #endregion

        //#region Singleton support
        ///// <summary>The singleton instance.</summary>
        //public static Api Instance
        //{
        //    get
        //    {
        //        _instance ??= new Api();
        //        return _instance;
        //    }
        //}

        ///// <summary>The singleton instance.</summary>
        //static Api? _instance;
        //#endregion

        #region Run script - Call lua functions from host
        public NebStatus OpenScript(string fn)
        {
            return NebStatus.Ok;
        }

        public NebStatus Step(int tick)
        {
            return NebStatus.Ok;
        }

        public NebStatus RcvNote(int chan_hnd, int note_num, double volume)
        {
            return NebStatus.Ok;
        }

        public NebStatus RcvController(int chan_hnd, int controller, int value)
        {
            return NebStatus.Ok;
        }
        #endregion

        //#region Events
        //public event EventHandler<CreateChannelArgs>? CreateChannel;
        //public void NotifyCreateChannel(CreateChannelArgs args) { CreateChannel?.Invoke(this, args); }

        //public event EventHandler<SendArgs>? Send;
        //public void NotifySend(SendArgs args) { Send?.Invoke(this, args); }

        //public event EventHandler<LogArgs>? Log;
        //public void NotifyLog(LogArgs args) { Log?.Invoke(this, args); }

        //public event EventHandler<ScriptArgs>? PropertyChange;
        //public void NotifyPropertyChange(ScriptArgs args) { PropertyChange?.Invoke(this, args); }
        //#endregion
    };

    #region Event args

    public class BaseArgs : EventArgs
    {
        public long Id;       // unique/opaque
        public int Ret;       // handler return value
    };

    public class CreateChannelArgs : BaseArgs
    {
        public string? DevName;
        public int ChanNum;
        public bool IsOutput; // else input
        public int Patch;     // output only
    };

    public class SendArgs : BaseArgs
    {
        public int ChanHnd;
        public bool IsNote;   // else controller
        public int What;      // note number or controller id
        public int Value;     // note velocity or controller payload
    };

    public class LogArgs : BaseArgs
    {
        public int LogLevel;
        public string? Msg;
    };

    public class ScriptArgs : BaseArgs
    {
        public int Bpm;
    };
    #endregion


    public class NotifIer
    {
        /// <summary>The singleton instance.</summary>
        public static NotifIer? Instance
        {
            get;
            //get
            //{
            //    if (_instance == null) { _instance = new(); }
            //    return _instance;
            //}
        }

        public NotifIer()
        {

        }

        /// <summary>Clean up resources.</summary>
        //~NotifIer();

        /// <summary>The singleton instance.</summary>
        static NotifIer? _instance;

        public event EventHandler<CreateChannelArgs>? CreateChannel;
        public void NotifyCreateChannel(CreateChannelArgs args) { CreateChannel?.Invoke(this, args); }

        public event EventHandler<SendArgs>? Send;
        public void NotifySend(SendArgs args) { Send?.Invoke(this, args); }

        public event EventHandler<LogArgs>? Log;
        public void NotifyLog(LogArgs args) { Log?.Invoke(this, args); }

        public event EventHandler<ScriptArgs>? PropertyChange;
        public void NotifyPropertyChange(ScriptArgs args) { PropertyChange?.Invoke(this, args); }
    };





}
