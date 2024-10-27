
-- Internal functions the script can call - essentially the script api.
-- Impedance matching between C and Lua. Hides or translates the raw C api.
-- Manages note collections as described by the composition.

local ut  = require("utils")
local api = require("host_api")
local st  = require("step_types")
local mid = require("midi_defs")
local mus = require("music_defs")
local com = require("neb_common")

-- TODOF stress test and bulletproof this.

local M = {}

-----------------------------------------------------------------------------
----- Global vars for access by app
-----------------------------------------------------------------------------

-- Key is section name, value is start tick. Total length is the last element.
section_info = {}


-----------------------------------------------------------------------------
----- Private vars
-----------------------------------------------------------------------------

-- All the sections defined in the script.
local _sections = {}

-- For parsing script sections.
local _current_section = nil

-- All the composition StepX. Key is tick aka when-to-play.
local _steps = {}

-- Things that are executed once and disappear: NoteOffs, script send_note(). Same structure as _steps.
local _transients = {}

-- Where we be.
local _current_tick = 0

-- Map the 0-9 script volume range to internal double. TODOF make user configurable per channel or enum map_curve = LIN, SQR, ....
local _vol_map = { 0.0, 0.01, 0.05, 0.11, 0.20, 0.31, 0.44, 0.60, 0.79, 1.0 }
-- linear local _vol_map = { 0.0, 0.11, 0.22, 0.33, 0.44, 0.55, 0.66, 0.77, 0.88, 1.0 }
-- original local _vol_map = { 0.0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9 }

-- Key is chan_hnd, value is double volume.
local _master_vols = {}

-- Debug stuff.
-- function _mole() return _steps, _transients end


-----------------------------------------------------------------------------
-----------------------------------------------------------------------------
----- Script api -- what you can call from the script
-----------------------------------------------------------------------------
-----------------------------------------------------------------------------

-- Log functions. Magic numbers from host code.
function M.log_error(msg) api.log(4, msg) end
function M.log_warn(msg)  api.log(3, msg) end
function M.log_info(msg)  api.log(2, msg) end
function M.log_debug(msg) api.log(1, msg) end
function M.log_trace(msg) api.log(0, msg) end

M.log_info('### loading nebulua.lua ### ')

-----------------------------------------------------------------------------
--- Set system tempo. This goes straight through to the host.
-- @param bpm new tempo
-- @return status
M.set_tempo = api.set_tempo

-----------------------------------------------------------------------------
--- Send a control message now. This goes straight through to the host.
-- @param chan_hnd channel handle
-- @param controller Specific controller 0 => 127
-- @param value Payload 0 => 127
-- @return status
M.send_controller = api.send_controller

-----------------------------------------------------------------------------
--- Create an input channel.
-- @param dev_name system name
-- @param chan_num channel number
-- @return the new chan_hnd or 0 if invalid
function M.create_input_channel(dev_name, chan_num)
    chan_hnd = api.create_input_channel(dev_name, chan_num)
    return chan_hnd
end

-----------------------------------------------------------------------------
--- Create an output channel.
-- @param dev_name system name
-- @param chan_num channel number
-- @param patch send this patch number
-- @return the new chan_hnd or 0 if invalid
function M.create_output_channel(dev_name, chan_num, patch)
    chan_hnd = api.create_output_channel(dev_name, chan_num, patch)
    _master_vols[chan_hnd] = 0.8
    return chan_hnd
end

-----------------------------------------------------------------------------
--- Set master volume for the channel.
-- @param chan_hnd the channel
-- @param volume master volume
-- @return status
function M.set_volume(chan_hnd, volume)
    if volume < 0.0 or volume > 1.0 then
        error(string.format("Invalid master volume %f", volume), 2)
    end
    _master_vols[chan_hnd] = volume

    return 0
end

