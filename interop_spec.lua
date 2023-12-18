
local M = {}

M.config =
{
    lua_lib_name = "nebulua_api",
    -- add_refs = { "<abc.h>", },
}


--[[ List of midi events
note:
name = "channel", S or I
name = "notenum", I
name = "volume", N 0 note_off else note_on
name = "dur", N 0 dur = note_on with dur = 0.1

controller:
name = "channel", S or I
name = "ctlr", I
name = "value", I
]]



-- Host calls lua.
M.lua_export_funcs =
{
    {
        lua_func_name = "setup",
        host_func_name = "Setup",
        description = "Called to initialize Nebulator stuff.",
        ret = { type = "I", description = "Status." }
    },

    {
        lua_func_name = "step",
        host_func_name = "Step",
        description = "Called every mmtimer increment.",
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
        ret = { type = "I", description = "Status." }
    },

    {
        lua_func_name = "input_note",
        host_func_name = "InputNote",
        description = "Called when input arrives. Optional.",
        args =
        {
            {
                name = "channel",
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
            -- {
            --     name = "velocity",
            --     type = "I",
            --     description = "Note velocity"
            -- },
        },
        ret = { type = "I", description = "Status." }
    },

    {
        lua_func_name = "input_controller",
        host_func_name = "InputController",
        description = "Called when input arrives. Optional.",
        args =
        {
            {
                name = "channel",
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
        ret = { type = "I", description = "Status." }
    },

}

-- Lua calls host.
M.host_export_funcs =
{

    {
        lua_func_name = "log",
        host_func_name = "Log",
        description = "Script wants to log something.",
        args =
        {
            {
                name = "level",
                type = "I",
                description = "Log level."
            },
            {
                name = "msg",
                type = "S",
                description = "Log message."
            },
        },
        ret = { type = "I", description = "Status." }
    },

    {
        lua_func_name = "send_note",
        host_func_name = "SendNote",
        description = "If volume is 0 note_off else note_on. If dur is 0 dur = note_on with dur = 0.1 (for drum/hit).",
        args =
        {
            {
                name = "channel",
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
        ret = { type = "I", description = "Status." }
    },

    {
        lua_func_name = "send_controller",
        host_func_name = "SendController",
        description = "Send a controller immediately.",
        args =
        {
            {
                name = "channel",
                type = "I",
                description = "Output channel handle"
            },
            {
                name = "ctlr",
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


--[[
    {
        lua_func_name = "send_note_on",
        host_func_name = "SendNoteOn",
        description = "Send an explicit note on immediately. Caller is responsible for sending note off later.",
        args =
        {
            {
                name = "channel",
                type = "S",
                description = "Channel name."
            },
            {
                name = "notenum",
                type = "I",
                description = "Note number."
            },
            {
                name = "volume",
                type = "N",
                description = "Volume between 0.0 and 1.0."
            },
        },
        ret = { type = "B", description = "Required empty." }
    },

    {
        lua_func_name = "send_note_off",
        host_func_name = "SendNoteOff",
        description = "Send an explicit note off immediately.",
        args =
        {
            {
                name = "channel",
                type = "S",
                description = "Channel name."
            },
            {
                name = "notenum",
                type = "I",
                description = "Note number."
            },
        },
        ret = { type = "B", description = "Required empty." }
    },

    {
        lua_func_name = "send_patch",
        host_func_name = "SendPatch",
        description = "Send a midi patch immediately.",
        args =
        {
            {
                name = "channel",
                type = "S",
                description = "Channel name."
            },
            {
                name = "patch",
                type = "I",
                description = "Specific patch."
            },
        },
        ret = { type = "B", description = "Required empty." }
    },
]]
}

return M
