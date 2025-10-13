
-- Functions the script can call - essentially the script api.
-- Impedance matching between C and Lua. Hides or translates the raw C interop.
-- Manages note collections currently being played.

local dbg = require("debugex")

local li  = require("luainterop")
local ut  = require("lbot_utils")
local sx  = require("stringex")
local tx  = require("tableex")
local st  = require("step_types")
local mid = require("midi_defs")
local mus = require("music_defs")


-- TODO stress test and bulletproof this.

local M = {}


-----------------------------------------------------------------------------
----- Private vars
-----------------------------------------------------------------------------

-- All the sections defined in the script.
local _sections = {}

-- For parsing script sections.
local _current_section = nil

-- All the composition StepX. Key is tick aka when-to-play.
local _steps = {}

-- Things that are executed once and disappear: NoteOffs, script send_midi_note(). Same structure as _steps.
local _transients = {}

-- Where we be.
local _current_tick = 0

-- Key is chan_hnd, value is volume 0.0 -> 1.0.
local _channel_volumes = {}

-- Map the 0-9 script volume levels to actual volumes. Give it a bit of a curve.
local _volume_map = { 0.0, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0 } -- modified linear

-----------------------------------------------------------------------------
-- Lazy add to collection.
-- @param tbl the table
-- @param key new entry key
-- @param val new entry value
local function _table_add(tbl, key, val)
   if tbl[key] == nil then tbl[key] = {} end
   table.insert(tbl[key], val)
end

-----------------------------------------------------------------------------
------------- Script calls host li and internal lua functions --------------
-----------------------------------------------------------------------------


-----------------------------------------------------------------------------
--- Log functions. This goes straight through to the host.
-- Level error is fatal and will stop the app.
-- Magic numbers must match host code.
-- @param msg what to log
function M.log_error(msg) li.log(4, msg) end
function M.log_warn(msg)  li.log(3, msg) end
function M.log_info(msg)  li.log(2, msg) end
function M.log_debug(msg) li.log(1, msg) end
function M.log_trace(msg) li.log(0, msg) end

M.log_info('Loading script_api.lua...')


-----------------------------------------------------------------------------
--- Set system tempo.
-- @param bpm new tempo
-- @return status
function M.set_tempo(bpm)
    local ret = li.set_ret(bpm)
    -- if tempo == -1 then error(string.format("Invalid tempo:%d", bpm), 2) end
end

-----------------------------------------------------------------------------
--- Send a control message now.
-- @param chan_hnd channel handle
-- @param controller Specific controller 0 -> 127
-- @param value Payload 0 -> 127
-- @return status
function M.send_midi_controller(chan_hnd, controller, value)
    local ret = li.send_midi_controller(chan_hnd, controller, value)
    -- if ret == -1 then log_warn(string.format("Invalid midi_controller %d %d %d", chan_hnd, controller, value), 2) end
end

-----------------------------------------------------------------------------
--- Create an input channel.
-- @param dev_name system name
-- @param chan_num channel number
-- @return the new chan_hnd or 0 if invalid
function M.open_midi_input(dev_name, chan_num, chan_name)
    local chan_hnd = li.open_midi_input(dev_name, chan_num, chan_name)
    if chan_hnd == -1 then error(string.format("Invalid midi input dev:%s num:%d name:%s", dev_name, chan_num, chan_name), 2) end
    return chan_hnd
end

-----------------------------------------------------------------------------
--- Create an output channel.
-- @param dev_name system name
-- @param chan_num channel number
-- @param patch send this patch number if >= 0
-- @return the new chan_hnd
function M.open_midi_output(dev_name, chan_num, chan_name, patch)
    local chan_hnd = li.open_midi_output(dev_name, chan_num, chan_name, patch)
    _channel_volumes[chan_hnd] = 1.0 -- default to passthrough.
    if chan_hnd == -1 then error(string.format("Invalid midi output dev:%s num:%d name:%s patch:%d", dev_name, chan_num, chan_name, patch), 2) end
    return chan_hnd
end

-----------------------------------------------------------------------------
--- Set volume for the channel.
-- @param chan_hnd the channel
-- @param volume volume
-- @return status
function M.set_volume(chan_hnd, volume)
    if volume < 0.1 or volume > 2.0 then
        M.log_warn(string.format("Invalid channel volume %f", volume), 2)
        -- error(string.format("Invalid channel volume %f", volume), 2)
    end
    _channel_volumes[chan_hnd] = volume

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
                   local dur = math.max(step.duration, 1) -- (min for drum/hit)
                   -- chase with noteoff
                   local noteoff = st.note(_current_tick + dur, step.chan_hnd, step.note_num, 0, 0)
                   _table_add(_transients, noteoff.tick, noteoff)
               end

                -- now send
                li.send_midi_note(step.chan_hnd, step.note_num, step.volume)
            elseif step.step_type == "controller" then
                li.send_midi_controller(step.chan_hnd, step.controller, step.value)
            elseif step.step_type == "function" then
                step.func(_current_tick)
            end
        end
    end

    -- Transients, usually chase noteoff.
    steps_now = _transients[tick] -- now
    if steps_now ~= nil then
        for _, step in ipairs(steps_now) do
            if step.step_type == "note" then
               li.send_midi_note(step.chan_hnd, step.note_num, step.volume)
            end
        end
        -- Disappear it from collection.
        _transients[tick] = nil
    end

    return 0
end

