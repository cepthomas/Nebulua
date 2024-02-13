
local M = {}

M.config =
{
    lua_lib_name = "host_api",
    -- add_refs = { "<abc.h>", },
}

-- Host calls lua.
M.lua_export_funcs =
{
    {
        lua_func_name = "setup",
        host_func_name = "Setup",
        description = "Called to initialize Nebulator stuff.",
        -- no args
        ret = { type = "I", description = "LUA_STATUS" }
    },

    {
        lua_func_name = "step",
        host_func_name = "Step",
        description = "Called every fast timer increment aka tick.",
        args =
        {
            { name = "tick", type = "I", description = "Current tick 0-N" },
        },
        ret = { type = "I", description = "LUA_STATUS" }
    },

    {
        lua_func_name = "input_note",
        host_func_name = "InputNote",
        description = "Called when input arrives.",
        args =
        {
            { name = "chan_hnd", type = "I", description = "Input channel handle" },
            { name = "note_num", type = "I", description = "Note number 0-MIDI_MAX" },
            { name = "volume", type = "N", description = "Volume between 0.0 and 1.0" },
        },
        ret = { type = "I", description = "LUA_STATUS" }
    },

    {
        lua_func_name = "input_controller",
        host_func_name = "InputController",
        description = "Called when input arrives.",
        args =
        {
            { name = "chan_hnd", type = "I", description = "Input channel handle" },
            { name = "controller", type = "I", description = "Specific controller id 0-MIDI_MAX" },
            { name = "value", type = "I", description = "Payload 0-MIDI_MAX" },
        },
        ret = { type = "I", description = "LUA_STATUS" }
    },

}

-- Lua calls host.
M.host_export_funcs =
{

    {
        lua_func_name = "create_output_channel",
        host_func_name = "CreateOutputChannel",
        description = "Create an output midi channel.",
        args =
        {
            { name = "dev_name", type = "S", description = "Midi device name" },
            { name = "chan_num", type = "I", description = "Midi channel number 1-16" },
            { name = "patch",    type = "I", description = "Midi patch number 0-MIDI_MAX" },
        },
        ret = { type = "I", description = "Channel handle or 0 if invalid" }
    },

    {
        lua_func_name = "create_input_channel",
        host_func_name = "CreateInputChannel",
        description = "Create an input midi channel.",
        args =
        {
            { name = "dev_name", type = "S", description = "Midi device name" },
            { name = "chan_num", type = "I", description = "Midi channel number 1-16" },
        },
        ret = { type = "I", description = "Channel handle or 0 if invalid" }
    },

    {
        lua_func_name = "log",
        host_func_name = "Log",
        description = "Script wants to log something.",
        args =
        {
            { name = "level", type = "I", description = "Log level" },
            { name = "msg", type = "S", description = "Log message" },
        },
        ret = { type = "I", description = "LUA_STATUS" }
    },

    {
        lua_func_name = "set_tempo",
        host_func_name = "SetTempo",
        description = "Script wants to change tempo.",
        args =
        {
            { name = "bpm", type = "I", description = "BPM 40-240" },
        },
        ret = { type = "I", description = "LUA_STATUS" }
    },

    {
        lua_func_name = "send_note",
        host_func_name = "SendNote",
        description = "If volume is 0 note_off else note_on. If dur is 0 send note_on with dur = 1 (for drum/hit).",
        args =
        {
            { name = "chan_hnd", type = "I", description = "Output channel handle" },
            { name = "note_num", type = "I", description = "Note number" },
            { name = "volume", type = "N", description = "Volume between 0.0 and 1.0" },
        },
        ret = { type = "I", description = "LUA_STATUS" }
    },

    {
        lua_func_name = "send_controller",
        host_func_name = "SendController",
        description = "Send a controller immediately.",
        args =
        {
            { name = "chan_hnd", type = "I", description = "Output channel handle" },
            { name = "controller", type = "I", description = "Specific controller 0-MIDI_MAX" },
            { name = "value", type = "I", description = "Payload 0-MIDI_MAX" },
        },
        ret = { type = "I", description = "LUA_STATUS" }
    },
}

return M