-----------------------------------------------------------------------------
--- Process notes due now.
-- @param tick Current tick
-- @return status
function M.process_step(tick)
    _current_tick = tick

    -- Composition steps.
    local steps_now = _steps[tick] -- now
    if steps_now ~= nil then
        for _, step in ipairs(steps_now) do
            if step.step_type == "note" then
               if step.volume > 0 then -- noteon - add note off chase
                   dur = step.duration
                   if dur == 0 then dur = 1 end -- (for drum/hit)
                   -- chase with noteoff
                   noteoff = StepNote(_current_tick + dur, step.chan_hnd, step.note_num, 0, 0)
                   ut.table_add(_transients, noteoff.tick, noteoff)
               end

                -- now send
                api.send_note(step.chan_hnd, step.note_num, step.volume)
            elseif step.step_type == "controller" then
                api.send_controller(step.chan_hnd, step.controller, step.value)
            elseif step.step_type == "function" then
                step.func(_current_tick)
            end
        end
    end

    -- Transients, mainly noteoff.
    steps_now = _transients[tick] -- now
    if steps_now ~= nil then
        for _, step in ipairs(steps_now) do
            if step.step_type == "note" then
               api.send_note(step.chan_hnd, step.note_num, 0)
            end
        end
        -- Disappear it.
        _transients[tick] = nil
    end

    return 0
end

-----------------------------------------------------------------------------
-- Send note immediately. Manages corresponding note off if tick clock is running.
function M.send_note(chan_hnd, note_num, volume, dur)
    if dur == nil then dur = 0 end
    -- M.log_debug(string.format("Send now hnd:%d note:%d vol:%f dur:%d", chan_hnd, note_num, volume, dur))
    if volume > 0 then -- noteon
        -- adjust volume
        volume = volume * _master_vols[chan_hnd]
        -- send note_on now
        api.send_note(chan_hnd, note_num, volume)
        if dur > 0 then
            -- chase with noteoff
            noteoff = StepNote(_current_tick + dur, chan_hnd, note_num, 0, 0)
            ut.table_add(_transients, noteoff.tick, noteoff)
        end
    else -- send note_off now
       api.send_note(chan_hnd, note_num, 0)
   end
end

-----------------------------------------------------------------------------
-- Process the chunks in the sequence into a list of steps and return that.
function M.parse_sequence_steps(seq)

    steps = {}

    for _, seq_chunk in ipairs(seq) do
        -- Reset position to start of sequence.
        tick = 0
        local seq_length, seq_steps = M.parse_chunk(seq_chunk, chan_hnd, tick)
        steps = seq_steps
    end

    return steps
end

-----------------------------------------------------------------------------
-- Manually send a list of steps starting now.
function M.send_sequence_steps(seq_steps)


    if seq_steps ~= nil then
        for _, step in ipairs(seq_steps) do
            if step.step_type == "note" then
               if step.volume > 0 then -- noteon - add note off chase
                   dur = step.duration
                   if dur == 0 then dur = 1 end -- (for drum/hit)
                   noteon = StepNote(_current_tick + step.tick, step.chan_hnd, step.note_num, step.volume, step.duration)
                   ut.table_add(_transients, noteon.tick, noteon)
                   -- chase with noteoff
                   noteoff = StepNote(_current_tick + step.tick + dur, step.chan_hnd, step.note_num, 0, 0)
                   ut.table_add(_transients, noteoff.tick, noteoff)
               end
            end
        end
    end
end

-----------------------------------------------------------------------------
--- Process all sections into discrete steps.
function M.process_comp()
    -- Hard reset.
    _steps = {}
    _transients = {}
    section_info = {}
    -- Total length of composition.
    local length = 0

    length = 0 -- cumulative total length

    for isect, section in ipairs(_sections) do
        -- Process the section. Requires a name.
        if section.name == nil then
            error(string.format("Missing section name in section %d", isect), 2)
        end

        -- Do the work.
        M.parse_section(section, length)

        -- Save info about section.
        section_info[section.name] = section.start

        -- Keep track of overall time.
        length = length + section.length
    end

    -- All done. Tack on the total.
    section_info["_LENGTH"] = length
