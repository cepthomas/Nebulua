#include <windows.h>
#include <wchar.h>
#include <vcclr.h>
#include "lua.hpp"
#include "luainterop.h"
extern "C" {
#include "luautils.h"
}
#include "AppInterop.h"


using namespace System;
using namespace System::Collections::Generic;
using namespace System::Text;
using namespace System::Diagnostics;
using namespace System::IO;
using namespace Nebulua::Interop;


#define MAX_PATH  260 // from win32 def

// This struct decl makes a warning go away per https://github.com/openssl/openssl/issues/6166.
struct lua_State {};

// Protect lua context calls by multiple threads.
static CRITICAL_SECTION _critsect;

// Convert managed string to unmanaged. Caller must free() returned buffer.
static char* _ToCString(String^ input);

// Convert unmanaged string to managed.
static String^ _ToManagedString(const char* input);


//--------------------------------------------------------//
AppInterop::AppInterop(List<String^>^ lpath)
{
    InitializeCriticalSection(&_critsect);

    // Init vars.
    _luaPath = lpath;
    Error = "";
    NebStatus nstat = NebStatus::Ok;
    int lstat = LUA_OK;

    // Get the directory name where the application lives.
    DirectoryInfo^ dinfo = gcnew DirectoryInfo(__FILE__);
    while (dinfo->Name != "Nebulua")
    {
        dinfo = dinfo->Parent;
    }
    _rootdir = dinfo->FullName;

    // Init lua.
    _l = luaL_newstate();

    _LogDebug("construct");

    // Load std libraries.
    luaL_openlibs(_l);

    // Fix lua path.
    if (_luaPath->Count > 0)
    {
        // https://stackoverflow.com/a/4156038
        lua_getglobal(_l, "package");
        lua_getfield(_l, -1, "path");
        String^ currentPath = _ToManagedString(lua_tostring(_l, -1));

        StringBuilder^ sb = gcnew StringBuilder(currentPath);
        sb->Append(";"); // default lua path doesn't have this.
        for each (String^ lp in _luaPath) // add app specific.
        {
            sb->Append(String::Format("{0}\\?.lua;", lp));
        }
        String^ newPath = sb->ToString();

        char* spath = _ToCString(newPath);
        lua_pop(_l, 1);
        lua_pushstring(_l, spath);
        lua_setfield(_l, -2, "path");
        lua_pop(_l, 1);
        free(spath);
    }

    // Load C host funcs into lua space. This table gets pushed on the stack and into globals.
    luainterop_Load(_l);

    // Pop the table off the stack as it interferes with calling the module functions.
    lua_pop(_l, 1);
}

//--------------------------------------------------------//
AppInterop::~AppInterop()
{
    _LogDebug("destruct");

    // Finished. Clean up resources and go home.
    DeleteCriticalSection(&_critsect);

    if (_l != nullptr)
    {
        lua_close(_l);
        _l = nullptr;
    }
}

