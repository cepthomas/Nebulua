
local ut = require("utils")
local md = require("music_defs")

local M = {}


---------------------- types ----------------------

step_type = { NONE=0, NOTE=1, CONTROLLER=2, PATCH=3, FUNCTION=4 }

-- TODO1 class or?? Primary container for everything describing a step - mainly notes but supplementary stuff also.
-- step_info = {}
--     subbeat
--     step_type
--     channel_num
--     payload: STEP_TYPE_NOTE: note_num(I), volume(N), duration(I subbeats)
--              STEP_TYPE_CONTROLLER: ctlid(I), value(I)
--              STEP_TYPE_PATCH: patch_num(I)
--              STEP_TYPE_FUNCTION: function(F)
-- For viewing pleasure. ToString()

InternalPPQ = 32
-- Only 4/4 time supported.
BeatsPerBar = 4
SubbeatsPerBeat = InternalPPQ
SubeatsPerBar = InternalPPQ * BeatsPerBar


------------------------------- all ------------------------------

-----------------------------------------------------------------------------
-- Description
-- Description
-- @param name type desc
-- @return type desc
function M.process_all(sequences, sections)

    -- Return a list of step_info ordered by subbeat.

    seq_step_infos = []

    for seq_name, seq_steps in ipairs(sequences) do
        -- test types?

        step_infos = []



        for _, seq_step in ipairs(seq_steps) do
            bad_step = false

            if #seq_steps == 2 and type(seq_steps[1]) == "string" then

                gr_steps = parse_graphic_steps(seq_steps[1])
                if gr_steps is nil then
                    bad_step = true
                    log.error("input_note") -- string.format("%s", variable_name), channel_name, note, vel)
                else
                    table.insert(step_infos, gr_steps)



                t2 = type(seq_steps[2])
                if t2 == "string" then


                elseif t2 == "number" then

                elseif t2 == "function" then


                else
                    bad_step = true
                end



            elseif #seq_steps in [3, 4] then

            -- case num==2 seq_step[1]isstring

            else
                bad_step = true
            end




        end

        goto error

    end

    table.sort(seq_step_infos, function (left, right)
        return left[2] < right[2]
    end)

    return seq_step_infos

end

::error::


-----------------------------------------------------------------------------
-- Parse a pattern. Note subbeats per beat is fixed at PPQ of 8.
-- Like: "|M-------|--      |        |        |7-------|--      |        |5-8---  |"
-- @param pattern type Ascii pattern string.
-- @param what type Specific note(s).
-- @param volume type Base volume.
-- @return partially filled-in step_info[]
function M.parse_graphic_notes(gr_str)

    step_infos = []

    -- Remove visual markers.
    pattern = gr_str.Replace("|", "")

    current_vol = 0 -- default, not sounding
    start_offset = 0 -- in pattern for the start of the current event

    for i, #pattern do
        c = pattern[i]

        if c == '-' then
            -- Continue current note.
            if current_vol > 0 then
                -- ok, do nothing
            else
                -- invalid condition
                throw new InvalidOperationException("Invalid \'-\'' in pattern string");
            end
        elseif c >= '1' and c <= '9' then
            -- A new note starting.
            -- Do we need to end the current note?
            if current_vol > 0 then
                make_note_event(i - 1)
            end
            -- Start new note.
            current_vol = pattern[i] - '0'
            start_offset = i - 1
        elseif c == ' ' or c == '.' then
            -- No sound.
            -- Do we need to end the current note?
            if current_vol > 0 then
                make_note_event(i - 1)
            end
            current_vol = 0
        else
            -- Invalid char.
            throw new InvalidOperationException("Invalid char in pattern string [{pattern[i]}]")
        end
    end

    -- Local function to package an event.
    function make_note_event(offset)
        -- offset is 0-based.
        volmod = current_vol / 10
        dur = offset - start_offset
        when = start_offset

        si = { step_type=STEP_TYPE_NOTE, subbeat=when, note_num=src, volume=volmod, duration=dur }
        table.insert(step_infos, si)
    end

    -- Straggler?
    if current_vol > 0 then
        make_note_event(#pattern - 1)
    end


-----------------------------------------------------------------------------
-- Description
-- Description
-- @param name type desc
-- @return type desc
function parse_specific_notes(notes_src)
        -- [ 0.0, drum.AcousticBassDrum,  4, 0.1 ], --XIM(X)
        -- [ 0.4,  44,   5, 0.1 ], --XIM(X)
        -- [ 4.0, sequence_func,  7, 1.0 ], --XFM(X)
        -- [ 7.4, "A#2", 7, 0.1 ]  --XSM(X)

    -- Return partially filled-in info.
    -- TODO2 check numbers in midi range
    step_infos = []

    t = type(src)
    if t == "number" then
        -- use as is
        si = { step_type=STEP_TYPE_NOTE, subbeat=999, note_num=src, volume=-1, }
        table.insert(step_infos, si)
    elseif t == "function" then
        -- use as is
        si = { step_type=STEP_TYPE_FUNCTION, subbeat=999, function=src }
        table.insert(step_infos, si)
    elseif t == "string" then
        notes = md.get_notes(src)
        for n in notes do
            si = { step_type=STEP_TYPE_NOTE, subbeat=999, note_num=n, volume=-1, }
            table.insert(step_infos, si)
    else
        step_infos = nil
    end        
    return step_infos
end






-----------------------------------------------------------------------------
-- Description
-- Description
-- @param name type desc
-- @return type desc
function M.do_step(send_stuff, bar, beat, subbeat)
    -- calc total subbeat
    -- get all 


end



-- /// <summary>
-- /// Construct a BarTime from Beat.Subbeat representation as a double. Subbeat is LOW_RES_PPQ = 8
-- /// </summary>
-- /// <param name="beat"></param>
-- /// <returns>New BarTime.</returns>
-- public BarTime(double beat)
-- {
--     var (integral, fractional) = MathUtils.SplitDouble(beat);
--     var beats = (int)integral;
--     var subbeats = (int)Math.Round(fractional * 10.0);

--     if (subbeats >= LOW_RES_PPQ)
--     {
--         throw new Exception($"Invalid subbeat value: {beat}");
--     }

--     // Scale subbeats to native.
--     subbeats = subbeats * MidiSettings.LibSettings.InternalPPQ / LOW_RES_PPQ;
--     TotalSubbeats = beats * MidiSettings.LibSettings.SubbeatsPerBeat + subbeats;
-- }


-- public static (double integral, double fractional) SplitDouble(double val)
-- {
--     double integral = Math.Truncate(val);
--     double fractional = val - integral;
--     return (integral, fractional);
-- }


-- Return the module.
return M


--[[ old stuff TODO2
-- return table:
-- index = subbeat
-- value = msg_info[] to play
function M.process_sequence(seq)
    -- Length in beats.
    local seq_beats = 1
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

-- sect is a list of lists.
function M.process_section(sect)
-- Length in beats.
-- public string Name { get; set; } = "";
-- Collection of sequences in this section.
-- public SectionElements Elements { get; set; } = new SectionElements();
-- Length in beats.
-- public int Beats { get; set; } = 0;

-- For viewing pleasure. ToString()
--     return $"Section: Beats:{Beats} Name:{Name} Elements:{Elements.Count}";
--     return $"SectionElement: ChannelName:{ChannelName}";

]]