-- Specifies the generated interop code for Nebulua.

local M = {}

M.config =
{
    lua_lib_name = "luainterop",    -- for require
    class_name = "Interop",         -- host filenames
    namespace = "Script"            -- host namespace
}

------------------------ Host => Script ------------------------
M.script_funcs =
{
    {
        lua_func_name = "setup",
        host_func_name = "Setup",
        required = "true",
        description = "Call to initialize Nebulator and composition.",
        -- no args
        ret = { type = "S", description = "Script meta info if composition" }
    },

    {
        lua_func_name = "step",
        host_func_name = "Step",
        required = "true",
        description = "Called every fast timer increment aka tick.",
        args =
        {
            { name = "tick", type = "I", description = "Current tick 0 => N" },
        },
        ret = { type = "I", description = "Unused" }
    },

    {
        lua_func_name = "rcv_note",
        host_func_name = "RcvNote",
        required = "false",
        description = "Called when midi input arrives.",
        args =
        {
            { name = "chan_hnd", type = "I", description = "Input channel handle" },
            { name = "note_num", type = "I", description = "Note number 0 => 127" },
            { name = "volume", type = "N", description = "Volume 0.0 => 1.0" },
        },
        ret = { type = "I", description = "Unused" }
    },

    {
        lua_func_name = "rcv_controller",
        host_func_name = "RcvController",
        required = "false",
        description = "Called when midi input arrives.",
        args =
        {
            { name = "chan_hnd", type = "I", description = "Input channel handle" },
            { name = "controller", type = "I", description = "Specific controller id 0 => 127" },
            { name = "value", type = "I", description = "Payload 0 => 127" },
        },
        ret = { type = "I", description = "Unused" }
    },

    {
        lua_func_name = "neb_command",
        host_func_name = "NebCommand",
        required = "true",
        description = "Host calls arbitrary lua function. For internal use only.",
        args =
        {
            { name = "cmd", type = "S", description = "Specific command" },
            { name = "arg", type = "S", description = "Optional argument" },
        },
        ret = { type = "S", description = "Script return" }
    },
}

------------------------ Script => Host ------------------------
M.host_funcs =
{
    {
        lua_func_name = "create_output_channel",
        host_func_name = "CreateOutputChannel",
        description = "Create an output midi channel.",
        args =
        {
            { name = "dev_name", type = "S", description = "Midi device name" },
            { name = "chan_num", type = "I", description = "Midi channel number 1 => 16" },
            { name = "patch",    type = "I", description = "Midi patch number 0 => 127" },
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
            { name = "chan_num", type = "I", description = "Midi channel number 1 => 16" },
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
        ret = { type = "I", description = "Unused" }
    },

    {
        lua_func_name = "set_tempo",
        host_func_name = "SetTempo",
        description = "Script wants to change tempo.",
        args =
        {
            { name = "bpm", type = "I", description = "BPM 40 => 240" },
        },
        ret = { type = "I", description = "Unused" }
    },

    {
        lua_func_name = "send_note",
        host_func_name = "SendNote",
        description = "If volume is 0 note_off else note_on. If dur is 0 send note_on with dur = 1 (min for drum/hit).",
        args =
        {
            { name = "chan_hnd", type = "I", description = "Output channel handle" },
            { name = "note_num", type = "I", description = "Note number" },
            { name = "volume", type = "N", description = "Volume 0.0 => 1.0" },
        },
        ret = { type = "I", description = "Unused" }
    },

    {
        lua_func_name = "send_controller",
        host_func_name = "SendController",
        description = "Send a controller immediately.",
        args =
        {
            { name = "chan_hnd", type = "I", description = "Output channel handle" },
            { name = "controller", type = "I", description = "Specific controller 0 => 127" },
            { name = "value", type = "I", description = "Payload 0 => 127" },
        },
        ret = { type = "I", description = "Unused" }
    },
}

return M
