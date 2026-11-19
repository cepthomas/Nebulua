
-- Mock the C luainterop functions for testing.

-- Create the namespace/module.
local M = {}

-- Script api calls.
M.activity = {}

-- Current tick for synchronizing data collection.
M.current_tick = 0


local function capture(msg)
    table.insert(M.activity, string.format("%05d %s", M.current_tick, msg))
    -- M.current_tick = M.current_tick + 1
end

local function format_nid(nid)
    local s = string.format("%02X-%02X", (nid >> 4) & 0x0F, nid & 0x0F)
    -- local s = string.format("%02X-%02X", (nid >> 8) & 0xFF, nid & 0xFF)
    return s
end

-----------------------------------------------------------

function M.log(level, msg)
    local s = string.format("log: level:%d msg:%s", level, msg)
    capture(s)
    -- print(s) -- make optional?
    return 0
end


function M.open_midi_input(dev_name, chan_num, chan_name)
    capture(string.format("open_midi_input: dev_name:%s chan_num:%d chan_name:%s", dev_name, chan_num, chan_name))
    local dev_index = 1 -- lower half
    return ((dev_index << 8) | (chan_num))
end


function M.open_midi_output(dev_name, chan_num, chan_name, patch)
    capture(string.format("open_midi_output: dev_name:%s chan_num:%d chan_name:%s patch:%d", dev_name, chan_num, chan_name, patch))
    local dev_index = 7 -- upper half
    return ((dev_index << 8) | (chan_num))
end


function M.set_tempo(bpm)
    capture(string.format("set_tempo: %d", bpm))
    return 0
end


function M.send_note(nid, note_num, volume)
    -- If volume is 0 note_off else note_on.
    capture(string.format("send_note: nid:%s note_num:%d volume:%0.1f", format_nid(nid), note_num, volume))
    return 0
end


function M.send_controller(nid, controller, value)
    capture(string.format("send_controller: nid:%s controller:%d value:%d", format_nid(nid), controller, value))
    return 0
end


-- Return the module.
return M
