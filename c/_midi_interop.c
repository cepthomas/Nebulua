
#include <windows.h>


eventToSend.GetAsShortMessage()
public virtual int GetAsShortMessage()
{
    return (channel - 1) + (int)commandCode; NoteOn etc
}
MmException.Try(MidiInterop.midiOutShortMsg(hMidiOut,message),"midiOutShortMsg");


// // IN:
// int num_in = midiInGetNumDevs();
// res = midiInGetDevCaps(dev_in, &caps_in, sizeof(caps_in));
// res = midiInOpen(&hmidi_in, dev_in, p_MidiInProc, 0, CALLBACK_FUNCTION);
// res = midiInStart(hmidi_in);
// res = midiInReset(hmidi_in);
// res = midiInStop(hmidi_in);
// res = midiInClose(hmidi_in);

// // OUT:
// int num_out = midiOutGetNumDevs();
// res = midiOutGetDevCaps(dev_out, &caps_out, sizeof(caps_out));
// res = midiOutOpen(&hmidi_out, dev_out, 0, 0, 0);
// int dwMsg = 0;
// res = midiOutShortMsg(hmidi_out, dwMsg);
// res = midiOutReset(hmidi_out);
// res = midiOutClose(hmidi_out);


///////// all useful MM api calls ///////
///////// all useful MM api calls ///////
///////// all useful MM api calls ///////

typedef struct midiincaps_tag {
  WORD    wMid;
  WORD    wPid;
  VERSION vDriverVersion;
  char    szPname[MAXPNAMELEN];
  DWORD   dwSupport;
} MIDIINCAPS, *PMIDIINCAPS, *NPMIDIINCAPS, *LPMIDIINCAPS;

typedef struct midioutcaps_tag {
  WORD    wMid;
  WORD    wPid;
  VERSION vDriverVersion;
  char    szPname[MAXPNAMELEN];
  WORD    wTechnology;
  WORD    wVoices;
  WORD    wNotes;
  WORD    wChannelMask;
  DWORD   dwSupport;
} MIDIOUTCAPS, *PMIDIOUTCAPS, *NPMIDIOUTCAPS, *LPMIDIOUTCAPS;


// http://msdn.microsoft.com/en-us/library/dd798452%28VS.85%29.aspx
MmResult midiInClose(IntPtr hMidiIn);

// http://msdn.microsoft.com/en-us/library/dd798453%28VS.85%29.aspx
MmResult midiInGetDevCaps(IntPtr deviceId, out MidiInCapabilities capabilities, int size);

// http://msdn.microsoft.com/en-us/library/dd798460%28VS.85%29.aspx
public delegate void MidiInCallback(IntPtr midiInHandle, MidiInMessage message, IntPtr userData, IntPtr messageParameter1, IntPtr messageParameter2);

// http://msdn.microsoft.com/en-us/library/dd798456%28VS.85%29.aspx
int midiInGetNumDevs();

// http://msdn.microsoft.com/en-us/library/dd798458%28VS.85%29.aspx
MmResult midiInOpen(out IntPtr hMidiIn, IntPtr uDeviceID, MidiInCallback callback, IntPtr dwInstance, int dwFlags);

// http://msdn.microsoft.com/en-us/library/dd798461%28VS.85%29.aspx
MmResult midiInReset(IntPtr hMidiIn);

// http://msdn.microsoft.com/en-us/library/dd798462%28VS.85%29.aspx
MmResult midiInStart(IntPtr hMidiIn);

// http://msdn.microsoft.com/en-us/library/dd798463%28VS.85%29.aspx
MmResult midiInStop(IntPtr hMidiIn);

// http://msdn.microsoft.com/en-us/library/dd798468%28VS.85%29.aspx
MmResult midiOutClose(IntPtr hMidiOut);

// http://msdn.microsoft.com/en-us/library/dd798469%28VS.85%29.aspx
MmResult midiOutGetDevCaps(IntPtr deviceNumber, out MidiOutCapabilities caps, int uSize);

// http://msdn.microsoft.com/en-us/library/dd798472%28VS.85%29.aspx
int midiOutGetNumDevs();

// http://msdn.microsoft.com/en-us/library/dd798475%28VS.85%29.aspx
MmResult midiOutMessage(IntPtr hMidiOut, int msg, IntPtr dw1, IntPtr dw2);

