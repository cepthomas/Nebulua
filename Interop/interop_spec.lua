-- Specifies the generated interop code for Nebulua.

local M = {}

M.config =
{
    lua_lib_name = "luainterop",    -- for require
    class_name = "Interop",         -- host filenames
}

------------------------ Host => Script ------------------------
M.script_funcs =
{
    {
        lua_func_name = "setup",
        host_func_name = "Setup",
        description = "Called to initialize script.",
        -- no args
        ret = { type = "S", description = "Script meta info if composition" }
    },

    {
        lua_func_name = "step",
        host_func_name = "Step",
        description = "Called every fast timer increment aka tick.",
        args =
        {
            { name = "tick", type = "I", description = "Current tick 0 => N" },
        },
        ret = { type = "I", description = "Unused" }
    },

    {
        lua_func_name = "receive_midi_note",
        host_func_name = "ReceiveMidiNote",
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
        lua_func_name = "receive_midi_controller",
        host_func_name = "ReceiveMidiController",
        description = "Called when midi input arrives.",
        args =
        {
            { name = "chan_hnd", type = "I", description = "Input channel handle" },
            { name = "controller", type = "I", description = "Specific controller id 0 => 127" },
            { name = "value", type = "I", description = "Payload 0 => 127" },
        },
        ret = { type = "I", description = "Unused" }
    },
}

------------------------ Script => Host ------------------------
M.host_funcs =
{
    {
        lua_func_name = "open_midi_output",
        host_func_name = "OpenMidiOutput",
        description = "Open a midi output channel.",
        args =
        {
            { name = "dev_name",  type = "S", description = "Midi device name" },
            { name = "chan_num",  type = "I", description = "Midi channel number 1 => 16" },
            { name = "chan_name", type = "S", description = "User channel name" },
            { name = "patch",     type = "I", description = "Midi patch number 0 => 127" },
        },
        ret = { type = "I", description = "Channel handle or 0 if invalid" }
    },

    {
        lua_func_name = "open_midi_input",
        host_func_name = "OpenMidiInput",
        description = "Open a midi input channel.",
        args =
        {
            { name = "dev_name",  type = "S", description = "Midi device name" },
            { name = "chan_num",  type = "I", description = "Midi channel number 1 => 16 or 0 => all" },
            { name = "chan_name", type = "S", description = "User channel name" },
        },
        ret = { type = "I", description = "Channel handle or 0 if invalid" }
    },

    {
        lua_func_name = "send_midi_note",
        host_func_name = "SendMidiNote",
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
        lua_func_name = "send_midi_controller",
        host_func_name = "SendMidiController",
        description = "Send a controller immediately.",
        args =
        {
            { name = "chan_hnd", type = "I", description = "Output channel handle" },
            { name = "controller", type = "I", description = "Specific controller 0 => 127" },
            { name = "value", type = "I", description = "Payload 0 => 127" },
        },
        ret = { type = "I", description = "Unused" }
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
}

return M
