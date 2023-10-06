
local ut = require("utils")
local md = require("music_defs")

local M = {}


---------------------- types ----------------------

STEP_TYPE = { NONE=0, NOTE=1, CONTROLLER=2, PATCH=3, FUNCTION=4 }

-- TODO1 class or?? Primary container for everything describing a step - mainly notes but supplementary stuff also.
-- step_info = {}
--     subbeat
--     STEP_TYPE
--     channel_num
--     payload: STEP_TYPE.NOTE: note_num(I), volume(N), duration(I subbeats)
--              STEP_TYPE.CONTROLLER: ctlid(I), value(I)
--              STEP_TYPE.PATCH: patch_num(I)
--              STEP_TYPE.FUNCTION: function(F)
-- For viewing pleasure. ToString()

internal_ppq = 32
-- Only 4/4 time supported.
beats_per_bar = 4
subbeats_per_beat = internal_ppq
subeats_per_bar = internal_ppq * beats_per_bar
-- subbeat is low_res_ppq
low_res_ppq = 8

------------------------------- all ------------------------------

-----------------------------------------------------------------------------
-- Description
-- Description
-- @param name type desc
-- @return list of step_info ordered by subbeat
function M.process_all(sequences, sections)
    seq_step_infos = {}

    for seq_name, seq_steps in ipairs(sequences) do
        -- test types?

        step_infos = {}

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
-- Parse a pattern.
-- @param notes_src list like: [ "|M-------|--      |        |        |7-------|--      |        |        |", "G4.m7" ]
-- @return partially filled-in step_info list
function M.parse_graphic_notes(notes_src)
    -- TODO2 check args, numbers in midi range

    -- [ "|        |        |        |5---    |        |        |        |5-8---  |", "D6" ] --SS
    -- [ "|M-------|--      |        |        |7-------|--      |        |        |", "G4.m7" ], --SS
    -- [ "|7-------|--      |        |        |7-------|--      |        |        |", 84 ], --SI
    -- [ "|7-------|--      |        |        |7-------|--      |        |        |", drum.AcousticSnare ], --SI
    -- [ "|        |        |        |5---    |        |        |        |5-8---  |", sequence_func ] --SF

    step_infos = {}

    note = notes_src[2]
    tnote = type(notes_src[2])
    notes = {}
    func = nil

    if tnote == "number" then
        -- use as is
        table.insert(notes, note)
    elseif tnote == "function" then
        -- use as is
        func = note
    elseif tnote == "string" then
        notes = md.get_notes(src)
    else
        step_infos = nil
    end        

    -- Remove visual markers from pattern.
    pattern = notes_src[1].Replace("|", "")

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

    -- Straggler?
    if current_vol > 0 then
        make_note_event(#pattern - 1)
    end

    -- Local function to package an event.
    function make_note_event(offset)
        -- offset is 0-based.
        volmod = current_vol / 10
        dur = offset - start_offset
        when = start_offset

        if func is not nil then
            si = { step_type=STEP_TYPE.NOTE, subbeat=when, note_num=src, volume=volmod, duration=dur }
        else
            si = { step_type=STEP_TYPE.FUNCTION, subbeat=when, function=func, volume=volmod, duration=dur }
        end
        table.insert(step_infos, si)
    end
end

-----------------------------------------------------------------------------
-- Description
-- Description
-- @param name type desc
-- @return partially filled-in type_info list
function parse_explicit_notes(notes_src)
    -- [ 0.0, drum.AcousticBassDrum,  4, 0.1 ], --XIM(X)
    -- [ 0.4, 44,                     5, 0.4 ], --XIM(X)
    -- [ 7.4, "A#min",                7, 1.2 ]  --XSM(X)
    -- [ 4.0, sequence_func,          7      ], --XFM(X)

    -- TODO2 check args, numbers in midi range
    step_infos = {}

    start = to_subbeats(notes_src[1])
    note = notes_src[2]
    tnote = type(notes_src[2])
    volume = notes_src[3]
    duration = notes_src[4] or 0.1

    if tnote == "number" then
        -- use as is
        si = { step_type=STEP_TYPE.NOTE, subbeat=start, note_num=src, volume=volume / 10 }
        table.insert(step_infos, si)
    elseif tnote == "function" then
        -- use as is
        si = { step_type=STEP_TYPE.FUNCTION, subbeat=start, function=src, volume=volume / 10 }
        table.insert(step_infos, si)
    elseif tnote == "string" then
        notes = md.get_notes(src)
        for n in notes do
            si = { step_type=STEP_TYPE.NOTE, subbeat=start, note_num=n, volume=volume / 10 }
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
function M.do_step(send_stuff, bar, beat, subbeat) -- TODO1
    -- calc total subbeat
    -- get all 


end


-----------------------------------------------------------------------------
-- Construct a BarTime from Beat.Subbeat representation as a double. Subbeat is low_res_ppq = 8
-- @param d number value to convert
-- @return type desc
function M.to_subbeats(dbeat)

    integral = math.truncate(dbeat)
    fractional = dbeat - integral

    beats = (int)integral
    subbeats = (int)math.round(fractional * 10.0)

    if (subbeats >= low_res_ppq)
        --throw new Exception($"Invalid subbeat value: {beat}")
    end

    -- Scale subbeats to native.
    subbeats = subbeats * internal_ppq / low_res_ppq
    total_subbeats = beats * subbeats_per_beat + subbeats


-- public static (double integral, double fractional) SplitDouble(double val)
-- {
--     return (integral, fractional);
-- }

end



-- /// <summary>
-- /// 
-- /// </summary>
-- /// <param name="beat"></param>
-- /// <returns>New BarTime.</returns>
-- public BarTime(double beat)
-- {
-- }




-- Return the module.
return M


--[[ old stuff TODO2
-- return table:
-- index = subbeat
-- value = msg_info list to play
function M.process_sequence(seq)
    -- Length in beats.
    local seq_beats = 1
    -- All notes in an instrument sequence.
    local elements = {}
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
        elem.notes = {} -- ints
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