end


-----------------------------------------------------------------------------
-----------------------------------------------------------------------------
----- Internal functions and stuff called from host
-----------------------------------------------------------------------------
-----------------------------------------------------------------------------

-----------------------------------------------------------------------------
--- Process one section. No return but does update the section fields.
-- @param section target
-- @param start_tick absolute start time 0-based
function M.parse_section(section, start_tick)
    section.start = start_tick
    section.length = 0

    local tick = section.start -- running position
    local seq_start = tick -- start of current sequence

    -- Iterate through the section contents.
    for _, sect_chan in ipairs(section) do
        -- Process section channel.

        if #sect_chan >= 2 and type(sect_chan[1]) == "number" then
            -- Specific channel
            local chan_hnd = sect_chan[1]

            -- Reset.
            seq_start = section.start

            -- Process each sequence.
            for i = 2, #sect_chan do
                -- Track max.
                seq_length_max = 0

                -- Process the chunks in the sequence.
                local current_seq = sect_chan[i]

                for _, seq_chunk in ipairs(current_seq) do
                    -- Reset position to start of sequence.
                    tick = seq_start

                    local seq_length, chunk_steps = M.parse_chunk(seq_chunk, chan_hnd, tick)
                    if seq_length ~= 0 then -- save the steps to master table
                        for _, step in ipairs(chunk_steps) do
                            ut.table_add(_steps, step.tick, step)
                        end
                        seq_length_max = math.max(seq_length_max, seq_length)
                    else
                        error(string.format("Couldn't parse sequence %d in section '%s'", i - 1, section.name), 1)
                    end
                end

                -- Keep track of overall times for section.
                tick = tick + seq_length_max
                seq_start = tick
            end
        else
            error(string.format("Invalid channel in section '%s'", section.name), 1)
        end
    end

    -- Finish up.
    section.length = tick - section.start
end

