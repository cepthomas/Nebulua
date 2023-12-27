#include <stdarg.h>
#include <string.h>
#include "logger.h"
#include "common.h"
#include "diag.h"


//--------------------- Defs -----------------------------//

#define BUFF_LEN 100

//------------------- Privates ---------------------------//


//------------------- Publics ----------------------------//


//--------------------------------------------------------//
bool common_EvalStatus(lua_State* l, int stat, const char* msg)
{
    bool has_error = false;
    if (stat >= LUA_ERRRUN)
    {
        has_error = true;
        const char* sstat = common_StatusToString(stat);

        if (stat <= LUA_ERRFILE) // internal lua error
        {
            // Get error message on stack if provided
            if (lua_gettop(l) > 0)
            {
                logger_Log(LVL_ERROR, "Status:%s errmsg:%s", sstat, lua_tostring(l, -1));
                lua_pop(l, 1); // remove it
            }
            else
            {
                logger_Log(LVL_ERROR, "Status:%s msg:%s", sstat, msg);
            }
            lua_error(l); // never returns...
        }
        else // assume nebulua error
        {
            logger_Log(LVL_ERROR, "Status:%s msg:%s", sstat, msg);
            lua_error(l); // never returns...
        }
    }

    return has_error;
}


//--------------------------------------------------------//
const char* common_StatusToString(int err)
{
    const char* serr = NULL;
    switch(err)
    {
        case NEB_OK: serr = "NEB_OK"; break;
        case NEB_ERR_BAD_APP_ARG: serr = "NEB_ERR_BAD_APP_ARG"; break;
        case NEB_ERR_BAD_LUA_ARG: serr = "NEB_ERR_BAD_LUA_ARG"; break;
        case NEB_ERR_BAD_MIDI_CFG: serr = "NEB_ERR_BAD_MIDI_CFG"; break;
        default: serr = diag_LuaStatusToString(err); break; // lua error?
    }
    return serr == NULL ? "No error string" : serr;
}


//////////////////////////////////////////////////////////

//--------------------------------------------------------//
bool common_StrToDouble(const char* str, double* val)
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
