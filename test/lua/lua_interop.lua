
-- Mock the C lua_interop functions for testing.

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

local function format_chan_hnd(chan_hnd)
    local s = string.format("%02X-%02X", (chan_hnd >> 8) & 0xFF, chan_hnd & 0xFF)
    return s
end

-----------------------------------------------------------

function M.log(level, msg)
    s = string.format("log: level:%d msg:%s", level, msg)
    capture(s)
    -- print(s) -- make optional?
    return 0
end


function M.create_input_channel(dev_name, chan_num)
    capture(string.format("create_input_channel: dev_name:%s chan_num:%d", dev_name, chan_num))
    local dev_index = 1 -- lower half
    return ((dev_index << 8) | (chan_num))
end


function M.create_output_channel(dev_name, chan_num, patch)
    capture(string.format("create_output_channel: dev_name:%s chan_num:%d patch:%d", dev_name, chan_num, patch))
    local dev_index = 7 -- upper half
    return ((dev_index << 8) | (chan_num))
end


function M.set_tempo(bpm)
    capture(string.format("set_tempo: %d", bpm))
    return 0
end


function M.send_note(chan_hnd, note_num, volume)
    -- If volume is 0 note_off else note_on.
    capture(string.format("send_note: chan_hnd:%s note_num:%d volume:%0.1f", format_chan_hnd(chan_hnd), note_num, volume))
    return 0
end


function M.send_controller(chan_hnd, controller, value)
    capture(string.format("send_controller: chan_hnd:%s controller:%d value:%d", format_chan_hnd(chan_hnd), controller, value))
    return 0
end


-- Return the module.
return M
