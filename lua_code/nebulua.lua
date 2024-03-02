
-- Internal functions the script can call - essentially the script api.
-- Impedance matching between C and Lua. Hides or translates the raw C api.
-- Manages note collections as described by the composition.

local ut = require("utils")
local api = require("host_api")
local st = require("step_types")
local md = require("midi_defs")
local mu = require("music_defs")
require('neb_common')

-- TODO2 bulletproof this.

-- TODO2 If you're only using it within a single block, you be even better off performance-wise by simply making it a local 
-- function instead (saves the overhead of a global lookup for each call). I quite often import table.insert and table.remove into 
-- the local namespace if I'm using them frequently, often as something like tinsert() and tremove()
-- also? table.removekey() would my best choice, too)


local M = {}

__nebulua = 292929

-----------------------------------------------------------------------------
-----------------------------------------------------------------------------
-----------------------------------------------------------------------------

-- All the composition StepX. Key is tick aka when-to-play.
local _steps = {}

-- Things that are executed once and disappear: NoteOffs, script send_note(). Same structure as _steps.
local _transients = {}

-- Total length of composition.
local _length = 0

-- Where we be.
local _current_tick = 0


function _mole() return _steps, _transients end -- TODO2 remove

local function lazy_add(tbl, key, obj)
   if tbl[key] == nil then tbl[key] = {} end
   table.insert(tbl[key], obj)
end

-----------------------------------------------------------------------------
-- Log functions. Magic numbers from host C code.
function M.log_error(msg) api.log(4, msg) end
function M.log_info(msg)  api.log(3, msg) end
function M.log_debug(msg) api.log(2, msg) end
function M.log_trace(msg) api.log(1, msg) end


-----------------------------------------------------------------------------
-- These go straight through to the host api.
M.create_input_channel = api.create_input_channel
M.create_output_channel = api.create_output_channel
M.set_tempo = api.set_tempo
M.send_controller = api.send_controller


-----------------------------------------------------------------------------
--- Process notes due now.
-- @param tick desc
-- @return status
function M.process_step(tick)
    _current_tick = tick

    -- Composition steps.
    local steps_now = _steps[tick] -- now
    if steps_now ~= nil then
        for _, step in ipairs(steps_now) do
            if step.step_type == STEP_NOTE then
                if step.volume > 0 then -- noteon - chase
                    dur = step.duration
                    if dur == 0 then dur = 1 end -- (for drum/hit)
                    -- chase with noteoff
                    noteoff = StepNote(_current_tick + dur, step.chan_hnd, step.note_num, 0, 0)
                    lazy_add(_transients, noteoff.tick, noteoff)
                end
                -- now send
                api.send_note(step.chan_hnd, step.note_num, step.volume)
            elseif step.step_type == STEP_CONTROLLER then
                api.send_controller(step.chan_hnd, step.controller, step.value)
            elseif step.step_type == STEP_FUNCTION then
                step.func(_current_tick)
            end
        end
    end

    -- Transients, mainly noteoff.
    steps_now = _transients[tick] -- now
    if steps_now ~= nil then
        for _, step in ipairs(steps_now) do
            if step.step_type == STEP_NOTE then
               api.send_note(step.chan_hnd, step.note_num, 0)
            end
        end
        -- Disappear it.
        _transients[tick] = nil
    end

    return 0
end


-----------------------------------------------------------------------------
-- Send note now. Manages corresponding note off.
function M.send_note(chan_hnd, note_num, volume, dur)
    -- print("send", chan_hnd, note_num, volume, dur)
    if volume > 0 then -- noteon
        if dur == 0 then dur = 1 end -- (for drum/hit)
        -- send note_on now
        api.send_note(chan_hnd, note_num, volume)
        -- chase with noteoff
        noteoff = StepNote(_current_tick + dur, chan_hnd, note_num, 0, 0)
        lazy_add(_transients, noteoff.tick, noteoff)
    else -- send note_off now
       api.send_note(chan_hnd, note_num, 0)
   end
end


-----------------------------------------------------------------------------
--- Process all sections into discrete steps.
-- @param sections table user section specs
-- @return total length in ticks.
function M.init(sections)
    -- Hard reset.
    _steps = {}
    _transients = {}
    local num = 0

    -- Absolute time for step calculation.
    local abs_tick = 0

    for _, section in ipairs(sections) do
        -- Sanity check.
        if section.name == nil then error("Missing section name", 2) end
        -- Save the start for markers.
        section.start = abs_tick
        local channel_max = 0

        -- Iterate the contents.
        for k, v in pairs(section) do
            if k == "name" or k == "start" then
                -- skip
            elseif ut.is_table(v) then
                -- Time offset for this channel events.
                -- TODO1 need a way to get the names/offsets back to host. maybe part of position command?
                local tick = section.start

                -- The sequences. Process each. First element is the channel.
                local chan_hnd = 0
                for i, seq in ipairs(v) do
                    if i == 1 then
                        chan_hnd = seq
                    else
                        for _, seq_chunk in ipairs(seq) do
                            -- { "|5-------|--      |        |        |7-------|--      |        |        |", "G4.m7" }
                            local chunk_steps, seq_length = M.parse_chunk(seq_chunk, chan_hnd, tick)
                            if chunk_steps == nil then
                                error(string.format("Couldn't parse chunk: %s", seq_chunk), 2)
                            else -- save them
                                for c, st in ipairs(chunk_steps) do
                                    lazy_add(_steps, st.tick, st)
                                    num = num +1
                                end
                            end
                            tick = tick + seq_length
                        end
                    end
                end

                -- Keep track of overall time.
                channel_max = math.max(channel_max, tick)
            else
                error(string.format("Element [%s] is not a valid channel", tostring(v)), 2)
            end
        end

        -- Keep track of overall time.
        abs_tick = abs_tick + channel_max

    end

    -- All done.

    return num
end


-----------------------------------------------------------------------------
--- Parse a chunk pattern. Should be local but this makes testing smoother.
-- @param chunk like: { "|5-------|--      |        |        |7-------|--      |        |        |", "G4.m7" }
-- @param chan_hnd
-- @param start_tick
-- @return steps, seq_length - list of StepX or nil if invalid.
function M.parse_chunk(chunk, chan_hnd, start_tick)
    if #chunk ~= 2 then return nil end

    local steps = { }
    local current_vol = 0 -- default, not sounding
    local start_offset = 0 -- in pattern for the start of the current event

    -- Process the note descriptor first. Could be number, string, function.
    local what_to_play = chunk[2]
    local tn = type(what_to_play)
    local notes = {}
    local func = nil

    ----- Local function to package an event. ------
    function make_event(offset)
        -- offset is 0-based.
        local vol = current_vol / 10
        local dur = offset - start_offset
        local when = start_offset + start_tick
        local si = nil

        if func ~= nil then -- func
            si = StepFunction(when, chan_hnd, func, vol)
            if si.err == nil then
                table.insert(steps, si)
            else
                -- Internal error
                error(si.err, 2)
            end
        else -- note
            for _, n in ipairs(notes) do
                si = StepNote(when, chan_hnd, n, vol, dur)
                if si.err == nil then
                    table.insert(steps, si)
                else
                    -- Internal error
                    error(si.err, 2)
                end
            end
        end
    end

    -- Start here
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
