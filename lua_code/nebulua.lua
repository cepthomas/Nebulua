
-- Core generic functions for this app. Matches/requires the C libs.


local st = require("step_types")


--[[

Glossary
    int chan_hnd (device id << 8 | chan_num)
    int controller (name - midi_defs)
    int value (controller payload)
    int note_num (0-127)
    double volume (0.0-1.0) 0 means note off
    int velocity (0-127) 0 means note off
    int bar (absolute)
    int beat (in bar)
    int subbeat (in beat)
    int subbeats (absolute/total - in sequence/section/composition)

Script defs:
   BAR is 0->N, BEAT is 0->neb.BEATS_PER_BAR-1, SUBBEAT is 0->neb.SUBBEATS_PER_BEAT-1
   WHAT_TO_PLAY is a string (see neb.get_notes_from_string(s)) or integer or function.
   BAR_TIME is a string of "BAR.BEAT.SUBBEAT" e.g. "1.2.3" or "1.2" or "1".
   VOLUME 0->9

Internal collections
    steps: index is sequence name, value is table of Step.
    steps =
    {
        sequence_name_1 =
        {
            subbeat1 = { StepX, StepX, ... },
            subbeat2 = { StepX, StepX, ... },
            ...
        },
        sequence_name_2 =
        {
            subbeat3 = { StepX, StepX, ... },
            subbeat4 = { StepX, StepX, ... },
            ...
        },
        ...
    }

    sections: index is section name, value is table of...
    sections =
    {
        section_name_1 =
        {
            { chan_hnd = { sequence_name_1,  sequence_name_2, ... },
            ...
        },
        ...
    }

    -- example.lua
    sections =
    {
        beginning =
        {
            { hkeys,  keys_verse,  keys_verse,  keys_verse,  keys_verse },
            { hdrums, drums_verse, drums_verse, drums_verse, drums_verse },
            { hbass,  bass_verse,  bass_verse,  bass_verse,  bass_verse }
        }
    }

]]

local M = {}

-- Misc defs - matches C host.
M.SUBBEATS_PER_BEAT 8
M.BEATS_PER_BAR     4
M.LOG_LEVEL = { NONE = 0, TRACE = 1, DEBUG = 2, INFO = 3, ERROR = 4 }


--- Convenience functions.
function M.error(msg) api.log(M.LOG_LEVEL.ERROR, msg) end
function M.info(msg)  api.log(M.LOG_LEVEL.INFO, msg) end
function M.debug(msg) api.log(M.LOG_LEVEL.DEBUG, msg) end
function M.trace(msg) api.log(M.LOG_LEVEL.TRACE, msg) end


-----------------------------------------------------------------------------
--- Report a syntax error.
-- @param info
local function syntax_error(desc, info)
    log.error(string.format("Syntax error: %s %s", desc, info))
end


-----------------------------------------------------------------------------
--- Process all sequences into discrete steps. Sections are stored as is.
-- @param sequences table user sequence specs
-- @param sections table user section specs
-- @return list of step_info ordered by subbeat
function M.process_all(sequences, sections)

    -------------- Process sequences.
    -- sequences =
    -- {
    --     sequence_name_1 =
    --     { seq_chunks
    --         -- |........|........|........|........|........|........|........|........|
    --         { "|        |        |        |        |        |        |        |        |", "ABC" }, one seq_chunk
    --         { "|        |        |        |        |        |        |        |        |", "ABC" },
    --     },
    -- }
    -- 
    -- ==>
    -- 
    -- steps =
    -- {
    --     sequence_name_1 =
    --     {
    --         subbeat1 = { StepX, StepX, ... },
    --         subbeat2 = { StepX, StepX, ... },
    --         ...
    --     },
    --     sequence_name_2 =
    --     {
    --         subbeat3 = { StepX, StepX, ... },
    --         subbeat4 = { StepX, StepX, ... },
    --         ...
    --     },
    --     ...
    -- }


    for seq_name, seq_chunks in ipairs(sequences) do
        -- test types?

        local steps = {}

        for _, seq_chunk in ipairs(seq_chunks) do
            local gr_steps = nil
            -- Make a guess.
            if #seq_chunk == 2 then    -- { "|  ...  |", "ABC" }
                gr_steps = parse_chunk(seq_chunk)
            else
                syntax_error("Invalid chunk", seq_chunk)
            end

            if gr_steps == nil then
                syntax_error("Couldn't parse chunk", seq_chunk)
            else
                steps[seq_name] = gr_steps
            end

            M.seq_steps.insert()
        end
    end

    table.sort(seq_steps, function (left, right) return left.subbeat < right.subbeat end)


    -------------- Process sections.
    _sections = sections

    -- sections =
    -- {
    --     beginning =
    --     {
    --         { hkeys,  keys_verse,  keys_verse,  keys_verse,  keys_verse },
    --         { hdrums, drums_verse, drums_verse, drums_verse, drums_verse },
    --         { hbass,  bass_verse,  bass_verse,  bass_verse,  bass_verse }
    --     },
    --     ...
    -- }
    -- 
    -- ==>
    -- 
    -- sections =
    -- {
    --     beginning =
    --     {
    --         { hkeys = { keys_verse,  keys_verse,  keys_verse,  keys_verse },
    --         ...
    --     },
    --     ...
    -- }

end


-----------------------------------------------------------------------------
--- Parse a chunk pattern.
-- @param chunk like: { "|M-------|--      |        |        |7-------|--      |        |        |", "G4.m7" }
-- @return list of Steps partially filled-in.
local function parse_chunk(chunk)
    local steps = { }

    -- Process the note descriptor first.
    local what_to_play = chunk[2]
    local tn = type(what_to_play)
    local notes = {}
    local func = nil

    if tn == "number" then
        -- use as is
        notes = { what_to_play }
    elseif tn == "function" then
        -- use as is
        func = what_to_play
    elseif tn == "string" then
        notes = md.get_notes(what_to_play)
    else
        syntax_error("hoo", "haa")
    end        

    -- Process note instances.
    -- Remove visual markers from pattern.
    local pattern = chunk[1].Replace("|", "")

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
                syntax_error("Invalid \'-\'' in pattern string")
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
            syntax_error("Invalid char in pattern string", c)
        end
    end

    -- Straggler?
    if current_vol > 0 then
        make_note_event(#pattern - 1)
    end

    -- Local function to package an event. chan_hnd is not know now and will get plugged in later.
    function make_note_event(offset)
        -- offset is 0-based.
        local volmod = current_vol / 10
        local dur = offset - start_offset
        local when = start_offset
        local si = nil

        if func then
            si = StepFunction(when, 0, func)
            -- { step_type=STEP_TYPE.FUNCTION, subbeat=when, function=func, volume=volmod, duration=dur }
        else
            si = StepNote(when, 0, note_num, dur)
            -- { step_type=STEP_TYPE.NOTE, subbeat=when, note_num=src, volume=volmod, duration=dur }
        end
        table.insert(steps, si)
    end
end


-----------------------------------------------------------------------------
--- Process notes at this time.
-- @param name type desc
-- @return type desc
function M.do_step(send_stuff, bar, beat, subbeat) -- TODO1
    -- calc total subbeat
    -- get all 
    -- return status?


end
