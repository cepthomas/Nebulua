
#include <stdlib.h>
#include <stdio.h>
#include <stdarg.h>
#include <string.h>
#include <errno.h>

#include "luautils.h"

#define BUFF_LEN 300



//--------------------------------------------------------//
int luautils_DumpStack(lua_State* l, FILE* fout, const char* info)
{
    static char buff[BUFF_LEN];

    fprintf(fout, "Dump stack:%s (L:%p)\n", info, l);

    for(int i = lua_gettop(l); i >= 1; i--)
    {
        int t = lua_type(l, i);

        switch(t)
        {
            case LUA_TSTRING:
                snprintf(buff, BUFF_LEN-1, "index:%d string:%s ", i, lua_tostring(l, i));
                break;
            case LUA_TBOOLEAN:
                snprintf(buff, BUFF_LEN-1, "index:%d bool:%s ", i, lua_toboolean(l, i) ? "true" : "false");
                break;
            case LUA_TNUMBER:
                snprintf(buff, BUFF_LEN-1, "index:%d number:%g ", i, lua_tonumber(l, i));
                break;
            case LUA_TNIL:
                snprintf(buff, BUFF_LEN-1, "index:%d nil", i);
                break;
            case LUA_TNONE:
                snprintf(buff, BUFF_LEN-1, "index:%d none", i);
                break;
            case LUA_TFUNCTION:
            case LUA_TTABLE:
            case LUA_TTHREAD:
            case LUA_TUSERDATA:
            case LUA_TLIGHTUSERDATA:
                snprintf(buff, BUFF_LEN-1, "index:%d %s:%p ", i, lua_typename(l, t), lua_topointer(l, i));
                break;
            default:
                snprintf(buff, BUFF_LEN-1, "index:%d type:%d", i, t);
                break;
        }
    
        fprintf(fout, "   %s\n", buff);
    }

    return 0;
}

//--------------------------------------------------------//
const char* luautils_LuaStatusToString(int stat)
{
    const char* sstat = "UNKNOWN";
    switch(stat)
    {
        case LUA_OK: sstat = "LUA_OK"; break;
        case LUA_YIELD: sstat = "LUA_YIELD"; break;
        case LUA_ERRRUN: sstat = "LUA_ERRRUN"; break;
        case LUA_ERRSYNTAX: sstat = "LUA_ERRSYNTAX"; break; // syntax error during pre-compilation
        case LUA_ERRMEM: sstat = "LUA_ERRMEM"; break; // memory allocation error
        case LUA_ERRERR: sstat = "LUA_ERRERR"; break; // error while running the error handler function
        case LUA_ERRFILE: sstat = "LUA_ERRFILE"; break; // couldn't open the given file
        default: break; // nothing else for now.
    }
    return sstat;
}

//--------------------------------------------------------//
int luautils_DumpTable(lua_State* l, FILE* fout, const char* name) // TODOF make recursive like lua dump_table()?
{
    fprintf(fout, "%s\n", name);

    // Put a nil key on stack.
    lua_pushnil(l);

    // key(-1) is replaced by the next key(-1) in table(-2).
    while (lua_next(l, -2) != 0)
    {
        // Get key(-2) name.
        const char* name = lua_tostring(l, -2);

        // Get type of value(-1).
        const char* type = luaL_typename(l, -1);

        // Get value(-1).
        const char* sval = luaL_tolstring(l, -1, NULL);
        fprintf(fout, "   %s:%s(%s)\n", name, sval, type);
        // Remove the sval from the stack.
        lua_pop(l, 1);

        // Remove value(-1), now key on top at(-1).
        lua_pop(l, 1);
    }
    
    return 0;
}

//--------------------------------------------------------//
int luautils_DumpGlobals(lua_State* l, FILE* fout)
{
    // Get global table.
    lua_pushglobaltable(l);

    luautils_DumpTable(l, fout, "GLOBALS");

    // Remove global table(-1).
    lua_pop(l,1);

    return 0;
}

 //--------------------------------------------------------//
 void luautils_EvalStack(lua_State* l, FILE* fout, int expected)
 {
     int num = lua_gettop(l);
     if (num != expected)
     {
         fprintf(fout, "Expected %d stack but is %d\n", expected, num);
     }
 }

//--------------------------------------------------------//
bool luautils_ParseDouble(const char* str, double* val, double min, double max)
{
    bool valid = true;
    char* p;

    errno = 0;
    *val = strtof(str, &p);
    if (errno == ERANGE)
    {
        // Mag is too large.
        valid = false;
    }
    else if (p == str)
    {
        // Bad string.
        valid = false;
    }
    else if (*val < min || *val > max)
    {
        // Out of range.
        valid = false;
    }

    return valid;
}

//--------------------------------------------------------//
bool luautils_ParseInt(const char* str, int* val, int min, int max)
{
    bool valid = true;
    char* p;

    errno = 0;
    *val = strtol(str, &p, 10);
    if (errno == ERANGE)
    {
        // Mag is too large.
        valid = false;
    }
    else if (p == str)
    {
        // Bad string.
        valid = false;
    }
    else if (*val < min || *val > max)
    {
        // Out of range.
        valid = false;
    }

    return valid;
}