//--------------------------------------------------------//
NebStatus AppInterop::OpenScript(String^ fn)
{
    NebStatus nstat = NebStatus::Ok;
    int lstat = LUA_OK;
    int ret = 0;
    Error = "";

    EnterCriticalSection(&_critsect);

    if (_l == nullptr)
    {
        Error = gcnew String("You forgot to call Init().");
        nstat = NebStatus::AppInteropError;
    }

    // Load the script into memory.
    if (nstat == NebStatus::Ok)
    {
        char* fnx = _ToCString(fn);
        // Pushes the compiled chunk as a lua function on top of the stack or pushes an error message.
        lstat = luaL_loadfile(_l, fnx);
        free(fnx);
        nstat = _EvalLuaStatus(lstat, "Load script file failed.");
    }

    // Execute the script to initialize it. This reports runtime syntax errors.
    if (nstat == NebStatus::Ok)
    {
        // Do the protected call. Use extended version which adds a stacktrace.
        lstat = luaex_docall(_l, 0, 0);
        nstat = _EvalLuaStatus(lstat, "Execute script failed.");
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
    
    LeaveCriticalSection(&_critsect);

    return nstat;
}

//--------------------------------------------------------//
NebStatus AppInterop::Step(int tick)
{
    NebStatus ret = NebStatus::Ok;
    Error = "";

    EnterCriticalSection(&_critsect);

    luainterop_Step(_l, tick);

    if (luainterop_Error() != NULL)
    {
        Error = gcnew String(luainterop_Error());
        ret = NebStatus::AppInteropError;
    }

    LeaveCriticalSection(&_critsect);

    return ret;
}

//--------------------------------------------------------//
NebStatus AppInterop::RcvNote(int chan_hnd, int note_num, double volume)
{
    NebStatus ret = NebStatus::Ok;
    Error = "";

    EnterCriticalSection(&_critsect);

    luainterop_RcvNote(_l, chan_hnd, note_num, volume);

    if (luainterop_Error() != NULL)
    {
        Error = gcnew String(luainterop_Error());
        ret = NebStatus::AppInteropError;
    }

    LeaveCriticalSection(&_critsect);

    return ret;
}

//--------------------------------------------------------//
NebStatus AppInterop::RcvController(int chan_hnd, int controller, int value)
{
    NebStatus ret = NebStatus::Ok;
    Error = "";

    EnterCriticalSection(&_critsect);

    luainterop_RcvController(_l, chan_hnd, controller, value);

    if (luainterop_Error() != NULL)
    {
        Error = gcnew String(luainterop_Error());
        ret = NebStatus::AppInteropError;
    }

    LeaveCriticalSection(&_critsect);

    return ret;
}

//--------------------------------------------------------//
String^ AppInterop::NebCommand(String^ cmd, String^ arg)
{
    Error = "";

    char* scmd = _ToCString(cmd);
    char* sarg = _ToCString(arg);
    const char* ret = luainterop_NebCommand(_l, scmd, sarg);
    free(scmd);
    free(sarg);

    if (luainterop_Error() != NULL)
    {
        Error = gcnew String(luainterop_Error());
    }

    return _ToManagedString(ret);
}

//------------------- Privates ---------------------------//

//--------------------------------------------------------//
NebStatus AppInterop::_EvalLuaStatus(int lstat, String^ info)
{
    NebStatus nstat;

    // Translate between internal LUA_XXX status and client facing NEB_XXX status.
    switch (lstat)
    {
        case LUA_OK:        nstat = NebStatus::Ok;              break;
        case LUA_ERRSYNTAX: nstat = NebStatus::SyntaxError;     break;
        case LUA_ERRFILE:   nstat = NebStatus::FileError;       break;
        case LUA_ERRRUN:    nstat = NebStatus::RunError;        break;
        default:            nstat = NebStatus::AppInteropError;        break;
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
void AppInterop::_LogDebug(String^ msg)
{
    LogArgs^ args = gcnew LogArgs();
    args->Sender = Id; // MAKE_ID(this);
    args->LogLevel = 1; // debug
    args->Message = gcnew String("APPINTEROP ")  + msg;
    NotifyLog(args);
}


//--------------------------------------------------------//
char* _ToCString(String^ input)
{
    int inlen = input->Length;
    char* buff = (char*)calloc(static_cast<size_t>(inlen) + 1, sizeof(char));

    if (buff) // shut up compiler
    {
        // https://learn.microsoft.com/en-us/cpp/dotnet/how-to-access-characters-in-a-system-string?view=msvc-170
        // not! const char* str4 = context->marshal_as<const char*>(input);
        interior_ptr<const wchar_t> ppchar = PtrToStringChars(input);
        for (int i = 0; *ppchar != L'\0' && i < inlen; ++ppchar, i++)
        {
            int c = wctob(*ppchar);
            buff[i] = c != -1 ? c : '?';
        }
    }

    return buff;
}

//--------------------------------------------------------//
String^ _ToManagedString(const char* input)
{
    return gcnew String(input);
}
