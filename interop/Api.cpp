
#include <windows.h>

#include "lua.hpp"
#include "nebcommon.h"
#include "luainterop.h"
#include "Api.h"

using namespace System;
using namespace System::Collections::Generic;


// The main Lua thread. This pointless struct decl makes a warning go away per https://github.com/openssl/openssl/issues/6166.
struct lua_State {};
static lua_State* _l;

// Protect lua context calls by multiple threads.
static CRITICAL_SECTION _critsect;

// Client gets NEB_XXX statuses.
static int MapStatus(int lua_status);


#pragma region Lifecycle
//--------------------------------------------------------//
int Interop::Api::Init()
{
    int stat = NEB_OK;

    InitializeCriticalSection(&_critsect);

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
    // Finished. Clean up resources and go home.
    DeleteCriticalSection(&_critsect);
    lua_close(_l);
}
#pragma endregion


#pragma region Host calls lua script
//--------------------------------------------------------//
int Interop::Api::OpenScript(String^ fn)
{
    int stat = NEB_OK;
    int ret = 0;

    EnterCriticalSection(&_critsect);

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
            stat = MapStatus(stat);
        }
    }

    if (stat == NEB_OK)
    {
        // Execute the script to initialize it. This catches runtime syntax errors.
        stat = lua_pcall(_l, 0, LUA_MULTRET, 0);
        if (stat != NEB_OK)
        {
            Error = gcnew String(nebcommon_EvalStatus(_l, stat, "Execute script failed."));
            stat = MapStatus(stat);
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
        ltype = lua_getglobal(_l, "_section_names");
        lua_pushnil(_l);
        while (lua_next(_l, -2) != 0 && stat == LUA_OK)
        {
            SectionInfo->Add((int)lua_tointeger(_l, -1), ToCliString(lua_tostring(_l, -2)));
            lua_pop(_l, 1);
        }
        lua_pop(_l, 1); // Clean up stack.

        // Tack on the overal length.
        SectionInfo->Add(length, ToCliString("LENGTH=========="));
    }
    
    stat = MapStatus(stat);

    LeaveCriticalSection(&_critsect);
    return stat;
}

//--------------------------------------------------------//
int Interop::Api::Step(int tick)
{
    int ret = NEB_ERR_API;
    EnterCriticalSection(&_critsect);

    luainterop_Step(_l, tick);
    if (luainterop_Error() != NULL)
    {
        Error = gcnew String(luainterop_Error());
        ret = NEB_OK;
    }

    LeaveCriticalSection(&_critsect);
    return ret;
}

//--------------------------------------------------------//
int Interop::Api::InputNote(int chan_hnd, int note_num, double volume)
{
    int ret = NEB_ERR_API;
    EnterCriticalSection(&_critsect);

    luainterop_InputNote(_l, chan_hnd, note_num, volume);
    if (luainterop_Error() != NULL)
    {
        Error = gcnew String(luainterop_Error());
        ret = NEB_OK;
    }

    LeaveCriticalSection(&_critsect);
    return ret;
}

//--------------------------------------------------------//
int Interop::Api::InputController(int chan_hnd, int controller, int value)
{
    int ret = NEB_ERR_API;
    EnterCriticalSection(&_critsect);

    luainterop_InputController(_l, chan_hnd, controller, value);
    if (luainterop_Error() != NULL)
    {
        Error = gcnew String(luainterop_Error());
        ret = NEB_OK;
    }

    LeaveCriticalSection(&_critsect);
    return ret;
}
#pragma endregion

#pragma region Private functions
//--------------------------------------------------------//

int MapStatus(int lua_status)
{
    int xstat;

    switch (lua_status)
    {
    case LUA_OK:
        xstat = NEB_OK;
        break;

    case LUA_ERRSYNTAX:
        xstat = NEB_ERR_SYNTAX;
        break;

    case LUA_ERRRUN:
        xstat = NEB_ERR_RUN;
        break;

    default:
        xstat = NEB_ERR_INTERNAL;
        break;
    }

    return xstat;
}
#pragma endregion
