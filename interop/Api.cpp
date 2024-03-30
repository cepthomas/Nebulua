#include <windows.h>
#include "lua.hpp"
#include "nebcommon.h"
#include "luainterop.h"
#include "Api.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Text;


// The main_lua thread. This pointless struct decl makes a warning go away per https://github.com/openssl/openssl/issues/6166.
struct lua_State {};
static lua_State* _l = nullptr;

// Protect lua context calls by multiple threads.
static CRITICAL_SECTION _critsect;


// Translate between internal LUA_XXX status and client facing NEB_XXX status.
static int MapStatus(int lua_status);


//--------------------------------------------------------//
Interop::Api::Api()
{
    Error = gcnew String("");
    SectionInfo = gcnew Dictionary<int, String^>();
    InitializeCriticalSection(&_critsect);
}

//--------------------------------------------------------//
int Interop::Api::Init(List<String^>^ luaPaths)
{
    int stat = NEB_OK;

    // Init lua.
    _l = luaL_newstate();

    // Load std libraries.
    luaL_openlibs(_l);

    // Fix lua path.
    if (luaPaths->Count > 0)
    {

        //int setLuaPath(lua_State * L, const char* path)  TODO1
        //{
        //    lua_getglobal(L, "package");
        //    lua_getfield(L, -1, "path"); // get field "path" from table at top of stack (-1)
        //    std::string cur_path = lua_tostring(L, -1); // grab path string from top of stack
        //    cur_path.append(";"); // do your path magic here
        //    cur_path.append(path);
        //    lua_pop(L, 1); // get rid of the string on the stack we just pushed on line 5
        //    lua_pushstring(L, cur_path.c_str()); // push the new one
        //    lua_setfield(L, -2, "path"); // set the field "path" in table at -2 with value at top of stack
        //    lua_pop(L, 1); // get rid of package table from top of stack
        //    return 0; // all done!
        //}



        StringBuilder^ sb = gcnew StringBuilder();
        sb->Append("package.path = package.path .. ");
        for each (String ^ lp in luaPaths)
        {
            sb->Append(String::Format("{0}\\?.lua;", lp));
        }
        const char* fnx = ToCString(sb->ToString());
        luaL_dostring(_l, fnx);
    }

    //_load host funcs into lua space. This table gets pushed on the stack and into globals.
    luainterop_Load(_l);

    // Pop the table off the stack as it interferes with calling the module functions.
    lua_pop(_l, 1);
    //luaL_dostring(_l, "print(package.path");

    return stat;
}

//--------------------------------------------------------//
Interop::Api::~Api()
{
    // Finished. Clean up resources and go home.
    DeleteCriticalSection(&_critsect);
    lua_close(_l);
}

//--------------------------------------------------------//
int Interop::Api::OpenScript(String^ fn)
{
    int nstat = NEB_OK;
    int lstat = LUA_OK;
    int ret = 0;

    EnterCriticalSection(&_critsect);

    if (_l == nullptr)
    {
        Error = gcnew String("You forgot to call Init().");
        nstat = NEB_ERR_API;
    }

    ///// Load the script /////
    if (nstat == NEB_OK)
    {
        const char* fnx = ToCString(fn);
        // Pushes the compiled chunk as a_lua function on top of the stack or pushes an error message.
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
        SectionInfo->Add(length, ToCliString("LENGTH"));
    }
    
    LeaveCriticalSection(&_critsect);
    return nstat;
}

//--------------------------------------------------------//
int Interop::Api::Step(int tick)
{
    int ret = NEB_OK;
    EnterCriticalSection(&_critsect);

    luainterop_Step(_l, tick);
    if (luainterop_Error() != NULL)
    {
        Error = gcnew String(luainterop_Error());
        ret = NEB_ERR_API;
    }

    LeaveCriticalSection(&_critsect);
    return ret;
}

//--------------------------------------------------------//
int Interop::Api::InputNote(int chan_hnd, int note_num, double volume)
{
    int ret = NEB_OK;
    EnterCriticalSection(&_critsect);

    if (luainterop_Error() != NULL)
    {
        Error = gcnew String(luainterop_Error());
        ret = NEB_ERR_API;
    }

    luainterop_InputNote(_l, chan_hnd, note_num, volume);

    LeaveCriticalSection(&_critsect);
    return ret;
}

//--------------------------------------------------------//
int Interop::Api::InputController(int chan_hnd, int controller, int value)
{
    int ret = NEB_OK;
    EnterCriticalSection(&_critsect);

    luainterop_InputController(_l, chan_hnd, controller, value);
    if (luainterop_Error() != NULL)
    {
        Error = gcnew String(luainterop_Error());
        ret = NEB_ERR_API;
    }

    LeaveCriticalSection(&_critsect);
    return ret;
}

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
