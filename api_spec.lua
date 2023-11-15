
local M = {}

M.config =
{
    lua_lib_name = "api_lib",
    namespace = "Ephemera.Nebulua",
    class = "Script",
    add_refs = { "System.Diagnostics", },
}



        -- #region C# calls lua functions - TODO1
        -- // public void Setup()
        -- // public void Step(int bar, int beat, int subbeat)
        -- // public void InputNote(string channelName, int note, int vel)
        -- // public void InputController(string channelName, int controller, int value)



-- Host calls lua.
M.lua_export_funcs =
{
    {
        lua_func_name = "my_lua_func",
        host_func_name = "MyLuaFunc",
        description = "Tell me something good.",
        args =
        {
            {
                name = "arg_one",
                type = "S",
                description = "some strings"
            },
            {
                name = "arg_two",
                type = "I",
                description = "a nice integer"
            },
            {
                name = "arg_three",
                type = "T",
            },
        },
        ret =
        {
            type = "T",
            description = "a returned thing"
        }
    },
    {
        lua_func_name = "my_lua_func2",
        host_func_name = "MyLuaFunc2",
        description = "wooga wooga",
        args =
        {
            {
                name = "arg_one",
                type = "B",
                description = "aaa bbb ccc"
            },
        },
         ret =
        {
            type = "N",
            description = "a returned number"
        }
    },
    {
        lua_func_name = "no_args_func",
        host_func_name = "NoArgsFunc",
        description = "no_args",
        ret =
        {
            type = "N",
            description = "a returned number"
        },
    },
}




-- Lua calls host.
M.host_export_funcs =
{
    {
        -- static int SendNoteWork(string channel, int note_num, double volume, double dur)
        lua_func_name = "send_note",
        host_func_name = "SendNote",
        description = "If volume is 0 note_off else note_on. If dur is 0 dur = note_on with dur = 0.1 (for drum/hit).",
        args =
        {
            {
                name = "channel",
                type = "S",
                description = "Channel name."
            },
            {
                name = "note_num",
                type = "I",
                description = "Note number."
            },
            {
                name = "volume",
                type = "N",
                description = "Volume between 0.0 and 1.0."
            },
            {
                name = "dur",
                type = "N",
                description = "Duration as bar.beat."
            },
        },
        ret = { type = "B", description = "Required empty." }
    },

    {
        -- static int SendNoteOnWork(string channel, int note_num, double volume)
        lua_func_name = "send_note_on",
        host_func_name = "SendNoteOn",
        description = "Explicit note on.",
        args =
        {
            {
                name = "channel",
                type = "S",
                description = "Channel name."
            },
            {
                name = "note_num",
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
        -- static int SendNoteOffWork(string channel, int note_num)
        lua_func_name = "send_note_off",
        host_func_name = "SendNoteOff",
        description = "Explicit note off.",
        args =
        {
            {
                name = "channel",
                type = "S",
                description = "Channel name."
            },
            {
                name = "note_num",
                type = "I",
                description = "Note number."
            },
        },
        ret = { type = "B", description = "Required empty." }
    },

    {
        -- static int SendControllerWork(string channel, int ctlr, int value)
        lua_func_name = "send_controller",
        host_func_name = "SendController",
        description = "Explicit note on.",
        args =
        {
            {
                name = "channel",
                type = "S",
                description = "Channel name."
            },
            {
                name = "ctlr",
                type = "I",
                description = "Specific controller."
            },
            {
                name = "value",
                type = "I",
                description = "Specific value."
            },
        },
        ret = { type = "B", description = "Required empty." }
    },

    {
        -- static int SendPatchWork(string channel, int patch)
        lua_func_name = "send_patch",
        host_func_name = "SendPatch",
        description = "Send patch now.",
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

}

return M