// http://msdn.microsoft.com/en-us/library/dd798476%28VS.85%29.aspx
MmResult midiOutOpen(out IntPtr lphMidiOut, IntPtr uDeviceID, MidiOutCallback dwCallback, IntPtr dwInstance, int dwFlags);

// http://msdn.microsoft.com/en-us/library/dd798479%28VS.85%29.aspx
MmResult midiOutReset(IntPtr hMidiOut);

// http://msdn.microsoft.com/en-us/library/dd798481%28VS.85%29.aspx
MmResult midiOutShortMsg(IntPtr hMidiOut, int dwMsg);



//////// Neb client stuff ///////
//////// Neb client stuff ///////
//////// Neb client stuff ///////

public static MidiInCapabilities DeviceInfo(int midiInDeviceNumber)
{
    MidiInCapabilities caps = new MidiInCapabilities();
    int structSize = Marshal.SizeOf(caps);
    MmException.Try(MidiInterop.midiInGetDevCaps((IntPtr)midiInDeviceNumber,out caps,structSize),"midiInGetDevCaps");
    return caps;
}


//////////////////////////////////////////////////////////////////
MidiIn _midiIn;
public event EventHandler<InputReceiveEventArgs>? InputReceive;

public MidiInput(string deviceName)
{
    DeviceName = deviceName;
    for (int i = 0; i < MidiIn.NumberOfDevices; i++)
    {
        if (deviceName == MidiIn.DeviceInfo(i).ProductName)
        {
            _midiIn = new MidiIn(i);
            _midiIn.MessageReceived += MidiIn_MessageReceived;
            _midiIn.ErrorReceived += MidiIn_ErrorReceived;
            break;
        }
    }
}

void MidiIn_MessageReceived(object? sender, MidiInMessageEventArgs e)
{
    MidiEvent me = MidiEvent.FromRawMessage(e.RawMessage);

    switch (me)
    {
        case NoteOnEvent evt:
            mevt = new InputReceiveEventArgs()
            {
                Channel = evt.Channel,
                Note = evt.NoteNumber,
                Value = evt.Velocity
            };
            break;

        case NoteEvent evt:
            mevt = new InputReceiveEventArgs()
            {
                Channel = evt.Channel,
                Note = evt.NoteNumber,
                Value = 0
            };
            break;

        case ControlChangeEvent evt:
            mevt = new InputReceiveEventArgs()
            {
                Channel = evt.Channel,
                Controller = (int)evt.Controller,
                Value = evt.ControllerValue
            };
            break;

        case PitchWheelChangeEvent evt:
            mevt = new InputReceiveEventArgs()
            {
                Channel = evt.Channel,
                Controller = InputReceiveEventArgs.PITCH_CONTROL,
                Value = evt.Pitch
            };
            break;

        default:
            // Ignore.
            break;
    }
    InputReceive.Invoke(this, mevt);
}

void MidiIn_ErrorReceived(object? sender, MidiInMessageEventArgs e)
{
    InputReceiveEventArgs evt = new()
    {
        ErrorInfo = $"Message:0x{e.RawMessage:X8}"
    };
    Log(evt);
}


//////////////////////////////////////////////////////////////////
MidiOut _midiOut;
public MidiOutput(string deviceName)
{
    DeviceName = deviceName;
    for (int i = 0; i < MidiOut.NumberOfDevices; i++)
    {
        if (deviceName == MidiOut.DeviceInfo(i).ProductName)
        {
            _midiOut = new MidiOut(i);
            break;
        }
    }
}
public void SendEvent(MidiEvent evt)
{
    _midiOut.Send(evt.GetAsShortMessage());
}


//////// Client stuff (from NAudio) ////////////
//////// Client stuff (from NAudio) ////////////
//////// Client stuff (from NAudio) ////////////

private IntPtr hMidiIn = IntPtr.Zero;
private MidiInterop.MidiInCallback callback;

public event EventHandler<MidiInMessageEventArgs> MessageReceived;
public event EventHandler<MidiInMessageEventArgs> ErrorReceived;

/// Gets the number of MIDI input devices available in the system
public static int MidiIn_NumberOfDevices
{
    return MidiInterop.midiInGetNumDevs();
}

/// Gets the MIDI in device info
public static MidiInCapabilities MidiIn_DeviceInfo(int midiInDeviceNumber)
{
    MidiInCapabilities caps = new MidiInCapabilities();
    int structSize = Marshal.SizeOf(caps);
    (MidiInterop.midiInGetDevCaps((IntPtr)midiInDeviceNumber,out caps,structSize),"midiInGetDevCaps");
    return caps;
}

