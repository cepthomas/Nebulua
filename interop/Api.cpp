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

 //Translate between internal LUA_XXX status and client facing NEB_XXX status.
//static int MapStatus(int lua_status);



//--------------------------------------------------------//
Interop::Api::Api()
{
    Error = gcnew String("");
    SectionInfo = gcnew Dictionary<int, String^>();
    InitializeCriticalSection(&_critsect);
}

//--------------------------------------------------------//
int Interop::Api::Init(List<String^>^ lpath)
{
    int stat = NEB_OK;

    // Init lua.
    _l = luaL_newstate();

    // Load std libraries.
    luaL_openlibs(_l);

    if (lpath->Count > 0)
    {
       // String^ paths = gcnew String("");
        StringBuilder^ paths = gcnew StringBuilder();

        array<int>^ arry = { 1,2,3,4,5 };

        for each(int x in arry)
        {
//>>>>>>>>>>>>>>>>>>>>>>>>>xxx
            Console::WriteLine(x);

        }   //     lpath->ForEach(p )

     //   paths.


        // Specific path. TODO1 Should look like ;;path1\?.lua;path2\?.lua;


        // public void SetLuaPath(List<string> paths)
        // {
        //     List<string> parts = new() { "?", "?.lua" };
        //     paths.ForEach(p => parts.Add(Path.Join(p, "?.lua").Replace('\\', '/')));
        //     string s = string.Join(';', parts);
        //     s = $"package.path = \"{s}\"";
        //     DoString(s);
        // }


// C:\Dev\repos\Lua\Nebulua\bin\Debug\net8.0-windows7.0\lua\?.lua;
// C:\Dev\repos\Lua\Nebulua\bin\Debug\net8.0-windows7.0\lua\?\init.lua;
// C:\Dev\repos\Lua\Nebulua\bin\Debug\net8.0-windows7.0\?.lua;
// C:\Dev\repos\Lua\Nebulua\bin\Debug\net8.0-windows7.0\?\init.lua;
// C:\Dev\repos\Lua\Nebulua\bin\Debug\net8.0-windows7.0\..\share\lua\5.4\?.lua;
// C:\Dev\repos\Lua\Nebulua\bin\Debug\net8.0-windows7.0\..\share\lua\5.4\?\init.lua;
// .\?.lua;
// .\?\init.lua;
// C:\Dev\repos\Lua\Nebulua\lua_code\?.lua;
// C:\Dev\repos\Lua\Nebulua\test\?.lua;
// C:\Dev\repos\Lua\LuaBagOfTricks\?.lua;


        // You can set the_lUA_PATH and_lUA_CPATH within c++ very easily by executing a couple lual_dostring functions.
        // luaL_dostring(L, "package.path = package.path .. ';?.lua'");
        // luaL_dostring(L, "package.cpath = package.cpath .. ';?.dll'");

    }

    //_load host funcs into lua space. This table gets pushed on the stack and into globals.
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

//--------------------------------------------------------//
int Interop::Api::OpenScript(String^ fn)
{
    int nstat = NEB_OK;
    int lstat = LUA_OK;
    int ret = 0;

    EnterCriticalSection(&_critsect);

    nstat = DoCheck();

    char fnx[MAX_PATH];
    if (nstat == NEB_OK)
    {
        if (!ToCString(fn, fnx, MAX_PATH))
        {
            Error = gcnew String("Bad script file name.");
            nstat = NEB_ERR_API;
        }
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
        SectionInfo->Add(length, ToCliString("LENGTH"));
    }
    
    LeaveCriticalSection(&_critsect);
    return nstat;
}

//--------------------------------------------------------//
int Interop::Api::Step(int tick)
{
    int ret = DoCheck();

    EnterCriticalSection(&_critsect);

    if (ret == NEB_OK)
    {
        luainterop_Step(_l, tick);
        if (luainterop_Error() != NULL)
        {
            Error = gcnew String(luainterop_Error());
            ret = NEB_OK;
        }
    }

    LeaveCriticalSection(&_critsect);
    return ret;
}

//--------------------------------------------------------//
int Interop::Api::InputNote(int chan_hnd, int note_num, double volume)
{
    int ret = DoCheck();

    EnterCriticalSection(&_critsect);

    if (ret == NEB_OK)
    {
        if (luainterop_Error() != NULL)
        {
            Error = gcnew String(luainterop_Error());
            ret = NEB_OK;
        }
    }
    luainterop_InputNote(_l, chan_hnd, note_num, volume);

    LeaveCriticalSection(&_critsect);
    return ret;
}

//--------------------------------------------------------//
int Interop::Api::InputController(int chan_hnd, int controller, int value)
{
    int ret = DoCheck();

    EnterCriticalSection(&_critsect);

    if (ret == NEB_OK)
    {
        luainterop_InputController(_l, chan_hnd, controller, value);
        if (luainterop_Error() != NULL)
        {
            Error = gcnew String(luainterop_Error());
            ret = NEB_OK;
        }
    }

    LeaveCriticalSection(&_critsect);
    return ret;
}

//--------------------------------------------------------//
int Interop::Api::MapStatus(int lua_status)
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

//--------------------------------------------------------//
int Interop::Api::DoCheck()
{
    if (_l == nullptr)
    {
        Error = gcnew String("You forgot to call Init().");
        return NEB_ERR_API;
    }
    else
    {
        return NEB_OK;
    }
}
