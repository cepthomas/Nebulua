#include <stdarg.h>
#include <string.h>
#include "logger.h"
#include "common.h"
#include "diag.h"


//--------------------- Defs -----------------------------//

#define BUFF_LEN 100





// typedef enum
// {
//     ERR_NONE,
//     ERR_USER_SYNTAX,
//     ERR_INTERNAL_LUA,
//     ERR_INTERNAL_C,
//     ERR_OTHER,
// } error_t;


// modified luaL_error(lua_State* l, const char *fmt, ...)

// // Raises an error. The error message format is given by fmt plus any extra arguments, following the same rules of lua_pushfstring.
// // It also adds at the beginning of the message the file name and the line number where the error occurred, if this information is available.
// // This function never returns, but it is an idiom to use it in C functions as return luaL_error(args).
// // int common_DoError(lua_State* l, error_t terr, const char *fmt, ...)
// int common_DoError(lua_State* l, const char *fmt, ...)
// {
//     va_list argp;
//     va_start(argp, fmt);
//     luaL_where(l, 1);
//     lua_pushvfstring(l, fmt, argp);
//     va_end(argp);
//     lua_concat(l, 2);
//     return lua_error(l);
// }








// //--------------------------------------------------------//
// bool common_EvalStatus(lua_State* l, int stat, const char* info)
// {
//     bool has_error = false;
//     if (stat >= LUA_ERRRUN)
//     {
//         has_error = true;
//         const char* sstat = common_StatusToString(stat);

//         if (stat <= LUA_ERRFILE) // internal lua error
//         {
//             // Get error message on stack if provided.
//             if (lua_gettop(l) > 0)
//             {
//                 logger_Log(LVL_ERROR, "Status:%s errmsg:%s  info:%s", sstat, lua_tostring(l, -1), info);
//                 lua_pop(l, 1); // remove it
//             }
//             else
//             {
//                 logger_Log(LVL_ERROR, "Status:%s info:%s", sstat, info);
//             }
//             lua_error(l); // never returns...
//         }
//         else // assume nebulua error
//         {
//             logger_Log(LVL_ERROR, "Status:%s info:%s", sstat, info);
//             lua_error(l); // never returns...
//         }
//     }

//     return has_error;
// }








//--------------------------------------------------------//
const char* common_StatusToString(int stat)
{
    const char* sstat = NULL;
    static char buff[16];

    switch(stat)
    {
        case NEB_OK: sstat = "NEB_OK"; break;
        case NEB_ERR_INTERNAL: sstat = "NEB_ERR_INTERNAL"; break;
        case NEB_ERR_BAD_APP_ARG: sstat = "NEB_ERR_BAD_APP_ARG"; break;
        case NEB_ERR_BAD_LUA_ARG: sstat = "NEB_ERR_BAD_LUA_ARG"; break;
        case NEB_ERR_BAD_MIDI_CFG: sstat = "NEB_ERR_BAD_MIDI_CFG"; break;
        case NEB_ERR_SYNTAX: sstat = "NEB_ERR_SYNTAX"; break;
        default: sstat = diag_LuaStatusToString(stat); break; // lua status?
    }

    if (sstat == NULL)
    {
        snprintf(buff, sizeof(buff) - 1, "STAT%d", stat);
        return buff;
    }
    else
    {
        return sstat;
    }
}


//--------------------------------------------------------//
bool common_StrToDouble(const char* str, double* val) // TODO1 ?? put these somewhere else
{
    bool valid = true;
    char* p;

    errno = 0;
    *val = strtof(str, &p);
    if(errno == ERANGE)
    {
        // Mag is too large.
        valid = false;
    }
    else if(p == str)
    {
        // Bad string.
        valid = false;
    }

    return valid;
}


//--------------------------------------------------------//
bool common_StrToInt(const char* str, int* val)
{
    bool valid = true;
    char* p;

    errno = 0;
    *val = strtol(str, &p, 10);
    if(errno == ERANGE)
    {
        // Mag is too large.
        valid = false;
    }
    else if(p == str)
    {
        // Bad string.
        valid = false;
    }

    return valid;
}
