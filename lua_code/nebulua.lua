
-- Core generic functions for this app. Matches/requires the C libs.
-- Hides the C api so this is one stop shopping for the user script.

local api = require("host_api") -- C api (or sim)
local st = require("step_types")
require('neb_common')


-- {
--     sequence_1 =
--     {
--         sub_1 = { StepX, StepX, ... },
--         sub_2 = { StepX, StepX, ... },
--         ...
--     },
--     ...
-- }
local _seq_steps = {}


-- {
--     section_1 =
--     {
--         { chan_hnd = { sequence_1,  sequence_2, ... },
--         { chan_hnd = { sequence_1,  sequence_2, ... },
--         ...
--     },
--     ...
-- }
local _sections = {}

tempdbg = { steps = _seq_steps, sections = _sections }

local M = {}

-----------------------------------------------------------------------------
-- Impedance matching between C and Lua.

-- Log functions. Magic numbers from C code.
function M.error(msg) api.log(4, msg) end
function M.info(msg)  api.log(3, msg) end
function M.debug(msg) api.log(2, msg) end
function M.trace(msg) api.log(1, msg) end

-- These go straight through so just thunk the C api.
M.create_input_channel = api.create_input_channel
M.create_output_channel = api.create_output_channel
M.set_tempo = api.set_tempo
M.send_controller = api.send_controller

-- TODO1 intercept and handle chasing note offs
function M.send_note(chan_hnd, note_num, volume, dur)
    -- "If volume is 0 note_off else note_on. If dur is 0 send note_on with dur = 1 (for drum/hit).",
    api.send_note(chan_hnd, note_num, volume)
end
-----------------------------------------------------------------------------


-----------------------------------------------------------------------------
--- Report a syntax error.
-- @param info
local function syntax_error(desc, info)
    M.error(string.format("Syntax error: %s %s", desc, info))
end


-----------------------------------------------------------------------------
--- Process all sequences into discrete steps. Sections are stored as is.
-- @param sequences table user sequence specs
-- @param sections table user section specs
-- @return list of step_info ordered by sub
function M.process_all(sequences, sections)

    for seq_name, seq_chunks in ipairs(sequences) do
        -- test types?

        local steps = {}

        for _, seq_chunk in ipairs(seq_chunks) do

    -- example_seq =
    -- {
    --     -- | beat 1 | beat 2 |........|........|........|........|........|........|,  WHAT_TO_PLAY
    --     { "|5-------|--      |        |        |7-------|--      |        |        |", "G4.m7" },
    -- },


            local gr_steps = nil

            if #seq_chunk == 2 then    -- { "|  ...  |", "ABC" }
                gr_steps = parse_chunk(seq_chunk)
            end

            if gr_steps == nil then
                syntax_error("Couldn't parse chunk", seq_chunk)
            else
                steps[seq_name] = gr_steps
            end

            _seq_steps.insert(steps)
        end
    end

    -- Put in time order.
    table.sort(_seq_steps, function (left, right) return left.sub < right.sub end)


    -- Process sections. TODO1?
    _sections = sections

end


-----------------------------------------------------------------------------
--- Parse a chunk pattern.
-- @param chunk like: { "|M-------|--      |        |        |7-------|--      |        |        |", "G4.m7" }
-- @return list of Steps partially filled-in.
local function parse_chunk(chunk)
    local steps = { }

    -- Process the note descriptor first. Could be number, string, function.
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
            -- { step_type=STEP_TYPE.FUNCTION, sub=when, function=func, volume=volmod, duration=dur }
        else
            si = StepNote(when, 0, note_num, dur)
            -- { step_type=STEP_TYPE.NOTE, sub=when, note_num=src, volume=volmod, duration=dur }
        end
        table.insert(steps, si)
    end
end


-----------------------------------------------------------------------------
--- Process notes at this time.
-- @param tick desc
-- @return status
function M.do_step(tick) -- TODO1
    -- calc total sub
    -- get all 
    -- return status?
end

-- Return module.
return M
