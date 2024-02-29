
--  Mock the C lua_interop functions for testing.

-- Create the namespace/module.
local M = {}

-- Script api calls.
M.activity = {}

-- Current tick for synchronizing data collection.
M.current_tick = 0


local function capture(msg)
    table.insert(M.activity, string.format("%05d %s", M.current_tick, msg))
    M.current_tick = M.current_tick + 1
end


-----------------------------------------------------------

function M.log(level, msg)
    capture(string.format("log: %d %s", level, msg))
    return 0
end


function M.create_input_channel(dev_name, chan_num)
    capture(string.format("create_input_channel: %s %d", dev_name, chan_num))
    local dev_index = 1 -- lower half
    return ((dev_index << 8) | (chan_num))
end


function M.create_output_channel(dev_name, chan_num, patch)
    capture(string.format("create_output_channel: %s %d %d", dev_name, chan_num, patch))
    local dev_index = 10 -- upper half
    return ((dev_index << 8) | (chan_num))
end


function M.set_tempo(bpm)
    capture(string.format("set_tempo: %d", bpm))
    return 0
end


function M.send_note(chan_hnd, note_num, volume)
    -- If volume is 0 note_off else note_on.
    capture(string.format("send_note: %d %d %0.1f", chan_hnd, note_num, volume))
    return 0
end


function M.send_controller(chan_hnd, controller, value)
    capture(string.format("send_controller: %d %d %d", chan_hnd, controller, value))
    return 0
end


-- Return the module.
return M
