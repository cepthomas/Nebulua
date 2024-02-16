
-- Core generic functions for this app. Matches/requires the C libs.
-- Hides the C api so this is one stop shopping for the user script.

local api = require("host_api") -- C api (or sim)
local st = require("step_types")
local md = require("midi_defs")
local mu = require("music_defs")
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
--- Report a user script syntax error.
-- @param info
local function syntax_error(desc, info)
    s = string.format("Syntax error: %s %s", desc, info or "")
    M.error(s)
    error(s, 3)
end

--- Gets the file and line of the caller.
-- @param level How deep to look:
--    0 is the getinfo() itself
--    1 is the function that called getinfo() - get_caller_info()
--    2 is the function that called get_caller_info() - usually the one of interest
-- @return { filename, linenumber } or nil if invalid
-- function M.get_caller_info(level)
    -- -- Print failure information.
    -- local caller = ut.get_caller_info(4)
    -- info = info or ""
    -- write_error(caller[1]..":"..caller[2].." "..msg..". "..info)

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
-- @param chunk like: { "|5-------|--      |        |        |7-------|--      |        |        |", "G4.m7" }
-- @return list of Steps partially filled-in or nil if invalid.
function M.parse_chunk(chunk) --TODO2 local
    local steps = { }
    local current_vol = 0 -- default, not sounding
    local start_offset = 0 -- in pattern for the start of the current event

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
        notes = mu.get_notes_from_string(what_to_play)
    else
        syntax_error("Invalid what_to_play "..tostring(chunk[2]))
    end

    -- Local function to package an event. chan_hnd is not know now and will get plugged in later.
    function make_event(offset)
        -- offset is 0-based.
        local vol = current_vol / 10
        local dur = offset - start_offset
        local when = start_offset
        local si = nil

        if func then -- func
            si = StepFunction(when, 0, vol, func)
            if si.err == nil then
                table.insert(steps, si)
            else
                -- Internal error
                syntax_error(si.err)
            end
        else -- note
            for _, n in ipairs(notes) do
                si = StepNote(when, 0, n, vol, dur)
                if si.err == nil then
                    table.insert(steps, si)
                else
                    -- Internal error
                    syntax_error(si.err)
                end
            end
        end
    end

    -- Process note instances.
    -- Remove visual markers from pattern.
    local pattern = string.gsub(chunk[1], "|", "")

    for i = 1, #pattern do
        local c = pattern:sub(i, i)
        local cnum = string.byte(c) - 48

        if c == '-' then
-- dbg()
            -- Continue current note.
            if current_vol > 0 then
                -- ok, do nothing
            else
                -- invalid condition
                syntax_error("Invalid \'-\'' in pattern string")
            end
        elseif cnum >= 1 and cnum <= 9 then

-- dbg.assert(false, 'howdy')

            -- A new note starting.
            -- Do we need to end the current note?
            if current_vol > 0 then
                make_event(i - 1)
            end
            -- Start new note.
            current_vol = cnum
            start_offset = i - 1
        elseif c == ' ' or c == '.' then
-- dbg()
            -- No sound.
            -- Do we need to end the current note?
            if current_vol > 0 then
                make_event(i - 1)
            end
            current_vol = 0
        else
-- dbg()
            -- Invalid char.
            syntax_error("Invalid char in pattern string", c)
        end
    end

    -- Straggler?
    if current_vol > 0 then
        make_event(#pattern - 1)
    end

    return steps
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
