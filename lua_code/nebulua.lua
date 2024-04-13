
-- Internal functions the script can call - essentially the script api.
-- Impedance matching between C and Lua. Hides or translates the raw C api.
-- Manages note collections as described by the composition.

local ut  = require("utils")
local api = require("host_api")
local st  = require("step_types")
local mid = require("midi_defs")
local mus = require("music_defs")
local com = require('neb_common')

-- TODO2 stress test and bulletproof this.

local M = {}


-----------------------------------------------------------------------------
----- Global vars for access by app
-----------------------------------------------------------------------------

-- Total length of composition.
_length = 0

-- Key is section name, value is start tick.
_section_names = {}


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

-- Map the 0-9 script volume range to internal double. TODO make user configurable per channel.
local _vol_map = { 0.0, 0.01, 0.05, 0.11, 0.20, 0.31, 0.44, 0.60, 0.79, 1.0 }
-- linear local _vol_map = { 0.0, 0.11, 0.22, 0.33, 0.44, 0.55, 0.66, 0.77, 0.88, 1.0 }
-- original local _vol_map = { 0.0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9 }

-- Key is chan_hnd, value is double volume.
local _master_vols = {}

-- Debug stuff TODO2 remove - or pass a debug/test flag?
function _mole() return _steps, _transients end


-----------------------------------------------------------------------------
----- Script api
-----------------------------------------------------------------------------

-- Log functions. Magic numbers from host code.
function M.log_error(msg) api.log(4, msg) end
function M.log_info(msg)  api.log(3, msg) end
function M.log_debug(msg) api.log(2, msg) end
function M.log_trace(msg) api.log(1, msg) end


-- These go straight through to the host.
M.create_input_channel = api.create_input_channel
M.set_tempo = api.set_tempo
M.send_controller = api.send_controller


-----------------------------------------------------------------------------
--- Create an output channel.
-- @param dev_name system name
-- @param chan_num channel number
-- @param patch send this patch number
-- @return the new chan_hnd
function M.create_output_channel(dev_name, chan_num, patch)
    chan_hnd = api.create_output_channel(dev_name, chan_num, patch)
    _master_vols[chan_hnd] = 0.8
    return chan_hnd
end    

-----------------------------------------------------------------------------
--- Set master volume for the channel.
-- @param chan_hnd the channel
-- @param tick volume master volume
function M.set_volume(chan_hnd, volume)
    if volume < 0.0 or volume > 1.0 then
        error(string.format("Invalid master volume %f", index_section), 1)
    end
    _master_vols[chan_hnd] = volume
end

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
            if step.step_type == "note" then
                if step.volume > 0 then -- noteon - chase
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
-- Send note now. Manages corresponding note off.
function M.send_note(chan_hnd, note_num, volume, dur)
    M.log_debug("send", chan_hnd, note_num, volume, dur)
    if volume > 0 then -- noteon
        if dur == 0 then dur = 1 end -- (for drum/hit)
        -- adjust volume
        volume = volume * _master_vols[chan_hnd]
        -- send note_on now
        api.send_note(chan_hnd, note_num, volume)
        -- chase with noteoff
        noteoff = StepNote(_current_tick + dur, chan_hnd, note_num, 0, 0)
        ut.table_add(_transients, noteoff.tick, noteoff)
    else -- send note_off now
       api.send_note(chan_hnd, note_num, 0)
   end
end

-----------------------------------------------------------------------------
--- Process all sections into discrete steps.
function M.process_comp()
    -- Hard reset.
    _steps = {}
    _transients = {}
    _section_names = {}
    _length = 0
    local ed = 2

    for index_section, section in ipairs(_sections) do
        -- Process one section.
        -- Requires a name.
        if section.name == nil then error(string.format("Missing section name in section %d", index_section), ed) end
        -- Save the start tick for markers.
        section.start = _length
        _section_names[section.name] = section.start

        -- The longest sequence in the section.
        local section_max = 0

        -- Iterate the channel sequences.
        local chan_seq_index = 0
        for k, v in pairs(section) do
            if k == "name" or k == "start" then
                -- skip, already handled
            elseif ut.is_table(v) then
                chan_seq_index = chan_seq_index + 1
                -- Time offset for this channel events.
                local tick = section.start

                -- Process the sequences. First element is the channel followed by the sequences.
                local chan_hnd = 0
                for index_elem, seq_elem in ipairs(v) do
                    if index_elem == 1 then
                        chan_hnd = seq_elem
                    else -- it's a sequence
                        -- Process the chunks in the sequence.
                        for c, seq_chunk in ipairs(seq_elem) do
                            -- { "|5-------|--      |        |        |7-------|--      |        |        |", notes... }
                            local seq_length, chunk_steps = M.parse_chunk(seq_chunk, chan_hnd, tick)
                            if seq_length == 0 then
                                error(string.format("Couldn't parse sequence in section:%s row:%d elem:%d\n%s",
                                    section.name, chan_seq_index, index_elem, chunk_steps), ed)
                            else -- save them
                                for c, st in ipairs(chunk_steps) do
                                    ut.table_add(_steps, st.tick, st)
                                    _length = _length +1
                                end
                                tick = tick + seq_length
                            end
                        end
                    end
                end

                -- Keep track of overall time for section.
                section_max = math.max(section_max, tick)
            -- else just blow it up??
            --     error(string.format("Element:%s in section:%s is not a valid channel", tostring(v)), ed)
            end
        end

        -- Keep track of overall time.
        _length = _length + section_max
    end

    -- All done.