/// Opens a specified MIDI in device
public MidiIn_MidiIn(int deviceNo)
{
    this.callback = new MidiInterop.MidiInCallback(Callback);
}

/// Closes this MIDI in device
public void MidiIn_Close() 
{
    MidiInterop.midiInClose(hMidiIn);
    disposed = true;
}

/// Start the MIDI in device
public void MidiIn_Start()
{
    (MidiInterop.midiInStart(hMidiIn), "midiInStart");
}

/// Stop the MIDI in device
public void MidiIn_Stop()
{
    (MidiInterop.midiInStop(hMidiIn), "midiInStop");
}

/// Reset the MIDI in device
public void MidiIn_Reset()
{
    (MidiInterop.midiInReset(hMidiIn), "midiInReset");
}

void MidiIn_Callback(IntPtr midiInHandle, MidiInterop.MidiInMessage message, IntPtr userData, IntPtr messageParameter1, IntPtr messageParameter2)
{
    switch(message)
    {
        case MidiInterop.MidiInMessage.Open:
            // message Parameter 1 & 2 are not used
            break;
        case MidiInterop.MidiInMessage.Data:
            // parameter 1 is packed MIDI message
            // parameter 2 is milliseconds since MidiInStart
            if (MessageReceived != null)
            {
                MessageReceived(this, new MidiInMessageEventArgs(messageParameter1.ToInt32(), messageParameter2.ToInt32()));
            }
            break;
        case MidiInterop.MidiInMessage.Error:
            // parameter 1 is invalid MIDI message
            if (ErrorReceived != null)
            {
                ErrorReceived(this, new MidiInMessageEventArgs(messageParameter1.ToInt32(), messageParameter2.ToInt32()));
            } 
            break;
        case MidiInterop.MidiInMessage.Close:
            // message Parameter 1 & 2 are not used
            break;
        case MidiInterop.MidiInMessage.LongData:
            // parameter 1 is pointer to MIDI header
            // parameter 2 is milliseconds since MidiInStart
            if (SysexMessageReceived != null)
            {
                MidiInterop.MIDIHDR hdr = (MidiInterop.MIDIHDR)Marshal.PtrToStructure(messageParameter1, typeof(MidiInterop.MIDIHDR));

                //  Copy the bytes received into an array so that the buffer is immediately available for re-use
                var sysexBytes = new byte[hdr.dwBytesRecorded];
                Marshal.Copy(hdr.lpData, sysexBytes, 0, hdr.dwBytesRecorded);

                SysexMessageReceived(this, new MidiInSysexMessageEventArgs(sysexBytes, messageParameter2.ToInt32()));
                //  Re-use the buffer - but not if we have no event handler registered as we are closing
                MidiInterop.midiInAddBuffer(hMidiIn, messageParameter1, Marshal.SizeOf(typeof(MidiInterop.MIDIHDR)));
            }
            break;
        case MidiInterop.MidiInMessage.LongError:
            // parameter 1 is pointer to MIDI header
            // parameter 2 is milliseconds since MidiInStart
            break;
        case MidiInterop.MidiInMessage.MoreData:
            // parameter 1 is packed MIDI message
            // parameter 2 is milliseconds since MidiInStart
            break;
    }
}

private IntPtr hMidiOut = IntPtr.Zero;

/// Gets the number of MIDI devices available in the system
public static int MidiOut_NumberOfDevices 
{
    return MidiInterop.midiOutGetNumDevs();
}

/// Gets the MIDI Out device info
public static MidiOutCapabilities MidiOut_DeviceInfo(int midiOutDeviceNumber)
{
    MidiOutCapabilities caps = new MidiOutCapabilities();
    int structSize = Marshal.SizeOf(caps);
    (MidiInterop.midiOutGetDevCaps((IntPtr)midiOutDeviceNumber, out caps, structSize), "midiOutGetDevCaps");
    return caps;
}

/// Opens a specified MIDI out device
public MidiOut_MidiOut(int deviceNo) 
{
    this.callback = new MidiInterop.MidiOutCallback(Callback);
    (MidiInterop.midiOutOpen(out hMidiOut, (IntPtr)deviceNo, callback, IntPtr.Zero, MidiInterop.CALLBACK_FUNCTION), "midiOutOpen");
}

