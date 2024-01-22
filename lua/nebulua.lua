
--- Core generic functions for this app. Matches/requires the C libs.


local M = {}


--- Log levels - must match those in the host C code.
M.LOG_LEVEL = { NONE = 0, DEBUG = 1, INFO = 2, ERROR = 3 }

--- Convenience functions.
function M.error(msg) api.log(M.LOG_LEVEL.ERROR, msg) end
function M.info(msg)  api.log(M.LOG_LEVEL.INFO, msg) end
function M.debug(msg) api.log(M.LOG_LEVEL.DEBUG, msg) end



-- WHAT_TO_PLAY is a string or integer or function.
-- STEP_TYPE = { NONE = 0, NOTE = 1, CONTROLLER = 2, PATCH = 3, FUNCTION = 4 }
-- SB_REL_ (subbeats) is integer when to play relative
-- VOL_VEL volume or velocity. 0


-- index is sequencename, value is steps list.
local _seq_step_infos =
{
    graphical_seq = 
    {
        SB_REL_1 =
        {
            { WHAT_TO_PLAY, VOL_VEL },
            { WHAT_TO_PLAY, VOL_VEL },
            { WHAT_TO_PLAY, VOL_VEL },
            { WHAT_TO_PLAY, VOL_VEL },
        },
        SB_REL_2 =
        {
            { WHAT_TO_PLAY, VOL/VEL },
            -- ...
        },
        -- ...
    }
}

    graphical_seq = -- these are 8 beats long - end with WHAT_TO_PLAY.
    list_seq = -- these are terminator beats long - seq[2] is WHAT_TO_PLAY.

-- sections =
-- {
--     sectionname =
--     {
--         { chanhandle, sequence,     nil,          function,     ... },
--     },
-- }

w = {x=0, y=0, label="console"}

-- list_seq = -- these are terminator beats long - seq[2] is WHAT_TO_PLAY.
-- {
--     { 0.0, "C2",    7, 0.1 },
--     { 0.0,  bdrum,  4, 0.1 },
--     { 0.4,  44,     5, 0.1 },
--     { 4.0,  func1,  7, 1.0 },
--     { 7.4, "A#2",   7, 0.1 },
--     { 8.0, "",      0, 0.0 }   -- terminator -> length
-- },




-- index is section name, value is list of sequence names.
local _sections = {}


-----------------------------------------------------------------------------
--- Process all sequences into discrete steps. Sections are stored as is.
-- @param sequences table user sequence specs
-- @param sections table user section specs
-- @return list of step_info ordered by subbeat
function M.process_all(sequences, sections)

    -- Process sections.

    for seq_name, seq_steps in ipairs(sequences) do
        -- test types?

        local step_infos = {}

        for _, seq_step in ipairs(seq_steps) do
            local gr_steps = nil
            -- Make a guess.
            if #seq_steps == 2 then
                gr_steps = parse_graphic_steps(seq_steps)
            elseif #seq_steps >= 3 then
                gr_steps = parse_explicit_notes(seq_steps)
            end

            if gr_steps == nil then
                log.error("input_note") -- string.format("%s", variable_name), channel_name, note, vel)
            else
                step_infos[seq_name] = gr_steps
            end

            M.seq_step_infos.insert()
        end
    end

    table.sort(seq_step_infos, function (left, right) return left.subbeat < right.subbeat end)

    -- Process sections.
    -- sections =
    -- {
    --     sectionname =
    --     {
    --         { chanhandle, sequence,     nil,          function,     ... },
    --     },
    -- }
    _sections = sections

    -- return seq_step_infos
end


