
-- Internal functions the script can call - essentially the script api.
-- Impedance matching between C and Lua. Hides or translates the raw C api.
-- Manages note collections as described by the composition.

local api = require("host_api")
local st = require("step_types")
local md = require("midi_defs")
local mu = require("music_defs")
require('neb_common')


local M = {}

-----------------------------------------------------------------------------
-----------------------------------------------------------------------------
-----------------------------------------------------------------------------

-- Steps in sequences.
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


-- Sequences in sections.
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


-- Things that are executed once and disappear: NoteOffs, script send now. Key is the tick.
-- {
--     tick_1 = { StepX, StepX, ... },
--     tick_2 = { StepX, StepX, ... },
--     ...
-- },
local _transients = {}


-- Total length of composition.
local _length = 0
-- function M.get_length() return _length end
-- function M.set_length(l) _length = l end

local _current_tick = 0


tempdbg = { steps = _seq_steps, sections = _sections } -- TODO2 Debug stuff - remove


-----------------------------------------------------------------------------
-- Log functions. Magic numbers from C code.
function M.log_error(msg) api.log(4, msg) end
function M.log_info(msg)  api.log(3, msg) end
function M.log_debug(msg) api.log(2, msg) end
function M.log_trace(msg) api.log(1, msg) end


-----------------------------------------------------------------------------
-- These go straight through so just thunk the C api.
M.create_input_channel = api.create_input_channel
M.create_output_channel = api.create_output_channel
M.set_tempo = api.set_tempo
M.send_controller = api.send_controller


-----------------------------------------------------------------------------
-- Send note. Manages corresponding note off.
function M.send_note(chan_hnd, note_num, volume, dur)
    if volume > 0 then -- noteon
       if dur == 0 then dur = 1 end -- (for drum/hit)
       -- send note_on now
       api.send_note(chan_hnd, note_num, volume)
       -- chase with noteoff
       noteoff = StepNote(_current_tick + dur, chan_hnd, note_num, 0, 0)
       table.insert(_transients, noteoff)
    else -- noteoff
       -- send note_off now
       api.send_note(chan_hnd, note_num, 0)
end


-----------------------------------------------------------------------------
--- Process notes due now.
-- @param tick desc
-- @return status
function M.process_step(tick)
    _current_tick = tick

    -- Composition steps.
    local steps = _seq_steps[tick] -- now
    if steps ~= nil then
        for _, step in ipairs(steps) do
            if step.step_type == STEP_NOTE then
               api.send_note(step.chan_hnd, step.note_num, step.volume)
            elseif step.step_type == STEP_CONTROLLER then
                api.send_controller(step.chan_hnd, step.controller, step.value)
            elseif step.step_type == STEP_FUNCTION then
                step.func(_current_tick)
            end
        end
    end

    -- Transients, mainly note off.
    steps = _transients[tick] -- now
    if steps ~= nil then
        for _, step in ipairs(steps) do
            if step.step_type == STEP_NOTE then
               api.send_note(step.chan_hnd, step.note_num, 0)
            end
        end
        table.remove(_transients, tick)
    end
end


-----------------------------------------------------------------------------
--- Process all sequences into discrete steps. Sections are stored as is.
-- @param sequences table user sequence specs
-- @param sections table user section specs
-- @return total length in ticks.
function M.init(sequences, sections)
    _seq_steps.clear()
    _transients.clear()

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
                error(string.format("Couldn't parse chunk: %s", seq_chunk), 2)
            else
                steps[seq_name] = gr_steps
            end

            _seq_steps.insert(steps)
        end
    end

    -- Put in time order.
    table.sort(_seq_steps, function (left, right) return left.sub < right.sub end)

    -- Process sections. Calculate length. TODO1
    _sections = sections
    _length = 100
    return _length
end


-----------------------------------------------------------------------------
--- Parse a chunk pattern.
-- @param chunk like: { "|5-------|--      |        |        |7-------|--      |        |        |", "G4.m7" }
-- @return list of Steps partially filled-in or nil if invalid.
function M.parse_chunk(chunk) --TODO2 should be local
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
        error(string.format("Invalid what_to_play %s", chunk[2]), 2)
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
                error(si.err, 2)
            end
        else -- note
            for _, n in ipairs(notes) do
                si = StepNote(when, 0, n, vol, dur)
                if si.err == nil then
                    table.insert(steps, si)
                else
                    -- Internal error
                    error(si.err, 2)
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
            -- Continue current note.
            if current_vol > 0 then
                -- ok, do nothing
            else
                -- invalid condition
                error(string.format("Invalid \'-\'' in pattern string: %s", chunk[1]), 2)
            end
        elseif cnum >= 1 and cnum <= 9 then
            -- A new note starting.
            -- Do we need to end the current note?
            if current_vol > 0 then
                make_event(i - 1)
            end
            -- Start new note.
            current_vol = cnum
            start_offset = i - 1
        elseif c == ' ' or c == '.' then
            -- No sound.
            -- Do we need to end the current note?
            if current_vol > 0 then
                make_event(i - 1)
            end
            current_vol = 0
        else
            -- Invalid char.
            error(string.format("Invalid char %c in pattern string: %s", c, chunk[1]), 2)
        end
    end

    -- Straggler?
    if current_vol > 0 then
        make_event(#pattern - 1)
    end

    return steps
end


-- Return module.
return M
