

namespace Nebulua.Interop
{
#pragma warning disable CA1822 // Mark members as static

    public enum NebStatus
    {
        Ok, SyntaxError, FileError, RunError, ApiError, AppInternalError,
    }

    public class Api
    {
        public string Error = "";

        public Dictionary<int, string> SectionInfo = [];

        public long Id { get { return _l; } }
        static long _l = 0;

        public Api(List<string> lpath)
        {
            _l++;
        }

        public void Dispose()
        {
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

        public static event EventHandler<CreateChannelArgs>? CreateChannel;
        public static void NotifyCreateChannel(CreateChannelArgs args) { CreateChannel?.Invoke(null, args); }

        public static event EventHandler<SendArgs>? Send;
        public static void NotifySend(SendArgs args) { Send?.Invoke(null, args); }

        public static event EventHandler<LogArgs>? Log;
        public static void NotifyLog(LogArgs args) { Log?.Invoke(null, args); }

        public static event EventHandler<PropertyArgs>? PropertyChange;
        public static void NotifyPropertyChange(PropertyArgs args) { PropertyChange?.Invoke(null, args); }
    };

    public class CreateChannelArgs
    {
        public long Id;       // unique/opaque
        public int Ret;       // handler return value
        public string? DevName;
        public int ChanNum;
        public bool IsOutput; // else input
        public int Patch;     // output only
    };

    public class SendArgs
    {
        public long Id;       // unique/opaque
        public int Ret;       // handler return value
        public int ChanHnd;
        public bool IsNote;   // else controller
        public int What;      // note number or controller id
        public int Value;     // note velocity or controller payload
    };

    public class LogArgs
    {
        public long Id;       // unique/opaque
        public int Ret;       // handler return value
        public int LogLevel;
        public string? Msg;
    };

    public class PropertyArgs
    {
        public long Id;       // unique/opaque
        public int Ret;       // handler return value
        public int Bpm;
    };
}
