
#include <windows.h>



// :startup
// - get in/out numdevs and caps
// - open output device(s)
// - open input device(s) -> callback(s)
// - midin(s) start


// :shutdown



// :rcv msg


void doit()
{

int deviceNo = 1;

MmResult midiInOpen(out IntPtr hMidiIn, IntPtr uDeviceID, MidiInCallback callback, IntPtr dwInstance, int dwFlags);
MMRESULT res = midiInOpen(
  LPHMIDIIN phmi,
  UINT      uDeviceID,
  DWORD_PTR dwCallback,
  DWORD_PTR dwInstance,
  DWORD     fdwOpen
);






}






/* all useful MM api calls:

enum MmResult
{
    /// <summary>no error, MMSYSERR_NOERROR</summary>
    NoError = 0,
    /// <summary>unspecified error, MMSYSERR_ERROR</summary>
    UnspecifiedError = 1,
    // etc...
}


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
MMRESULT midiInOpen(
  LPHMIDIIN phmi,
  UINT      uDeviceID,
  DWORD_PTR dwCallback,
  DWORD_PTR dwInstance,
  DWORD     fdwOpen
);

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

*/


/* Client stuff (from cs):

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
    (MidiInterop.midiInOpen(out hMidiIn, (IntPtr) deviceNo,this.callback,IntPtr.Zero,MidiInterop.CALLBACK_FUNCTION),"midiInOpen");
MMRESULT midiInOpen(
  LPHMIDIIN phmi,
  UINT      uDeviceID,
  DWORD_PTR dwCallback,
  DWORD_PTR dwInstance,
  DWORD     fdwOpen
);
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
    // TODO: Volume can be accessed by device ID
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
*/