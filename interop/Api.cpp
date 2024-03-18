#include "lua.hpp"
#include "nebcommon.h"
#include "luainterop.h"
#include "Api.h"

using namespace System;
using namespace System::Collections::Generic;


// The main Lua thread. This pointless struct decl makes a warning go away per https://github.com/openssl/openssl/issues/6166.
struct lua_State {};
static lua_State* _l;


#pragma region Lifecycle
//--------------------------------------------------------//
int Interop::Api::Init()
{
    int stat = NEB_OK;

    // Init internal lib.
    _l = luaL_newstate();
    Error = gcnew String("");
    SectionInfo = gcnew Dictionary<int, String^>();

    // Load std libraries.
    luaL_openlibs(_l);

    // Load host funcs into lua space. This table gets pushed on the stack and into globals.
    luainterop_Load(_l);

    // Pop the table off the stack as it interferes with calling the module functions.
    lua_pop(_l, 1);

    return stat;
}

//--------------------------------------------------------//
Interop::Api::~Api()
{
    int stat = NEB_OK;

    // Finished. Clean up resources and go home.
    lua_close(_l);
}
#pragma endregion


#pragma region Host calls lua script
//--------------------------------------------------------//
int Interop::Api::OpenScript(String^ fn)
{
    int stat = NEB_OK;
    int ret = 0;

    char fnx[MAX_PATH];
    ToCString(fn, fnx, MAX_PATH);

    ///// Load the script /////

    if (stat == NEB_OK)
    {
        // Load/compile the script file. Pushes the compiled chunk as a Lua function on top of the stack or pushes an error message.
        stat = luaL_loadfile(_l, fnx);
        if (stat != NEB_OK)
        {
            Error = gcnew String(nebcommon_EvalStatus(_l, stat, "Load script file failed."));
        }
    }

    if (stat == NEB_OK)
    {
        // Execute the script to initialize it. This catches runtime syntax errors.
        stat = lua_pcall(_l, 0, LUA_MULTRET, 0);
        if (stat != NEB_OK)
        {
            Error = gcnew String(nebcommon_EvalStatus(_l, stat, "Execute script failed."));
        }
    }

    ///// Run the script /////

    if (stat == NEB_OK)
    {
        luainterop_Setup(_l);
        if (luainterop_Error() != NULL)
        {
            Error = gcnew String(luainterop_Error());
            stat = NEB_ERR_SYNTAX;
        }
    }

    if (stat == NEB_OK)
    {
        // Get script info.

        // Get length.
        int ltype = lua_getglobal(_l, "_length");
        int length = (int)lua_tointeger(_l, -1);
        lua_pop(_l, 1); // Clean up stack.

        // Get section info.
        //memset(_section_descs, 0, sizeof(_section_descs));
        ltype = lua_getglobal(_l, "_section_names");
        lua_pushnil(_l);
        while (lua_next(_l, -2) != 0 && stat == NEB_OK)
        {
            SectionInfo->Add((int)lua_tointeger(_l, -1), ToCliString(lua_tostring(_l, -2)));
            lua_pop(_l, 1);
        }
        lua_pop(_l, 1); // Clean up stack.

        // Tack on the overal length.
        SectionInfo->Add(length, ToCliString("LENGTH=========="));
    }

    return stat;
}

//--------------------------------------------------------//
bool Interop::Api::Step(int tick)
{
    luainterop_Step(_l, tick);
    if (luainterop_Error() != NULL)
    {
        Error = gcnew String(luainterop_Error());
        return true;
    }
    return false;
}

//--------------------------------------------------------//
bool Interop::Api::InputNote(int chan_hnd, int note_num, double volume)
{
    luainterop_InputNote(_l, chan_hnd, note_num, volume);
    if (luainterop_Error() != NULL)
    {
        Error = gcnew String(luainterop_Error());
        return true;
    }
    return false;
}

//--------------------------------------------------------//
bool Interop::Api::InputController(int chan_hnd, int controller, int value)
{
    luainterop_InputController(_l, chan_hnd, controller, value);
    if (luainterop_Error() != NULL)
    {
        Error = gcnew String(luainterop_Error());
        return true;
    }
    return false;
}
#pragma endregion
