
-- Internal functions the script can call - essentially the script api.
-- Impedance matching between C and Lua. Hides or translates the raw C api.
-- Manages note collections as described by the composition.

local ut = require("utils")
local api = require("host_api") -- C api core
local st = require("step_types")
local md = require("midi_defs")
local mu = require("music_defs")
require('neb_common')


local M = {}

-----------------------------------------------------------------------------
-----------------------------------------------------------------------------
-----------------------------------------------------------------------------

-- Steps in sequences.
-- { seq_name_1 = { tick_1 = { StepX, StepX, ... }, tick_2 = { StepX, StepX, ... }, ... }, seq_name_2 = ..., }
local _seq_steps = {}

-- Sequences in sections.
-- { section_name_1 = { { chan_hnd_1 = { seq_name_1,  seq_name_2, ... }, { chan_hnd_2 = { seq_name_3,  seq_name_4, ... }, ... }, section_name_2 = ..., }
local _sections = {}

-- Things that are executed once and disappear: NoteOffs, script send_note().
-- { tick_1 = { StepX, StepX, ... }, tick_2 = ..., },
local _transients = {}

-- Total length of composition.
local _length = 0

-- Where we be.
local _current_tick = 0

function _mole() return _seq_steps, _sections, _transients end


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
-- Send note now. Manages corresponding note off.
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

    -- Transients, mainly noteoff.
    steps = _transients[tick] -- now
    if steps ~= nil then
        for _, step in ipairs(steps) do
            if step.step_type == STEP_NOTE then
               api.send_note(step.chan_hnd, step.note_num, 0)
            end
        end
        table.remove(_transients, tick)
    end

    return 0
end


-----------------------------------------------------------------------------
--- Process all sequences into discrete steps. Sections are stored as is.
-- @param sequences table user sequence specs
-- @param sections table user section specs
-- @return total length in ticks.
function M.init(sequences, sections)
    -- Hard reset.
    _seq_steps = {}
    _transients = {}

    for seq_name, seq_chunks in pairs(sequences) do
        local steps = {}

        for _, seq_chunk in ipairs(seq_chunks) do
            -- { "|5-------|--      |        |        |7-------|--      |        |        |", "G4.m7" }
            local chunk_steps, seq_length = M.parse_chunk(seq_chunk)
            if chunk_steps == nil then
                error(string.format("Couldn't parse chunk: %s", seq_chunk), 2)
            else -- save them
                for c, st in ipairs(chunk_steps) do
                    table.insert(steps, st)
                end
            end
        end

        -- Finished a sequence.
        if #steps > 0 then
            -- Put in time order and save.
            table.sort(steps, function(left, right) return left.tick < right.tick end)
            _seq_steps[seq_name] = steps
        end
    end

    -- Process sections. Fill in Calculate length. TODO1
    _sections = sections

    for sect_name, chan_sections in pairs(sections) do

        for chan_hnd, chan_sequences in pairs(chan_sections) do

            for _, chan_sequence in ipairs(chan_sequences) do






            end




        end




    end



    -- beginning =
    -- {
    --     { hnd_instrument1, nil,         keys_verse,    keys_verse,  keys_verse },
    --     { hnd_instrument2, bass_verse,  bass_verse,    nil,         bass_verse }
    -- },

    -- middle =
    -- {
    --     { hnd_instrument1, nil,          keys_chorus,  keys_chorus,  keys_chorus },
    --     { hnd_instrument2, bass_chorus,  bass_chorus,  bass_chorus,  bass_chorus }
    -- },

    -- drums_verse =
    -- {
    --     -- |........|........|........|........|........|........|........|........|
    --     { "|8       |        |8       |        |8       |        |8       |        |", 10 },
    --     { "|    8   |        |    8   |    8   |    8   |        |    8   |    8   |", 11 },
    --     { "|        |     8 8|        |     8 8|        |     8 8|        |     8 8|", 12 }
    -- },
    -- keys_verse =
    -- {
    --     -- |........|........|........|........|........|........|........|........|
    --     { "|7-------|--      |        |        |7-------|--      |        |        |", "G4.m7" },
    --     { "|        |        |        |5---    |        |        |        |5-8---  |", "G4.m6" }
    -- },



    _length = 100

    return _length
end


-----------------------------------------------------------------------------
--- Parse a chunk pattern. Should be local but this makes testing smoother.
-- @param chunk like: { "|5-------|--      |        |        |7-------|--      |        |        |", "G4.m7" }
-- @return steps, seq_length - list of Steps partially filled-in or nil if invalid.
function M.parse_chunk(chunk)
    if #chunk ~= 2 then return nil end

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

        if func ~= nil then -- func
            si = StepFunction(when, func)
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
    local seq_length = #pattern

    for i = 1, seq_length do
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

    return steps, seq_length
end


-- Return module.
return M
