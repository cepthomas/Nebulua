
#include <stdarg.h>
#include <string.h>

#include "logger.h"
#include "common.h"


#define BUFF_LEN 100


//--------------------------------------------------------//
int common_DumpStack(lua_State* L, const char* fn, int line, const char* info)
{
    static char buff[BUFF_LEN];

    logger_Log(LVL_DEBUG, fn, line, "Dump stack:%s (L:%p)", info, L);

    for(int i = lua_gettop(L); i >= 1; i--)
    {
        int t = lua_type(L, i);

        switch(t)
        {
            case LUA_TSTRING:
                snprintf(buff, BUFF_LEN-1, "index:%d string:%s ", i, lua_tostring(L, i));
                break;
            case LUA_TBOOLEAN:
                snprintf(buff, BUFF_LEN-1, "index:%d bool:%s ", i, lua_toboolean(L, i) ? "true" : "false");
                break;
            case LUA_TNUMBER:
                snprintf(buff, BUFF_LEN-1, "index:%d number:%g ", i, lua_tonumber(L, i));
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
                snprintf(buff, BUFF_LEN-1, "index:%d %s:%p ", i, lua_typename(L, t), lua_topointer(L, i));
                break;
            default:
                snprintf(buff, BUFF_LEN-1, "index:%d type:%d", i, t);
                break;
        }
    
        logger_Log(LVL_DEBUG, fn, line, "   %s", buff);
    }

    return 0;
}

//--------------------------------------------------------//
void common_LuaError(lua_State* L, const char* fn, int line, int err, const char* format, ...)
{
    static char buff[BUFF_LEN];

    va_list args;
    va_start(args, format);
    logger_Log(LVL_DEBUG, fn, line, format, args);
    va_end(args);

    switch(err)
    {
        case LUA_ERRRUN:
            snprintf(buff, BUFF_LEN-1, "LUA_ERRRUN");
            break;
        case LUA_ERRSYNTAX:
            snprintf(buff, BUFF_LEN-1, "LUA_ERRSYNTAX: syntax error during pre-compilation");
            break;
        case LUA_ERRMEM:
            snprintf(buff, BUFF_LEN-1, "LUA_ERRMEM: memory allocation error");
            break;
        // case LUA_ERRGCMM:
        //     snprintf(buff, BUFF_LEN-1, "LUA_ERRGCMM: GC error");
        //     break;
        case LUA_ERRERR:
            snprintf(buff, BUFF_LEN-1, "LUA_ERRERR: error while running the error handler function");
            break;
        case LUA_ERRFILE:
            snprintf(buff, BUFF_LEN-1, "LUA_ERRFILE: couldn't open the given file");
            break;
        default:
            snprintf(buff, BUFF_LEN-1, "Unknown error %i (caller:%d)", err, line);
            break;
    }
    logger_Log(LVL_DEBUG, fn, line, "   %s", buff);

    // Dump trace.
    luaL_traceback(L, L, NULL, 1);
    snprintf(buff, BUFF_LEN-1, "%s | %s | %s", lua_tostring(L, -1), lua_tostring(L, -2), lua_tostring(L, -3));
    logger_Log(LVL_DEBUG, fn, line, "   %s", buff);

    lua_error(L); // never returns
}

//--------------------------------------------------------//
int common_DumpTable(lua_State* L, const char* name)
{
    logger_Log(LVL_DEBUG, name, -1, name);

    // Put a nil key on stack.
    lua_pushnil(L);

    // key(-1) is replaced by the next key(-1) in table(-2).
    while (lua_next(L, -2) != 0)
    {
        // Get key(-2) name.
        const char* name = lua_tostring(L, -2);

        // Get type of value(-1).
        const char* type = luaL_typename(L, -1);

        logger_Log(LVL_DEBUG, name, -1, "   %s=%s", name, type);

        // Remove value(-1), now key on top at(-1).
        lua_pop(L, 1);
    }
    
    return 0;
}
