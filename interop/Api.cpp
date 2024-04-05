#include <windows.h>
#include <wchar.h>
#include <vcclr.h>
#include "lua.hpp"
#include "luainterop.h"
extern "C" {
#include "luautils.h"
}
#include "Api.h"


using namespace System;
using namespace System::Collections::Generic;
using namespace System::Text;


// The main lua thread. This pointless struct decl makes a warning go away per https://github.com/openssl/openssl/issues/6166.
struct lua_State {};

// Protect lua context calls by multiple threads.
static CRITICAL_SECTION _critsect;


//--------------------------------------------------------//
Interop::Api::Api(List<String^>^ lpath)
{
    _lpath = lpath;
    Error = gcnew String("");
    SectionInfo = gcnew Dictionary<int, String^>();
    InitializeCriticalSection(&_critsect);

    NebStatus stat = NebStatus::Ok;

    // Init lua.
    _l = luaL_newstate();

    // Load std libraries.
    luaL_openlibs(_l);

    // Fix lua path.
    if (_lpath->Count > 0)
    {
        // https://stackoverflow.com/a/4156038
        lua_getglobal(_l, "package");
        lua_getfield(_l, -1, "path");
        String^ currentPath = ToCliString(lua_tostring(_l, -1));

        StringBuilder^ sb = gcnew StringBuilder(currentPath);
        sb->Append(";"); // default lua doesn't have this.
        for each (String^ lp in _lpath)
        {
            sb->Append(String::Format("{0}\\?.lua;", lp));
        }
        const char* newPath = ToCString(sb->ToString());

        lua_pop(_l, 1);
        lua_pushstring(_l, newPath);
        lua_setfield(_l, -2, "path");
        lua_pop(_l, 1);
    }

    // Load host funcs into lua space. This table gets pushed on the stack and into globals.
    luainterop_Load(_l);

    // Pop the table off the stack as it interferes with calling the module functions.
    lua_pop(_l, 1);
    //luaL_dostring(_l, "print(package.path");
}

//--------------------------------------------------------//
Interop::Api::~Api()
{
    // Finished. Clean up resources and go home.
    DeleteCriticalSection(&_critsect);
    if (_l != nullptr)
    {
        lua_close(_l);
        _l = nullptr;
    }
}

//--------------------------------------------------------//
Interop::NebStatus Interop::Api::OpenScript(String^ fn)
{
    NebStatus nstat = NebStatus::Ok;
    int lstat = LUA_OK;
    int ret = 0;
    Error = gcnew String("");
    SectionInfo->Clear();

    EnterCriticalSection(&_critsect);

    if (_l == nullptr)
    {
        Error = gcnew String("You forgot to call Init().");
        nstat = NebStatus::ApiError;
    }

    // Load the script.
    if (nstat == NebStatus::Ok)
    {
        const char* fnx = ToCString(fn);
        // Pushes the compiled chunk as a lua function on top of the stack or pushes an error message.
        lstat = luaL_loadfile(_l, fnx);
        nstat = EvalLuaStatus(lstat, "Load script file failed.");
    }

    // Execute the script to initialize it. This catches runtime syntax errors.
    if (nstat == NebStatus::Ok)
    {
        lstat = lua_pcall(_l, 0, LUA_MULTRET, 0);
        nstat = EvalLuaStatus(lstat, "Execute script failed.");
    }

    // Execute setup().
    if (nstat == NebStatus::Ok)
    {
        luainterop_Setup(_l);
        if (luainterop_Error() != NULL)
        {
            Error = gcnew String(luainterop_Error());
            nstat = NebStatus::SyntaxError;
        }
    }

    // Get length and script info.
    if (nstat == NebStatus::Ok)
    {
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
Interop::NebStatus Interop::Api::Step(int tick)
{
    NebStatus ret = NebStatus::Ok;
    Error = gcnew String("");
    EnterCriticalSection(&_critsect);

    luainterop_Step(_l, tick);
    if (luainterop_Error() != NULL)
    {
        Error = gcnew String(luainterop_Error());
        ret = NebStatus::ApiError;
    }

    LeaveCriticalSection(&_critsect);
    return ret;
}

//--------------------------------------------------------//
Interop::NebStatus Interop::Api::RcvNote(int chan_hnd, int note_num, double volume)
{
    NebStatus ret = NebStatus::Ok;
    Error = gcnew String("");
    EnterCriticalSection(&_critsect);

    if (luainterop_Error() != NULL)
    {
        Error = gcnew String(luainterop_Error());
        ret = NebStatus::ApiError;
    }

    luainterop_RcvNote(_l, chan_hnd, note_num, volume);

    LeaveCriticalSection(&_critsect);
    return ret;
}

//--------------------------------------------------------//
Interop::NebStatus Interop::Api::RcvController(int chan_hnd, int controller, int value)
{
    NebStatus ret = NebStatus::Ok;
    Error = gcnew String("");
    EnterCriticalSection(&_critsect);

    luainterop_RcvController(_l, chan_hnd, controller, value);
    if (luainterop_Error() != NULL)
    {
        Error = gcnew String(luainterop_Error());
        ret = NebStatus::ApiError;
    }

    LeaveCriticalSection(&_critsect);
    return ret;
}


//------------------- Privates ---------------------------//

//--------------------------------------------------------//
Interop::NebStatus Interop::Api::EvalLuaStatus(int lstat, String^ info)
{
    NebStatus nstat;

    // Translate between internal LUA_XXX status and client facing NEB_XXX status.
    switch (lstat)
    {
        case LUA_OK:        nstat = NebStatus::Ok;              break;
        case LUA_ERRSYNTAX: nstat = NebStatus::SyntaxError;     break;
        case LUA_ERRFILE:   nstat = NebStatus::FileError;       break;
        case LUA_ERRRUN:    nstat = NebStatus::RunError;        break;
        default:            nstat = NebStatus::InternalError;   break;
    }

    if (nstat != NebStatus::Ok)
    {
        // Maybe lua error message.
        const char* smsg = "";
        if (lstat <= LUA_ERRFILE && _l != NULL && lua_gettop(_l) > 0)
        {
            smsg = lua_tostring(_l, -1);
            lua_pop(_l, 1);
            Error = String::Format(gcnew String("{0}: {1}\n{2}:{3}"), nstat.ToString(), info, lstat, gcnew String(smsg));
        }
        else
        {
            Error = String::Format(gcnew String("{0}: {1}"), nstat.ToString(), info);
        }
    }
    else
    {
        Error = "";
    }

    return nstat;
}

//--------------------------------------------------------//
const char* Interop::Api::ToCString(String^ input)
{
    static char buff[2000]; // TODO1 fixed/max length bad.
    bool ok = true;
    int len = input->Length > 1999 ? 1999 : input->Length;

    // https://learn.microsoft.com/en-us/cpp/dotnet/how-to-access-characters-in-a-system-string?view=msvc-170
    // not! const char* str4 = context->marshal_as<const char*>(input);
    interior_ptr<const wchar_t> ppchar = PtrToStringChars(input);
    int i = 0;
    for (; *ppchar != L'\0' && i < len && ok; ++ppchar, i++)
    {
        int c = wctob(*ppchar);
        if (c != -1)
        {
            buff[i] = c;
        }
        else
        {
            ok = false;
            buff[i] = '?';
        }
    }
    buff[i] = 0; // terminate

    return buff;
}

//--------------------------------------------------------//
String^ Interop::Api::ToCliString(const char* input)
{
    return gcnew String(input);
}
