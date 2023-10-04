
local ut = require("utils")

local M = {}



--     msg_info (all I):
--         msg_type: note, ctlr, patch, function
--         channel_num
--         dur in subbeats (opt)
--         payload1: note_num or ctlid or patchnum
--         payload2: velocity or ctlvalue

-- For viewing pleasure. ToString()


-- return table:
-- index = subbeat
-- value = msg_info[] to play
function M.process_sequence(seq)

    -- Length in beats.
    local seq_beats = 1;


    -- All notes in an instrument sequence.
    local elements = []

    -- Parse seq string.
    local seq_name = "???"
    local seq_lines = ut.strsplit("\n", seq)

    for i = 1, #seq_lines do
        local seq_line = seq_lines[i]
        -- One note or chord or function etc in the sequence. Essentially something that gets played.
        local elem = {}


        -- process line here

        -- public void Add(string pattern, string what, double volume)

        -- Notes = MusicDefinitions.GetNotesFromString(s);
        -- if(Notes.Count == 0)
        -- {
        --     // It might be a drum.
        --     try
        --     {
        --         int idrum = MidiDefs.GetDrumNumber(s);
        --         Notes.Add(idrum);
        --     }
        --     catch { }
        -- }

        -- Individual note volume.
        elem.vol = 0.8
        -- When to play in Sequence. BarTime?
        elem.when = 3.3
        -- Time between note on/off. 0 (default) means not used. BarTime?
        elem.dur = 1.4
        -- The 0th is the root note and other values comprise possible chord notes.
        elem.notes = [] -- ints
        -- or call a script function.
        elem.func = nil

        -- Store.
        table.insert(elements, elem)
    end

    -- sequences[seq_name] = elements

    -- Return sequence info.
    return { elements = elements, seq_beats = seq_beats }
}


-- For viewing pleasure. ToString()
--     return $"Sequence: Beats:{Beats} Elements:{Elements.Count}";
--     return $"SequenceElement: When:{When} NoteNum:{Notes[0]:F2} Volume:{Volume:F2} Duration:{Duration}";

end


------------------------------- section ------------------------------


-- sect is a list of lists.
function M.process_section(sect)

    -- middle = [
    --     [ keys,    keys_chorus,  keys_chorus,  keys_chorus,  keys_chorus ],
    --     [ drums,   drums_chorus, drums_chorus, drums_chorus, drums_chorus ],
    --     [ bass,    bass_chorus,  bass_chorus,  bass_chorus,  bass_chorus ],
    --     [ synth,   algo_func,    nil,          algo_func,    dynamic ]
    -- ]

-- Length in beats.
-- public string Name { get; set; } = "";
-- Collection of sequences in this section.
-- public SectionElements Elements { get; set; } = new SectionElements();
-- Length in beats.
-- public int Beats { get; set; } = 0;


-- For viewing pleasure. ToString()
--     return $"Section: Beats:{Beats} Name:{Name} Elements:{Elements.Count}";
--     return $"SectionElement: ChannelName:{ChannelName}";

end

------------------------------- all ------------------------------


function M.process_all(sequences, sections)

    -- Return a table with:
    -- table indexed by subbeat. fields: 
    -- function do_step(bar, beat, subbeat)

    -- public void BuildSteps()
    -- public Dictionary<int, string> GetSectionMarkers()
    -- public IEnumerable<MidiEventDesc> GetEvents()
    -- List<MidiEventDesc> ConvertToEvents(Channel channel, Sequence seq, int startBeat)

end


function M.do_step(send_stuff, bar, beat, subbeat)
    -- calc total subbeat
    -- get all 


end


-- Return the module.
return M