/// Closes this MIDI out device
public void MidiOut_Close() 
{
    MidiInterop.midiOutClose(hMidiOut);
    disposed = true;
}

/// Gets or sets the volume for this MIDI out device
public int MidiOut_Volume 
{
    // TO-DO: Volume can be accessed by device ID
    get 
    {
        int volume = 0;
        (MidiInterop.midiOutGetVolume(hMidiOut,ref volume),"midiOutGetVolume");
        return volume;
    }
    set 
    {
        (MidiInterop.midiOutSetVolume(hMidiOut,value),"midiOutSetVolume");
    }
}

/// Resets the MIDI out device
public void MidiOut_Reset() 
{
    (MidiInterop.midiOutReset(hMidiOut),"midiOutReset");
}

/// Sends a MIDI out message
public void MidiOut_SendDriverMessage(int message, int param1, int param2) 
{
    (MidiInterop.midiOutMessage(hMidiOut,message,(IntPtr)param1,(IntPtr)param2),"midiOutMessage");
}

/// Sends a MIDI message to the MIDI out device
public void MidiOut_Send(int message) 
{
    (MidiInterop.midiOutShortMsg(hMidiOut,message),"midiOutShortMsg");
}

public enum MidiCommandCode : byte 
{
    /// <summary>Note Off</summary>
    NoteOff = 0x80,
    /// <summary>Note On</summary>
    NoteOn = 0x90,
    /// <summary>Key After-touch</summary>
    KeyAfterTouch = 0xA0,
    /// <summary>Control change</summary>
    ControlChange = 0xB0,
    /// <summary>Patch change</summary>
    PatchChange = 0xC0,
    /// <summary>Channel after-touch</summary>
    ChannelAfterTouch = 0xD0,
    /// <summary>Pitch wheel change</summary>
    PitchWheelChange = 0xE0,
    /// <summary>Sysex message</summary>
    Sysex = 0xF0,
    /// <summary>Eox (comes at end of a sysex message)</summary>
    Eox = 0xF7,
    /// <summary>Timing clock (used when synchronization is required)</summary>
    TimingClock = 0xF8,
    /// <summary>Start sequence</summary>
    StartSequence = 0xFA,
    /// <summary>Continue sequence</summary>
    ContinueSequence = 0xFB,
    /// <summary>Stop sequence</summary>
    StopSequence = 0xFC,
    /// <summary>Auto-Sensing</summary>
    AutoSensing = 0xFE,
    /// <summary>Meta-event</summary>
    MetaEvent = 0xFF,
}


///////////// probably reduncant ///////////
///////////// probably reduncant ///////////
///////////// probably reduncant ///////////

