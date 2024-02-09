
--  mock the real C host_api component.

-- Create the namespace/module.
local M = {}


-- Script wants to log something.
-- @param[in] l Internal lua state.
-- @param[in] level Log level
-- @param[in] msg Log message
-- @return LUA_STATUS
-- int luainteropwork_Log(lua_State* l, int level, const char* msg);
function M.log(level, msg)

end


-- Create an output midi channel.
-- @param[in] l Internal lua state.
-- @param[in] device Midi device name
-- @param[in] channum Midi channel number 1-16
-- @param[in] patch Midi patch number
-- @return Channel handle or 0 if invalid
-- int luainteropwork_CreateOutputChannel(lua_State* l, const char* device, int channum, int patch);
function M.create_output_channel(device, channum, patch)

end


-- Create an input midi channel.
-- @param[in] l Internal lua state.
-- @param[in] device Midi device name
-- @param[in] channum Midi channel number 1-16
-- @return Channel handle or 0 if invalid
-- int luainteropwork_CreateInputChannel(lua_State* l, const char* device, int channum);
function M.create_input_channel(device, channum)

end


-- Script wants to change tempo.
-- @param[in] l Internal lua state.
-- @param[in] bpm BPM
-- @return LUA_STATUS
-- int luainteropwork_SetTempo(lua_State* l, int bpm);
function M.set_tempo(bpm)

end


-- If volume is 0 note_off else note_on. If dur is 0 send note_on with dur = 0.1 (for drum/hit).
-- @param[in] l Internal lua state.
-- @param[in] chan_hnd Output channel handle
-- @param[in] note_num Note number
-- @param[in] volume Volume between 0.0 and 1.0
-- @param[in] dur Duration as bar.beat
-- @return LUA_STATUS
-- int luainteropwork_SendNote(lua_State* l, int chan_hnd, int note_num, double volume, double dur);
function M.send_note(chan_hnd, note_num, volume, dur)

end


-- Send a controller immediately.
-- @param[in] l Internal lua state.
-- @param[in] chan_hnd Output channel handle
-- @param[in] controller Specific controller
-- @param[in] value Payload.
-- @return LUA_STATUS
-- int luainteropwork_SendController(lua_State* l, int chan_hnd, int controller, int value);
function M.send_controller(chan_hnd, controller, value)

end


-- Return the module.
return M
