
-- Class family for midi things to send.

local ut = require("utils")
require('class')

STEP_TYPE = { NONE = 0, NOTE = 1, CONTROLLER = 2, FUNCTION = 3 }

-- base
StepInfo = class(
    function(a, subbeats, chan_hnd)
        a.type = STEP_TYPE.NONE
        a.subbeats = subbeats
        a.chan_hnd = chan_hnd
    end)

function StepInfo:__tostring()
    -- ex: interp( [[Hello {name}, welcome to {company}.]], { name = name, company = get_company_name() } )
    return self.subbeats..' '..self.chan_hnd..': '..self:format()
    -- return self.name..': '..self:speak()
end


-- derived
StepNote = class(StepInfo,
    function(c, subbeats, chan_hnd, note_num, velocity)
        StepInfo.init(c, subbeats, chan_hnd) -- init base
        c.type = STEP_TYPE.NOTE
        c.note_num = note_num
        c.velocity = velocity
    end)

function StepNote:format()
    return 'NOTE'
end


-- derived
StepController = class(StepInfo,
    function(c, subbeats, chan_hnd, controller, value)
        StepInfo.init(c, subbeats, chan_hnd) -- init base
        c.type = STEP_TYPE.CONTROLLER
        c.controller = controller
        c.value = value
    end)

function StepFunction:format()
    return 'CONTROLLER'
end


-- derived
StepFunction = class(StepInfo,
    function(c, subbeats, chan_hnd, func)
        StepInfo.init(c, subbeats, chan_hnd) -- init base
        c.type = STEP_TYPE.FUNCTION
        c.func = func
    end)

function StepFunction:format()
    return 'FUNCTION'
end


--[[
fields?
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
]]