namespace NAudio.Midi
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto
    public struct MidiInCapabilities
    {
        /// <summary>
        /// wMid
        /// </summary>
        UInt16 manufacturerId; xxx
        /// <summary>
        /// wPid
        /// </summary>
        UInt16 productId; xxx
        /// <summary>
        /// vDriverVersion
        /// </summary>
        UInt32 driverVersion;
        /// <summary>
        /// Product Name
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxProductNameLength)]
        string productName; xxx
        /// <summary>
        /// Support - Reserved
        /// </summary>
        Int32 support;

        private const int MaxProductNameLength = 32;
    }

    /// MIDIOUTCAPS: http://msdn.microsoft.com/en-us/library/dd798467%28VS.85%29.aspx
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MidiOutCapabilities
    {
        Int16 manufacturerId;
        Int16 productId;
        int driverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxProductNameLength)]
        string productName;
        Int16 wTechnology;
        Int16 wVoices;
        Int16 wNotes;
        UInt16 wChannelMask;
        MidiOutCapabilityFlags dwSupport;

        const int MaxProductNameLength = 32;

        [Flags]
        enum MidiOutCapabilityFlags
        {
            Volume = 1,
            LeftRightVolume = 2,
            PatchCaching = 4,
            Stream = 8,
        }
    }



    internal class MidiInterop
    {
        public enum MidiInMessage
        {
            Open = 0x3C1,
            Close = 0x3C2,
            Data = 0x3C3,
            LongData = 0x3C4,
            Error = 0x3C5,
            LongError = 0x3C6,
            MoreData = 0x3CC,
        }

        public enum MidiOutMessage
        {
            Open = 0x3C7,
            Close = 0x3C8,
            Done = 0x3C9
        }


        // TO-DO: this is general MM interop
        public const int CALLBACK_FUNCTION = 0x30000;
        public const int CALLBACK_NULL = 0;

        // http://msdn.microsoft.com/en-us/library/dd757347%28VS.85%29.aspx
        // TO-DO: not sure this is right
        [StructLayout(LayoutKind.Sequential)]
        public struct MMTIME
        {
            public int wType;
            public int u;
        }

        // TO-DO: check for ANSI strings in these structs
        // TO-DO: check for WORD params
        [StructLayout(LayoutKind.Sequential)]
        public struct MIDIEVENT
        {
            public int dwDeltaTime;
            public int dwStreamID;
            public int dwEvent;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public int dwParms;
        }

        // http://msdn.microsoft.com/en-us/library/dd798449%28VS.85%29.aspx
        [StructLayout(LayoutKind.Sequential)]
        public struct MIDIHDR
        {
            public IntPtr lpData; // LPSTR
            public int dwBufferLength; // DWORD
            public int dwBytesRecorded; // DWORD
            public IntPtr dwUser; // DWORD_PTR
            public int dwFlags; // DWORD
            public IntPtr lpNext; // struct mididhdr_tag *
            public IntPtr reserved; // DWORD_PTR
            public int dwOffset; // DWORD
            // n.b. MSDN documentation incorrect, see mmsystem.h
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)] 
            public IntPtr[] dwReserved; // DWORD_PTR dwReserved[8]
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIDIPROPTEMPO
        {
            public int cbStruct;
            public int dwTempo;
        }

    public enum MmResult   /// Windows multimedia error codes from mmsystem.h.
    {
        /// <summary>no error, MMSYSERR_NOERROR</summary>
        NoError = 0,
        /// <summary>unspecified error, MMSYSERR_ERROR</summary>
        UnspecifiedError = 1,
        /// <summary>device ID out of range, MMSYSERR_BADDEVICEID</summary>
        BadDeviceId = 2,
        /// <summary>driver failed enable, MMSYSERR_NOTENABLED</summary>
        NotEnabled = 3,
        /// <summary>device already allocated, MMSYSERR_ALLOCATED</summary>
        AlreadyAllocated = 4,
        /// <summary>device handle is invalid, MMSYSERR_INVALHANDLE</summary>
        InvalidHandle = 5,
        /// <summary>no device driver present, MMSYSERR_NODRIVER</summary>
        NoDriver = 6,
        /// <summary>memory allocation error, MMSYSERR_NOMEM</summary>
        MemoryAllocationError = 7,
        /// <summary>function isn't supported, MMSYSERR_NOTSUPPORTED</summary>
        NotSupported = 8,
        /// <summary>error value out of range, MMSYSERR_BADERRNUM</summary>
        BadErrorNumber = 9,
        /// <summary>invalid flag passed, MMSYSERR_INVALFLAG</summary>
        InvalidFlag = 10,
        /// <summary>invalid parameter passed, MMSYSERR_INVALPARAM</summary>
        InvalidParameter = 11,
        /// <summary>handle being used simultaneously on another thread (eg callback),MMSYSERR_HANDLEBUSY</summary>
        HandleBusy = 12,
        /// <summary>specified alias not found, MMSYSERR_INVALIDALIAS</summary>
        InvalidAlias = 13,
        /// <summary>bad registry database, MMSYSERR_BADDB</summary>
        BadRegistryDatabase = 14,
        /// <summary>registry key not found, MMSYSERR_KEYNOTFOUND</summary>
        RegistryKeyNotFound = 15,
        /// <summary>registry read error, MMSYSERR_READERROR</summary>
        RegistryReadError = 16,
        /// <summary>registry write error, MMSYSERR_WRITEERROR</summary>
        RegistryWriteError = 17,
        /// <summary>registry delete error, MMSYSERR_DELETEERROR</summary>
        RegistryDeleteError = 18,
        /// <summary>registry value not found, MMSYSERR_VALNOTFOUND</summary>
        RegistryValueNotFound = 19,
        /// <summary>driver does not call DriverCallback, MMSYSERR_NODRIVERCB</summary>
        NoDriverCallback = 20,
        /// <summary>more data to be returned, MMSYSERR_MOREDATA</summary>
        MoreData = 21,

        /// <summary>unsupported wave format, WAVERR_BADFORMAT</summary>
        WaveBadFormat = 32,
        /// <summary>still something playing, WAVERR_STILLPLAYING</summary>
        WaveStillPlaying = 33,
        /// <summary>header not prepared, WAVERR_UNPREPARED</summary>
        WaveHeaderUnprepared = 34,
        /// <summary>device is synchronous, WAVERR_SYNC</summary>
        WaveSync = 35,

        // ACM error codes, found in msacm.h

        /// <summary>Conversion not possible (ACMERR_NOTPOSSIBLE)</summary>
        AcmNotPossible = 512,
        /// <summary>Busy (ACMERR_BUSY)</summary>
        AcmBusy = 513,
        /// <summary>Header Unprepared (ACMERR_UNPREPARED)</summary>
        AcmHeaderUnprepared = 514,
        /// <summary>Cancelled (ACMERR_CANCELED)</summary>
        AcmCancelled = 515,

        // Mixer error codes, found in mmresult.h

        /// <summary>invalid line (MIXERR_INVALLINE)</summary>
        MixerInvalidLine = 1024,
        /// <summary>invalid control (MIXERR_INVALCONTROL)</summary>
        MixerInvalidControl = 1025,
        /// <summary>invalid value (MIXERR_INVALVALUE)</summary>
        MixerInvalidValue = 1026,
    }

