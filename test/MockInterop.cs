
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

        public Api(List<string> lpath)
        {
            _l++;
        }

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
    };

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

    public class PropertyArgs : BaseArgs
    {
        public int Bpm;
    };

    public class NotifIer
    {
        /// <summary>The singleton instance.</summary>
        public static NotifIer? Instance { get; }

        NotifIer()
        {
        }

        public event EventHandler<CreateChannelArgs>? CreateChannel;
        public void NotifyCreateChannel(CreateChannelArgs args) { CreateChannel?.Invoke(this, args); }

        public event EventHandler<SendArgs>? Send;
        public void NotifySend(SendArgs args) { Send?.Invoke(this, args); }

        public event EventHandler<LogArgs>? Log;
        public void NotifyLog(LogArgs args) { Log?.Invoke(this, args); }

        public event EventHandler<PropertyArgs>? PropertyChange;
        public void NotifyPropertyChange(PropertyArgs args) { PropertyChange?.Invoke(this, args); }
    };
}
