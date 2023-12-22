

-- TODO3 midi event class may be useful.

-- public class Channel
-- {
--     ///<summary>The collection of playable events for this channel and pattern. Key is the internal subbeat/time.</summary>
--     readonly Dictionary<int, List<MidiEvent>> _events = new();

--     /// <summary>Things that are executed once and disappear: NoteOffs, script send now. Key is the internal subbeat/time.</summary>
--     readonly Dictionary<int, List<MidiEvent>> _transients = new();

--     /// <summary>Actual 1-based midi channel number - required.</summary>
--     public int ChannelNumber { get; set; } = -1;

--     /// <summary>For muting/soloing.</summary>
--     public ChannelState State { get; set; } = ChannelState.Normal;

--     /// <summary>Current patch.</summary>
--     public int Patch { get; set; } = -1;

--     /// <summary>Current volume constrained to legal values.</summary>
--     public double Volume

--     /// <summary>Associated device.</summary>
--     public IOutputDevice? Device { get; set; } = null;

--     /// <summary>Add a ghost note off for note on.</summary>
--     public bool AddNoteOff { get; set; } = false;

--     /// <summary>Optional UI label/reference.</summary>
--     public string ChannelName { get; set; } = "";

--     /// <summary>Drums may be handled differently.</summary>
--     public bool IsDrums { get; set; } = false;

--     /// <summary>The device used by this channel. Used to find and bind the device at runtime.</summary>
--     public string DeviceId { get; set; } = "";

--     ///<summary>The duration of the whole channel - calculated.</summary>
--     public int MaxSubbeat { get; private set; } = 0;

--     /// <summary>Get the number of events - calculated.</summary>
--     public int NumEvents { get { return _events.Count; } }
-- }


local ut = require("utils")
require('class')

STEP_TYPE = { NONE = 0, NOTE = 1, CONTROLLER = 2, PATCH = 3, FUNCTION = 4 }

-- base
StepInfo = class(
    function(a, subbeat, channel_num)
        a.type = STEP_TYPE.NONE
        a.subbeat = subbeat
        a.channel_num = channel_num
    end)

function StepInfo:__tostring()

-- ex: interp( [[Hello {name}, welcome to {company}.]], { name = name, company = get_company_name() } )

    return self.subbeat..' '..self.channel_num..': '..self:format()
    -- return self.name..': '..self:speak()
end

-- derived
StepNote = class(StepInfo,
    function(c, subbeat, channel_num)
        StepInfo.init(c, subbeat, channel_num) -- must init base!
        c.type = STEP_TYPE.NOTE
        -- notenum(I), volume(N), duration(I subbeats)
    end)

function StepNote:format()
    return 'NOTE'
end


-- StepController   STEP_TYPE.CONTROLLER: ctlid(I), value(I)
-- StepPatch        STEP_TYPE.PATCH: patch_num(I)
-- StepFunction     STEP_TYPE.FUNCTION: function(F)