///// Yes:
        // http://msdn.microsoft.com/en-us/library/dd798452%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiInClose(IntPtr hMidiIn);

        // http://msdn.microsoft.com/en-us/library/dd798453%28VS.85%29.aspx
        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        public static extern MmResult midiInGetDevCaps(IntPtr deviceId, out MidiInCapabilities capabilities, int size);

        // http://msdn.microsoft.com/en-us/library/dd798460%28VS.85%29.aspx
        public delegate void MidiInCallback(IntPtr midiInHandle, MidiInMessage message, IntPtr userData, IntPtr messageParameter1, IntPtr messageParameter2);

        // http://msdn.microsoft.com/en-us/library/dd798456%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern int midiInGetNumDevs();

        // http://msdn.microsoft.com/en-us/library/dd798458%28VS.85%29.aspx
        [DllImport("winmm.dll", EntryPoint = "midiInOpen")]
        public static extern MmResult midiInOpen(out IntPtr hMidiIn, IntPtr uDeviceID, MidiInCallback callback, IntPtr dwInstance, int dwFlags);

        // http://msdn.microsoft.com/en-us/library/dd798461%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiInReset(IntPtr hMidiIn);

        // http://msdn.microsoft.com/en-us/library/dd798462%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiInStart(IntPtr hMidiIn);

        // http://msdn.microsoft.com/en-us/library/dd798463%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiInStop(IntPtr hMidiIn);

        // http://msdn.microsoft.com/en-us/library/dd798478%28VS.85%29.aspx
        public delegate void MidiOutCallback(IntPtr midiInHandle, MidiOutMessage message, IntPtr userData, IntPtr messageParameter1, IntPtr messageParameter2);

        // http://msdn.microsoft.com/en-us/library/dd798468%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutClose(IntPtr hMidiOut);

        // http://msdn.microsoft.com/en-us/library/dd798469%28VS.85%29.aspx
        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        public static extern MmResult midiOutGetDevCaps(IntPtr deviceNumber, out MidiOutCapabilities caps, int uSize);

        // http://msdn.microsoft.com/en-us/library/dd798472%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern int midiOutGetNumDevs();

        // http://msdn.microsoft.com/en-us/library/dd798475%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutMessage(IntPtr hMidiOut, int msg, IntPtr dw1, IntPtr dw2);

        // http://msdn.microsoft.com/en-us/library/dd798476%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutOpen(out IntPtr lphMidiOut, IntPtr uDeviceID, MidiOutCallback dwCallback, IntPtr dwInstance, int dwFlags);

        // http://msdn.microsoft.com/en-us/library/dd798479%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutReset(IntPtr hMidiOut);

        // http://msdn.microsoft.com/en-us/library/dd798481%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutShortMsg(IntPtr hMidiOut, int dwMsg);