end

-----------------------------------------------------------------------------
--- Parse a chunk pattern. Global for unit testing.
--- This does not call error() so caller can process in context. However this results in a somewhat messy
--- multiple early return scenario. Sorry.
-- @param chunk like: { "|5-------|--      |        |        |7-------|--      |        |        |", "G4.m7" }
-- @param chan_hnd
-- @param start_tick
-- @return seq_length, steps - length, list of StepX OR 0, error message if invalid.
function M.parse_chunk(chunk, chan_hnd, start_tick)
    if #chunk ~= 2 then
        return 0, "Improperly formed chunk."
    end

    local steps = { }
    local current_vol = 0 -- 0 -> 9
    local start_offset = 0 -- offset in pattern for the start of the current event
    -- local chunk_error = nil

    -- Process the note descriptor first. Could be number, string, function.
    local what_to_play = chunk[2]
    local tn = type(what_to_play)
    local notes = {}
    local func = nil

    ----- Local function to package an event. ------
    function make_event(offset)
        -- offset is 0-based.
        -- returns nil if ok else error string.

        -- scale volume
        local vol = _vol_map[current_vol]
        local dur = offset - start_offset
        local when = start_offset + start_tick
        local evt_err = nil

        if func ~= nil then -- func
            local si = StepFunction(when, chan_hnd, func, vol)
            if si.err == nil then
                table.insert(steps, si)
            else
                -- Syntax error
                evt_err = si.err
            end
        else -- note
            for _, n in ipairs(notes) do
                local si = StepNote(when, chan_hnd, n, vol, dur)
                if si.err == nil then
                    table.insert(steps, si)
                else
                    -- Syntax error
                    evt_err = si.err
                end
            end
        end

        return evt_err
    end

    -- Start here
    if tn == "number" then
        -- use as is
        notes = { what_to_play }
    elseif tn == "function" then
        -- use as is
        func = what_to_play
        if func == nil then
            return 0, string.format("Invalid func %s", chunk[2])
        end
    elseif tn == "string" then
        notes = mus.get_notes_from_string(what_to_play)
        if notes == nil then
            return 0, string.format("Invalid notes %s", chunk[2])
        end
    else
        return 0, string.format("Invalid what_to_play %s", chunk[2])
    end

    -- Process note instances.
    -- Remove visual markers from pattern.
    local pattern = string.gsub(chunk[1], "|", "")
    local seq_length = #pattern

    for i = 1, seq_length do
        local c = pattern:sub(i, i)
        local vol_num = string.byte(c) - 48 -- '0'
        local seq_err = nil

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
                seq_err = make_event(i - 1)
            end
            -- Start new note.
            current_vol = vol_num
            start_offset = i - 1
        elseif c == ' ' or c == '.' then
            -- No sound.
            -- Do we need to end the current note?
            if current_vol > 0 then
                seq_err = make_event(i - 1)
            end
            current_vol = 0
        else
            -- Invalid char.
            seq_err = string.format("Invalid char %c in pattern string: %s", c, chunk[1])
        end

        if seq_err ~= nil then return 0, seq_err end
    end

    -- Straggler?
    if current_vol > 0 then
        local seq_err = make_event(#pattern - 1)
        if seq_err ~= nil then return 0, seq_err end
    end

    return seq_length, steps
end

-----------------------------------------------------------------------------
--- Parsing: start a new section definition.
-- @param name what to call it
function M.sect_start(name)
    _current_section = {}
    _current_section.name = name
    table.insert(_sections, _current_section)
end

-----------------------------------------------------------------------------
--- Parsing: add sequences to the current section.
-- @param chan_hnd the channel
-- @param ... the sequences
function M.sect_seqs(chan_hnd, ...)
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
        error("No section name", 2)
    end
end

-----------------------------------------------------------------------------
--- Diagnostic.
-- @param chan_hnd the channel
-- @param ... the sequences
function M.dump()
    fp = io.open('C:\\Dev\\repos\\Lua\\Nebulua\\test\\_dump.txt', 'w+')
        -- 00000 01-02 NOTE 67 0.4 10
        -- 00000 01-02 CTRL 70 101

    for tick, sts in pairs(_steps) do
        for i, st in ipairs(sts) do
            fp:write(string.format("%s\n", st.format()))
            -- fp:write(string.format("%d %s\n", tick, st.format()))
            -- fp:write(string.format("%d %s\n", tick, ut.dump_table_string(st, true, "X")))
        end
    end
    fp:close()
end


-----------------------------------------------------------------------------
-- Return module.
return M
