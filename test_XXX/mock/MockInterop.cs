

namespace Nebulua.Interop
{
    //#pragma warning disable CA1822 // Mark members as static

    public enum NebStatus
    {
        Ok, SyntaxError, FileError, RunError, AppInteropError, AppInternalError,
    }

    /// <summary>Mock app interop. See real class for doc.</summary>
    public class AppInterop
    {
        public string Error = "";

        public Dictionary<int, string> SectionInfo = [];

        public long Id { get { return _l; } }
        static long _l = 0;

        public AppInterop(List<string> lpath)
        {
            _l++;
        }

        public void Dispose()
        {
        }

        public NebStatus OpenScript(string fn)
        {
            // Fake contents.
            SectionInfo = new() { [0] = "start", [200] = "middle", [300] = "end", [400] = "LENGTH" };

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

        public string NebCommand(string cmd, string arg)
        {
            return "ending,512|middle,256|_LENGTH,768|beginning,0";
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

    #region Event args
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
        public string? Message;
    };

    public class PropertyArgs
    {
        public long Id;       // unique/opaque
        public int Ret;       // handler return value
        public int Bpm;
    };
    #endregion
}