///// No:

        // http://msdn.microsoft.com/en-us/library/dd798446%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiConnect(IntPtr hMidiIn, IntPtr hMidiOut, IntPtr pReserved);

        // http://msdn.microsoft.com/en-us/library/dd798447%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiDisconnect(IntPtr hMidiIn, IntPtr hMidiOut, IntPtr pReserved);

        // http://msdn.microsoft.com/en-us/library/dd798450%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiInAddBuffer(IntPtr hMidiIn, IntPtr lpMidiInHdr, int uSize);

        // http://msdn.microsoft.com/en-us/library/dd798454%28VS.85%29.aspx
        // TO-DO: review this, probably doesn't work
        [DllImport("winmm.dll")]
        public static extern MmResult midiInGetErrorText(int err, string lpText, int uSize);

        // http://msdn.microsoft.com/en-us/library/dd798455%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiInGetID(IntPtr hMidiIn, out int lpuDeviceId);

        // http://msdn.microsoft.com/en-us/library/dd798457%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiInMessage(IntPtr hMidiIn, int msg, IntPtr dw1, IntPtr dw2);

        // http://msdn.microsoft.com/en-us/library/dd798458%28VS.85%29.aspx
        [DllImport("winmm.dll", EntryPoint = "midiInOpen")]
        public static extern MmResult midiInOpenWindow(out IntPtr hMidiIn, IntPtr uDeviceID, IntPtr callbackWindowHandle, IntPtr dwInstance, int dwFlags);

        // http://msdn.microsoft.com/en-us/library/dd798459%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiInPrepareHeader(IntPtr hMidiIn, IntPtr lpMidiInHdr, int uSize);

        // http://msdn.microsoft.com/en-us/library/dd798464%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiInUnprepareHeader(IntPtr hMidiIn, IntPtr lpMidiInHdr, int uSize);

        // http://msdn.microsoft.com/en-us/library/dd798465%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutCacheDrumPatches(IntPtr hMidiOut, int uPatch, IntPtr lpKeyArray, int uFlags);

        // http://msdn.microsoft.com/en-us/library/dd798466%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutCachePatches(IntPtr hMidiOut, int uBank, IntPtr lpPatchArray, int uFlags);

        // http://msdn.microsoft.com/en-us/library/dd798470%28VS.85%29.aspx
        // TO-DO: review, probably doesn't work
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutGetErrorText(IntPtr err, string lpText, int uSize);

        // http://msdn.microsoft.com/en-us/library/dd798471%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutGetID(IntPtr hMidiOut, out int lpuDeviceID);

        // http://msdn.microsoft.com/en-us/library/dd798473%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutGetVolume(IntPtr uDeviceID, ref int lpdwVolume);

        // http://msdn.microsoft.com/en-us/library/dd798474%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutLongMsg(IntPtr hMidiOut, ref MIDIHDR lpMidiOutHdr, int uSize);

        // http://msdn.microsoft.com/en-us/library/dd798477%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutPrepareHeader(IntPtr hMidiOut, ref MIDIHDR lpMidiOutHdr, int uSize);

        // http://msdn.microsoft.com/en-us/library/dd798480%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutSetVolume(IntPtr hMidiOut, int dwVolume);

        // http://msdn.microsoft.com/en-us/library/dd798482%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutUnprepareHeader(IntPtr hMidiOut, ref MIDIHDR lpMidiOutHdr, int uSize);

        // http://msdn.microsoft.com/en-us/library/dd798485%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamClose(IntPtr hMidiStream);

        // http://msdn.microsoft.com/en-us/library/dd798486%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamOpen(out IntPtr hMidiStream, IntPtr puDeviceID, int cMidi, IntPtr dwCallback, IntPtr dwInstance, int fdwOpen);

        // http://msdn.microsoft.com/en-us/library/dd798487%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamOut(IntPtr hMidiStream, ref MIDIHDR pmh, int cbmh);

        // http://msdn.microsoft.com/en-us/library/dd798488%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamPause(IntPtr hMidiStream);

        // http://msdn.microsoft.com/en-us/library/dd798489%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamPosition(IntPtr hMidiStream, ref MMTIME lpmmt, int cbmmt);

        // http://msdn.microsoft.com/en-us/library/dd798490%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamProperty(IntPtr hMidiStream, IntPtr lppropdata, int dwProperty);

        // http://msdn.microsoft.com/en-us/library/dd798491%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamRestart(IntPtr hMidiStream);

        // http://msdn.microsoft.com/en-us/library/dd798492%28VS.85%29.aspx
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamStop(IntPtr hMidiStream);

    }
}