-----------------------------------------------------------------------------
--- Parse a pattern.
-- @param notes_src like: { "|M-------|--      |        |        |7-------|--      |        |        |", "G4.m7" }
-- @return partially filled-in step_info list
function parse_graphic_notes(notes_src)

    -- { "|        |        |        |5---    |        |        |        |5-8---  |", "D6" },
    -- { "|M-------|--      |        |        |7-------|--      |        |        |", "G4.m7" },
    -- { "|7-------|--      |        |        |7-------|--      |        |        |", 84 },
    -- { "|7-------|--      |        |        |7-------|--      |        |        |", drum.AcousticSnare },
    -- { "|        |        |        |5---    |        |        |        |5-8---  |", sequence_func }

    local step_infos = { }

    local note = notes_src[2]
    local tnote = type(notes_src[2])
    local notes = {}
    local func = nil

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
    local pattern = notes_src[1].Replace("|", "")

    local current_vol = 0 -- default, not sounding
    local start_offset = 0 -- in pattern for the start of the current event

    for i = 1, #pattern do
        local c = pattern[i]

        if c == '-' then
            -- Continue current note.
            if current_vol > 0 then
                -- ok, do nothing
            else
                -- invalid condition
                -- throw new InvalidOperationException("Invalid \'-\'' in pattern string");
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
            -- throw new InvalidOperationException("Invalid char in pattern string [{pattern[i]}]")
        end
    end

    -- Straggler?
    if current_vol > 0 then
        make_note_event(#pattern - 1)
    end

    -- Local function to package an event.
    function make_note_event(offset)
        -- offset is 0-based.
        local volmod = current_vol / 10
        local dur = offset - start_offset
        local when = start_offset
        local si = nil

        if func then
            si = { step_type=STEP_TYPE.FUNCTION, subbeat=when, function=func, volume=volmod, duration=dur }
        else
            si = { step_type=STEP_TYPE.NOTE, subbeat=when, notenum=src, volume=volmod, duration=dur }
        end
        table.insert(step_infos, si)
    end
end

-----------------------------------------------------------------------------
--- Description
-- @param notes_src like: { 0.4, 44, 5, 0.4 }
-- @return partially filled-in type_info list
function parse_explicit_notes(notes_src)

    -- { 0.0, drum.AcousticBassDrum,  4, 0.1 },
    -- { 0.4, 44,                     5, 0.4 },
    -- { 7.4, "A#min",                7, 1.2 },
    -- { 4.0, sequence_func,          7      },

    local step_infos = {}

    local start = to_subbeats(notes_src[1])
    local note = notes_src[2]
    local tnote = type(notes_src[2])
    local volume = notes_src[3]
    local duration = notes_src[4] or 0.1
    local si = nil

    if tnote == "number" then
        -- use as is
        si = { step_type=STEP_TYPE.NOTE, subbeat=start, notenum=src, volume=volume / 10 }
        table.insert(step_infos, si)
    elseif tnote == "function" then
        -- use as is
        si = { step_type=STEP_TYPE.FUNCTION, subbeat=start, function=src, volume=volume / 10 }
        table.insert(step_infos, si)
    elseif tnote == "string" then
        local notes = md.get_notes(src)
        for n in notes do
            si = { step_type=STEP_TYPE.NOTE, subbeat=start, notenum=n, volume=volume / 10 }
            table.insert(step_infos, si)
    else
        step_infos = nil
    end        
    return step_infos
end

-----------------------------------------------------------------------------
--- Process notes at this tick.
-- @param name type desc
-- @return type desc
function M.do_step(send_stuff, bar, beat, subbeat)
    -- calc total subbeat
    -- get all 
    -- return status?


end


-----------------------------------------------------------------------------
--- Construct a subbeat from beat.subbeat representation as a double.
-- @param d number value to convert
-- @return type desc
function M.to_subbeats(dbeat)

    local integral = math.truncate(dbeat)
    local fractional = dbeat - integral
    local beats = (int)integral
    local subbeats = (int)math.round(fractional * 10.0)

    if (subbeats >= LOW_RES_PPQ)
        --throw new Exception($"Invalid subbeat value: {beat}")
    end

    -- Scale subbeats to native.
    subbeats = subbeats * INTERNAL_PPQ / LOW_RES_PPQ
    total_subbeats = beats * SUBBEATS_PER_BEAT + subbeats
end


-- Return the module.
return M
