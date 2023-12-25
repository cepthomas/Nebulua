
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
        ret = { type = "I", description = "Status TODOE" }
    },

    {
        lua_func_name = "step",
        host_func_name = "Step",
        description = "Called every fast timer increment.",
        args =
        {
            {
                name = "bar",
                type = "I",
                description = "Which bar"
            },
            {
                name = "beat",
                type = "I",
                description = "Which beat"
            },
            {
                name = "subbeat",
                type = "I",
                description = "Which subbeat"
            },
        },
        ret = { type = "I", description = "Status" }
    },

    {
        lua_func_name = "input_note",
        host_func_name = "InputNote",
        description = "Called when input arrives.",
        args =
        {
            {
                name = "hndchan",
                type = "I",
                description = "Input channel handle"
            },
            {
                name = "notenum",
                type = "I",
                description = "Note number"
            },
            {
                name = "volume",
                type = "N",
                description = "Volume between 0.0 and 1.0."
            },
        },
        ret = { type = "I", description = "Status" }
    },

    {
        lua_func_name = "input_controller",
        host_func_name = "InputController",
        description = "Called when input arrives.",
        args =
        {
            {
                name = "hndchan",
                type = "I",
                description = "Input channel handle"
            },
            {
                name = "controller",
                type = "I",
                description = "Specific controller id"
            },
            {
                name = "value",
                type = "I",
                description = "Payload"
            },
        },
        ret = { type = "I", description = "Status" }
    },

}

-- Lua calls host.
M.host_export_funcs =
{

    {
        lua_func_name = "create_channel",
        host_func_name = "CreateChannel",
        description = "Create an in or out midi channel.",
        args =
        {
            {
                name = "device",
                type = "S",
                description = "Midi device name"
            },
            {
                name = "channum",
                type = "I",
                description = "Midi channel number 1-16"
            },
            {
                name = "patch",
                type = "I",
                description = "Midi patch number (output channel only)"
            },
        },
        ret = { type = "I", description = "Channel handle or 0 if invalid" }
    },

    {
        lua_func_name = "log",
        host_func_name = "Log",
        description = "Script wants to log something.",
        args =
        {
            {
                name = "level",
                type = "I",
                description = "Log level"
            },
            {
                name = "msg",
                type = "S",
                description = "Log message"
            },
        },
        ret = { type = "I", description = "Status" }
    },

    {
        lua_func_name = "set_tempo",
        host_func_name = "SetTempo",
        description = "Script wants to change tempo.",
        args =
        {
            {
                name = "bpm",
                type = "I",
                description = "BPM"
            },
        },
        ret = { type = "I", description = "Status" }
    },

    {
        lua_func_name = "send_note",
        host_func_name = "SendNote",
        description = "If volume is 0 note_off else note_on. If dur is 0 send note_on with dur = 0.1 (for drum/hit).",
        args =
        {
            {
                name = "hndchan",
                type = "I",
                description = "Output channel handle"
            },
            {
                name = "notenum",
                type = "I",
                description = "Note number"
            },
            {
                name = "volume",
                type = "N",
                description = "Volume between 0.0 and 1.0"
            },
            {
                name = "dur",
                type = "N",
                description = "Duration as bar.beat"
            },
        },
        ret = { type = "I", description = "Status" }
    },

    {
        lua_func_name = "send_controller",
        host_func_name = "SendController",
        description = "Send a controller immediately.",
        args =
        {
            {
                name = "hndchan",
                type = "I",
                description = "Output channel handle"
            },
            {
                name = "controller",
                type = "I",
                description = "Specific controller"
            },
            {
                name = "value",
                type = "I",
                description = "Payload."
            },
        },
        ret = { type = "I", description = "Status" }
    },

}

return M
