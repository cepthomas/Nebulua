#include <windows.h>
#include <wchar.h>
#include <vcclr.h>
#include "lua.hpp"
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


//--------------------------------------------------------//
Interop::Api::Api()
{
    Error = gcnew String("");
    SectionInfo = gcnew Dictionary<int, String^>();
    InitializeCriticalSection(&_critsect);
}

//--------------------------------------------------------//
Interop::NebStatus Interop::Api::Init(List<String^>^ luaPaths)
{
    NebStatus stat = NebStatus::Ok;

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
Interop::NebStatus Interop::Api::OpenScript(String^ fn)
{
    NebStatus nstat = NebStatus::Ok;
    int lstat = LUA_OK;
    int ret = 0;

    EnterCriticalSection(&_critsect);

    if (_l == nullptr)
    {
        Error = gcnew String("You forgot to call Init().");
        nstat = NebStatus::Api;
    }

    ///// Load the script /////
    if (nstat == NebStatus::Ok)
    {
        const char* fnx = ToCString(fn);
        // Pushes the compiled chunk as a_lua function on top of the stack or pushes an error message.
        lstat = luaL_loadfile(_l, fnx);
        nstat = EvalLuaStatus(lstat, "Load script file failed.");
    }

    if (nstat == NebStatus::Ok)
    {
        // Execute the script to initialize it. This catches runtime syntax errors.
        lstat = lua_pcall(_l, 0, LUA_MULTRET, 0);
        nstat = EvalLuaStatus(lstat, "Execute script failed.");
    }

    ///// Run the script /////
    if (nstat == NebStatus::Ok)
    {
        luainterop_Setup(_l);
        if (luainterop_Error() != NULL)
        {
            Error = gcnew String(luainterop_Error());
            nstat = NebStatus::Syntax;
        }
    }

    if (nstat == NebStatus::Ok)
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
Interop::NebStatus Interop::Api::Step(int tick)
{
    NebStatus ret = NebStatus::Ok;
    EnterCriticalSection(&_critsect);

    luainterop_Step(_l, tick);
    if (luainterop_Error() != NULL)
    {
        Error = gcnew String(luainterop_Error());
        ret = NebStatus::Api;
    }

    LeaveCriticalSection(&_critsect);
    return ret;
}

//--------------------------------------------------------//
Interop::NebStatus Interop::Api::InputNote(int chan_hnd, int note_num, double volume)
{
    NebStatus ret = NebStatus::Ok;
    EnterCriticalSection(&_critsect);

    if (luainterop_Error() != NULL)
    {
        Error = gcnew String(luainterop_Error());
        ret = NebStatus::Api;
    }

    luainterop_InputNote(_l, chan_hnd, note_num, volume);

    LeaveCriticalSection(&_critsect);
    return ret;
}

//--------------------------------------------------------//
Interop::NebStatus Interop::Api::InputController(int chan_hnd, int controller, int value)
{
    NebStatus ret = NebStatus::Ok;
    EnterCriticalSection(&_critsect);

    luainterop_InputController(_l, chan_hnd, controller, value);
    if (luainterop_Error() != NULL)
    {
        Error = gcnew String(luainterop_Error());
        ret = NebStatus::Api;
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
        case LUA_OK:        nstat = NebStatus::Ok;          break;
        case LUA_ERRSYNTAX: nstat = NebStatus::Syntax;      break;
        case LUA_ERRFILE:   nstat = NebStatus::File;        break;
        case LUA_ERRRUN:    nstat = NebStatus::Run;         break;
        default:            nstat = NebStatus::Internal;    break;
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
    static char buff[2000]; // TODO2 fixed/max length bad.
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
