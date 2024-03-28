
#include <windows.h>

#include "lua.hpp"
#include "nebcommon.h"
#include "luainterop.h"
#include "Api.h"

using namespace System;
using namespace System::Collections::Generic;


// The main_lua thread. This pointless struct decl makes a warning go away per https://github.com/openssl/openssl/issues/6166.
struct lua_State {};
static lua_State* _l;

// Protect lua context calls by multiple threads.
static CRITICAL_SECTION _critsect;

// Client gets NEB_XXX statuses.
static int MapStatus(int lua_status);


#pragma region Lifecycle

//--------------------------------------------------------//
int Interop::Api::Init(List<String^>^ lpath)
{
    int stat = NEB_OK;

    //_load std libraries.
    luaL_openlibs(_l);

    if (lpath->Count > 0)
    {
        // Specific path.




    }



    // You can set the_lUA_PATH and_lUA_CPATH within c++ very easily by executing a couple lual_dostring functions.
// luaL_dostring(L, "package.path = package.path .. ';?.lua'");
// luaL_dostring(L, "package.cpath = package.cpath .. ';?.dll'");



    //_load host funcs into lua space. This table gets pushed on the stack and into globals.
    luainterop_Load(_l);

    // Pop the table off the stack as it interferes with calling the module functions.
    lua_pop(_l, 1);

    return stat;
}

//--------------------------------------------------------//
Interop::Api::Api()
{
    // Init internal lib.
    _l = luaL_newstate();
    Error = gcnew String("");
    SectionInfo = gcnew Dictionary<int, String^>();
    InitializeCriticalSection(&_critsect);
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
    int nstat = NEB_OK;
    int lstat = LUA_OK;
    int ret = 0;

    EnterCriticalSection(&_critsect);

    char fnx[MAX_PATH];
    if (!ToCString(fn, fnx, MAX_PATH))
    {
        Error = gcnew String("Bad script file name.");
        nstat = NEB_ERR_API;
    }

    /////_load the script /////
    if (nstat == NEB_OK)
    {
        //_load/compile the script file. Pushes the compiled chunk as a_lua function on top of the stack or pushes an error message.
        lstat = luaL_loadfile(_l, fnx);
        if (lstat != LUA_OK)
        {
            Error = gcnew String(EvalStatus(_l, lstat, "Load script file failed."));
            nstat = MapStatus(lstat);
        }
    }

    if (nstat == NEB_OK)
    {
        // Execute the script to initialize it. This catches runtime syntax errors.
        lstat = lua_pcall(_l, 0, LUA_MULTRET, 0);
        if (lstat != LUA_OK)
        {
            Error = gcnew String(EvalStatus(_l, lstat, "Execute script failed."));
            nstat = MapStatus(lstat);
        }
    }

    ///// Run the script /////

    if (nstat == NEB_OK)
    {
        luainterop_Setup(_l);
        if (luainterop_Error() != NULL)
        {
            Error = gcnew String(luainterop_Error());
            nstat = NEB_ERR_SYNTAX;
        }
    }

    if (nstat == NEB_OK)
    {
        // Get script info.

        // Get length.
        int ltype = lua_getglobal(_l, "_length");
        int length = (int)lua_tointeger(_l, -1);
        lua_pop(_l, 1); // Clean up stack.

        // Get section info.
        ltype = lua_getglobal(_l, "_section_names");
        lua_pushnil(_l);
        while (lua_next(_l, -2) != 0)// && lstat ==_lUA_OK)
        {
            SectionInfo->Add((int)lua_tointeger(_l, -1), ToCliString(lua_tostring(_l, -2)));
            lua_pop(_l, 1);
        }
        lua_pop(_l, 1); // Clean up stack.

        // Tack on the overal length.
        SectionInfo->Add(length, ToCliString("LENGTH=========="));
    }
    
    LeaveCriticalSection(&_critsect);
    return nstat;
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
    case LUA_OK:        xstat = NEB_OK;             break;
    case LUA_ERRSYNTAX: xstat = NEB_ERR_SYNTAX;     break;
    case LUA_ERRFILE:   xstat = NEB_ERR_FILE;       break;
    case LUA_ERRRUN:    xstat = NEB_ERR_RUN;        break;
    default:            xstat = NEB_ERR_INTERNAL;   break;
    }

    return xstat;
}
#pragma endregion