-----------------------------------------------------------------------------
--- Parse a chunk pattern.
--- This does not call error() so caller can process in context. However this results in a somewhat messy
--- multiple early return scenario. Sorry.
-- @param chunk like: { "|5-------|--      |        |        |7-------|--      |        |        |", "G4.m7" }
-- @param chan_hnd the channel
-- @param start_tick absolute start time 0-based
-- @return length, list of StepXs OR 0, list of errors if invalid.
function M.parse_chunk(chunk, chan_hnd, start_tick)
    if chunk[1] == nil or chunk[2] == nil then
        return 0, {"Improperly formed chunk."}
    end

    -- array of steps in this chunk
    local steps = { }
    local current_vol = 0 -- 0 -> 9
    local event_offset = 0 -- offset in chunk pattern for the start of the current event


    ---------- Local function to package an event. ------ ------
    function make_note_event(offset, notes_to_play)
        -- scale volume
        local vol = _vol_map[current_vol + 1] -- to lua index
        -- calc duration from start of note
        local dur = offset - event_offset
        -- must be valid duration, covers drum hits too
        if dur <= 0 then dur = 1 end

        for _, nt in ipairs(notes_to_play) do
            local si = StepNote(start_tick + event_offset, chan_hnd, nt, vol, dur)
            if si.err == nil then
                table.insert(steps, si)
            else
                -- Syntax error
            return si.err
            end
        end
        return nil
    end

    ---------- Local function to package an event. ------------
    function make_func_event(offset, func, vol_num)
        -- scale volume
        local vol = _vol_map[vol_num]
        local si = StepFunction(start_tick + event_offset, chan_hnd, func, vol)
        if si.err == nil then
            table.insert(steps, si)
        else
            -- Syntax error
            return si.err
        end
        return nil
    end

    ---------- Actual start here ----------

    -- Process the note descriptor first. Could be number, string, function.
    local what_to_play = chunk[2]
    local tn = type(what_to_play)
    local notes_to_play = nil    local func = nil

    if tn == "number" then
        -- use as is
        notes_to_play = { what_to_play }
    elseif tn == "function" then
        -- use as is
        func = what_to_play
        -- if func == nil then
        --     return 0, {string.format("Invalid func %s", chunk[2])}
        -- end
    elseif tn == "string" then
        notes_to_play = mus.get_notes_from_string(what_to_play)
        -- if notes_to_play == nil then
        --     return 0, {string.format("Invalid notes %s", chunk[2])}
        -- end
    else
        return 0, {string.format("Invalid note descriptor '%s'", tostring(chunk[2]))}
    end

    -- Process note instances.
    -- Remove visual markers from pattern.
    local pattern = string.gsub(chunk[1], "|", "")
    local seq_length = #pattern

    for i = 1, seq_length do
        local c = pattern:sub(i, i)
        local vol_num = string.byte(c) - 48 -- '0'
        local seq_err = nil

        if func ~= nil then -- func
            if vol_num > 0 and vol_num <= 9 then
                seq_err = make_func_event(i - 1, func, vol_num)
            end
        else -- note
            if c == '-' then
                -- Continue current note.
                if current_vol > 0 then
                    -- ok, do nothing
                else
                    -- invalid condition
                    seq_err = string.format("Invalid \'-\' in pattern string: %s", chunk[1])
                end
            elseif vol_num >= 1 and vol_num <= 9 then
                -- A new note starting.
                -- Do we need to end the current note?
                if current_vol > 0 then
                    seq_err = make_note_event(i - 1, notes_to_play)
                end
                -- Start new note.
                current_vol = vol_num
                event_offset = i - 1
            elseif c == ' ' or c == '.' then
                -- No sound.
                -- Do we need to end the current note?
                if current_vol > 0 then
                    seq_err = make_note_event(i - 1, notes_to_play)
                end
                current_vol = 0
            else
                -- Invalid char.
                seq_err = string.format("Invalid char %c in pattern string: %s", c, chunk[1])
            end
        end

        if seq_err ~= nil then return 0, {seq_err} end
    end

    -- Straggler?
    if func == nil then
        if current_vol > 0 then
            local seq_err = make_note_event(seq_length - 1, notes_to_play)
            if seq_err ~= nil then return 0, {seq_err} end
        end
    end

    return seq_length, steps
end

-----------------------------------------------------------------------------
--- Composition spec: start a new section definition.
-- @param name what to call it
function M.sect_start(name)
    _current_section = {}
    _current_section.name = name
    table.insert(_sections, _current_section)
end

-----------------------------------------------------------------------------
--- Composition spec: add sequences to the current section.
-- @param chan_hnd the channel
-- @param ... the sequences
function M.sect_chan(chan_hnd, ...)
    if _current_section ~= nil then
        elems = {}
        if type(chan_hnd) ~= "number" then -- should check for valid/known handle
            error("Invalid channel", 2)
        end
        table.insert(elems, chan_hnd)

        num_args = select('#', ...)
        if num_args < 1 then
            error("No sequences", 2)
        end

        for i = 1, num_args do
            seq = select(i, ...)
            if type(seq) ~= "table" then -- should check for valid/known
                error("Invalid sequence "..i, 2)
            end
            table.insert(elems, seq)
        end

        table.insert(_current_section, elems)
    else
        error("No section name in sect_start()", 2)
    end
end

-----------------------------------------------------------------------------
--- Diagnostic.
-- @param fn file name
function M.dump_steps(fn)
    fp, err = io.open(fn, 'w+')
    if fp == nil then error("Can't open file: "..err) end
    for tick, sts in pairs(_steps) do
        for i, step in ipairs(sts) do
            fp:write(string.format("%s\n", step.format()))
            -- fp:write(string.format("%d %s\n", tick, step.format()))
            -- fp:write(string.format("%d %s\n", tick, ut.dump_table_string(step, true, "X")))
        end
    end
    fp:close()
end


-----------------------------------------------------------------------------
-- Return module.
return M
