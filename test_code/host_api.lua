
--  mock the real C host_api component.

-- Create the namespace/module.
local M = {}

-- api requests.
local activity = {}


-- #define MAKE_HANDLE(dev_index, chan_num) ((dev_index << 8) | (chan_num))
-- #define GET_DEV_INDEX(chan_hnd) ((chan_hnd >> 8) & 0xFF)
-- #define GET_CHAN_NUM(chan_hnd) (chan_hnd & 0xFF)


-- Script wants to log something.
-- @param[in] l Internal lua state.
-- @param[in] level Log level
-- @param[in] msg Log message
-- @return LUA_STATUS
function M.log(level, msg)
    table.concat(activity, string.format("log: %d %s", level, msg))
    return 0
end


-- Create an input midi channel.
-- @param[in] l Internal lua state.
-- @param[in] dev_name Midi device name
-- @param[in] chan_num Midi channel number 1-16
-- @return Channel handle or 0 if invalid
function M.create_input_channel(dev_name, chan_num)
    table.concat(activity, string.format("create_input_channel: %s %d", dev_name, chan_num))
    local dev_index = 0 -- lower half
    return ((dev_index << 8) | (chan_num))
end


-- Create an output midi channel.
-- @param[in] l Internal lua state.
-- @param[in] dev_name Midi device name
-- @param[in] chan_num Midi channel number 1-16
-- @param[in] patch Midi patch number
-- @return Channel handle or 0 if invalid
function M.create_output_channel(dev_name, chan_num, patch)
    table.concat(activity, string.format("create_output_channel: %s %d %d ", dev_name, chan_num, patch))
    local dev_index = 8 -- upper half
    return ((dev_index << 8) | (chan_num))
end


-- Script wants to change tempo.
-- @param[in] l Internal lua state.
-- @param[in] bpm BPM
-- @return LUA_STATUS
function M.set_tempo(bpm)
    table.concat(activity, string.format("set_tempo: %d", bpm))
    return 0
end


-- If volume is 0 note_off else note_on. If dur is 0 send note_on with dur = 0.1 (for drum/hit).
-- @param[in] l Internal lua state.
-- @param[in] chan_hnd Output channel handle
-- @param[in] note_num Note number
-- @param[in] volume Volume between 0.0 and 1.0
-- @param[in] dur Duration in subbeats
-- @return LUA_STATUS
function M.send_note(chan_hnd, note_num, volume, dur)
    table.concat(activity, string.format("send_note: %d %d %f %d", chan_hnd, note_num, volume, dur))
    return 0
end


-- Send a controller immediately.
-- @param[in] l Internal lua state.
-- @param[in] chan_hnd Output channel handle
-- @param[in] controller Specific controller
-- @param[in] value Payload.
-- @return LUA_STATUS
-- int luainteropwork_SendController(lua_State* l, int chan_hnd, int controller, int value);
function M.send_controller(chan_hnd, controller, value)
    table.concat(activity, string.format("send_controller: %d %d %d", chan_hnd, controller, value))
    return 0
end


-- Return the module.
return M
