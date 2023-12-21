
TODO cherrypick settings then delete this.

public class MidiSettings
{
    public List<DeviceSpec> InputDevices { get; set; } = new();

    public List<DeviceSpec> OutputDevices { get; set; } = new();

    public int DefaultTempo { get; set; } = 100;

    public SnapType Snap { get; set; } = SnapType.Beat;

    /// <summary>Only 4/4 time supported.</summary>
    public int BeatsPerBar { get { return 4; } }

    public int SubbeatsPerBeat { get { return InternalPPQ; } }

    public int SubeatsPerBar { get { return InternalPPQ * BeatsPerBar; } }
}

public class DeviceSpec
{
    public string DeviceId { get; set; } = "";

    public string DeviceName { get; set; } = "";
}

public class Channel
{
    ///<summary>The collection of playable events for this channel and pattern. Key is the internal subbeat/time.</summary>
    readonly Dictionary<int, List<MidiEvent>> _events = new();

    /// <summary>Things that are executed once and disappear: NoteOffs, script send now. Key is the internal subbeat/time.</summary>
    readonly Dictionary<int, List<MidiEvent>> _transients = new();

    /// <summary>Actual 1-based midi channel number - required.</summary>
    public int ChannelNumber { get; set; } = -1;

    /// <summary>For muting/soloing.</summary>
    public ChannelState State { get; set; } = ChannelState.Normal;

    /// <summary>Current patch.</summary>
    public int Patch { get; set; } = -1;

    /// <summary>Current volume constrained to legal values.</summary>
    public double Volume

    /// <summary>Associated device.</summary>
    public IOutputDevice? Device { get; set; } = null;

    /// <summary>Add a ghost note off for note on.</summary>
    public bool AddNoteOff { get; set; } = false;

    /// <summary>Optional UI label/reference.</summary>
    public string ChannelName { get; set; } = "";

    /// <summary>Drums may be handled differently.</summary>
    public bool IsDrums { get; set; } = false;

    /// <summary>The device used by this channel. Used to find and bind the device at runtime.</summary>
    public string DeviceId { get; set; } = "";

    ///<summary>The duration of the whole channel - calculated.</summary>
    public int MaxSubbeat { get; private set; } = 0;

    /// <summary>Get the number of events - calculated.</summary>
    public int NumEvents { get { return _events.Count; } }
}