-----------------------------------------------------------------------------
-- Send note immediately. Manages corresponding note off.
function M.send_midi_note(chan_hnd, note_num, volume, dur)
    if dur == nil then dur = 0 end
    -- M.log_debug(string.format("Send now hnd:%d note:%d vol:%f dur:%d", chan_hnd, note_num, volume, dur))

    if volume > 0 then -- noteon
        -- adjust volume
        volume = volume * _channel_volumes[chan_hnd]
        -- send note_on now
        li.send_midi_note(chan_hnd, note_num, volume)
        if dur > 0 then
            -- chase with noteoff
            local noteoff = st.note(_current_tick + dur, chan_hnd, note_num, 0, 0)
            _table_add(_transients, noteoff.tick, noteoff)
        end
    else -- send note_off now
       li.send_midi_note(chan_hnd, note_num, 0)
   end
end

-----------------------------------------------------------------------------
-- Process the chunks in the sequence into a list of steps and return that.
function M.parse_sequence_steps(chan_hnd, seq)
    local steps = {}
    for _, seq_chunk in ipairs(seq) do
        -- Reset position to start of sequence.
        local tick = 0
        local seq_length, seq_steps = M.parse_chunk(seq_chunk, chan_hnd, tick)
        for _, step in ipairs(seq_steps) do
            table.insert(steps, step)
        end
    end
    return steps
end

-----------------------------------------------------------------------------
-- Send a list of steps immediately.
function M.send_sequence_steps(seq_steps, tick)
    if seq_steps == nil then return end

    for _, step in ipairs(seq_steps) do
        if step.step_type == "note" then

           if step.volume > 0 then -- is noteon
               local dur = math.max(step.duration, 1) -- (min for drum/hit)

               local noteon = st.note(tick + step.tick, step.chan_hnd, step.note_num, step.volume, dur)
               _table_add(_transients, noteon.tick, noteon)

               -- chase with noteoff
               local noteoff = st.note(tick + step.tick + dur, step.chan_hnd, step.note_num, 0, 0)
               _table_add(_transients, noteoff.tick, noteoff)

           else -- note off
               local noteoff = st.note(tick + step.tick, step.chan_hnd, step.note_num, 0, 0)
               _table_add(_transients, noteoff.tick, noteoff)
           end

        end
    end
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
    local function make_note_event(offset, notes_to_play)
        -- scale volume
        local vol = _volume_map[current_vol + 1] -- to lua index
        -- calc duration from start of note
        local dur = math.max(offset - event_offset, 1) -- (min for drum/hit)

        for _, nt in ipairs(notes_to_play) do
            local si = st.note(start_tick + event_offset, chan_hnd, nt, vol, dur)
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
    local function make_func_event(offset, func, vol_num)
        -- scale volume
        local vol = _volume_map[vol_num]
        local si = st.func(start_tick + event_offset, chan_hnd, func, vol)
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
    local notes_to_play = nil
    local func = nil

    if tn == "number" then
        -- use as is
        notes_to_play = { what_to_play }
    elseif tn == "function" then
        -- use as is
        func = what_to_play
    elseif tn == "string" then
        notes_to_play = mus.get_notes_from_string(what_to_play)
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
                seq_err = string.format("Invalid char %s in pattern string: %s", c, chunk[1])
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
----------------- Composition stuff -----------------------------------------
-----------------------------------------------------------------------------

-----------------------------------------------------------------------------
--- Process all sections into discrete steps. Return meta info about composition.
function M.process_comp()
    -- Hard reset.
    _steps = {}
    _transients = {}

    -- Key is section name, value is start tick. Total length is the last element.
    local section_info = {}

    -- Accumulate length of composition.
    local length = 0

    -- dbg()

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

    -- Stringify the meta.
    local res = {}
    for k, v in pairs(section_info) do
        table.insert(res, k..','..v)
    end

    return sx.strjoin('|', res)
end

-----------------------------------------------------------------------------
--- Process one section. Updates the section fields.
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
                local seq_length_max = 0

                -- Process the chunks in the sequence.
                local current_seq = sect_chan[i]

                for _, seq_chunk in ipairs(current_seq) do
                    -- Reset position to start of sequence.
                    tick = seq_start

                    local seq_length, chunk_steps = M.parse_chunk(seq_chunk, chan_hnd, tick)
                    if seq_length ~= 0 then -- save the steps to master table
                        for _, step in ipairs(chunk_steps) do
                            _table_add(_steps, step.tick, step)
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
        local elems = {}
        if type(chan_hnd) ~= "number" then -- should check for valid/known handle
            error("Invalid channel", 2)
        end
        table.insert(elems, chan_hnd)

        local num_args = select('#', ...)
        if num_args < 1 then
            error("No sequences", 2)
        end

        for i = 1, num_args do
            local seq = select(i, ...)
            if type(seq) == "table" then -- should check for valid/known
                table.insert(elems, seq)
            else
                error("Invalid sequence "..type(seq)..' '..i, 2)
            end
        end

        table.insert(_current_section, elems)
    else
        error("No section name in sect_start()", 2)
    end
end

-----------------------------------------------------------------------------
--- Diagnostic.
-- @param fn file name
-- @param which 's'=steps 't'=transients
function M.dump_steps(fn, which)
    local t = nil
    if which == 's' then
        t = _steps
    elseif which == 't' then
        t = _transients
    else
        error('Invalid which '..which)
    end

    local fp, _ = io.open(fn, 'w+')
    for tick, sts in pairs(t) do
        for i, step in ipairs(sts) do
            fp:write(tostring(step)..'\n')
        end
    end
    fp:close()
end


-----------------------------------------------------------------------------
-- Return module.
return M
