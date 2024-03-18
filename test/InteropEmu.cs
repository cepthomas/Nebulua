
namespace Interop
{
    #region Event args
    public class CreateChannelEventArgs : EventArgs
    {
        public string DevName;
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

    public class MiscInternalEventArgs : EventArgs
    {
        public int LogLevel;
        public int Bpm;
        public string Msg;
        public int Ret;       // handler return value
    };
    #endregion


    public class Api
    {
        /// <summary>If a function failed this contains info.</summary>
        public string Error;

        /// <summary>What's in the script.</summary>
        public Dictionary<int, string> SectionInfo;

        #region Lifecycle
        /// <summary>
        /// Initialize everything.
        /// </summary>
        /// <returns>Status</returns>
        public int Init()
        {
            return 0;
        }

        /// <summary>
        /// Clean up resources.
        /// </summary>
        ~Api()
        {
        }
        #endregion

        #region Singleton support
        /// <summary>The singleton instance.</summary>
        public static Api Instance 
        {
            get
            {
                if (_instance is null) { _instance = new Api(); }
                return _instance;
            }
        }
        /// <summary>The singleton instance.</summary>
        static Api _instance;

        /// <summary>Prevent client instantiation.</summary>
        Api() { }
        #endregion

        #region Run script - Call lua functions from host
        /// <summary>
        /// Load and process.
        /// </summary>
        /// <param name="fn">Full path</param>
        /// <returns>Standard status</returns>
        public int OpenScript(string fn)
        {
            return 0;
        }

        /// <summary>
        /// Called every fast timer increment aka tick.
        /// </summary>
        /// <param name="tick">Current tick 0 => N</param>
        /// <param name="ret">Script function return - unused</param>
        /// <returns>Fail = true</returns>
        public bool Step(int tick)
        {
            return false;
        }

        /// <summary>
        /// Called when input arrives.
        /// </summary>
        /// <param name="chan_hnd">Input channel handle</param>
        /// <param name="note_num">Note number 0 => 127</param>
        /// <param name="volume">Volume 0.0 => 1.0</param>
        /// <param name="ret">Script function return - unused</param>
        /// <returns>Fail = true</returns>
        public bool InputNote(int chan_hnd, int note_num, double volume)
        {
            return false;
        }

        /// <summary>
        /// Called when input arrives.
        /// </summary>
        /// <param name="chan_hnd">Input channel handle</param>
        /// <param name="controller">Specific controller id 0 => 127</param>
        /// <param name="value">Payload 0 => 127</param>
        /// <param name="ret">Script function return - unused</param>
        /// <returns>Fail = true</returns>
        public bool InputController(int chan_hnd, int controller, int value)
        {
            return false;
        }
        #endregion

        #region Events
        public event EventHandler<CreateChannelEventArgs> CreateChannelEvent;
        void RaiseCreateChannel(CreateChannelEventArgs args) { CreateChannelEvent(this, args); }

        public event EventHandler<SendEventArgs> SendEvent;
        void RaiseSend(SendEventArgs args) { SendEvent(this, args); }

        public event EventHandler<MiscInternalEventArgs> MiscInternalEvent;
        void RaiseMiscInternal(MiscInternalEventArgs args) { MiscInternalEvent(this, args); }
        #endregion
    };
}
