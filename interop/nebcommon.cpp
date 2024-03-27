#include <wchar.h>
#include <vcclr.h>
#include "nebcommon.h"

using namespace System;

//--------------------------------------------------------//
bool ToCString(String^ input, char* output, int len)
{
    bool ok = true;
    // https://learn.microsoft.com/en-us/cpp/dotnet/how-to-access-characters-in-a-system-string?view=msvc-170
    // not: const char* str4 = context->marshal_as<const char*>(input);
    interior_ptr<const wchar_t> ppchar = PtrToStringChars(input);
    int i = 0;
    for (; *ppchar != L'\0' && i < len - 1 && ok; ++ppchar, i++)
    {
        int c = wctob(*ppchar);
        if (c != -1)
        {
            output[i] = c;

        }
        else
        {
            ok = false;
            output[i] = '?';
        }
    }
    output[i] = 0; // terminate
    return ok;
}

//--------------------------------------------------------//
String^ ToCliString(const char* input)
{
    return gcnew String(input);
}

//--------------------------------------------------------//
String^ nebcommon_EvalStatus(lua_State* l, int stat, String^ info)
{
    String^ sret = gcnew String("");

    if (stat >= LUA_ERRRUN)
    {
        // Error string.
        const char* sstat = NULL;
        switch (stat)
        {
            // generic
            case 0:                         sstat = "NO_ERR"; break;
            // lua 0-6
            case LUA_YIELD:                 sstat = "LUA_YIELD"; break;
            case LUA_ERRRUN:                sstat = "LUA_ERRRUN"; break;
            case LUA_ERRSYNTAX:             sstat = "LUA_ERRSYNTAX"; break; // syntax error during pre-compilation
            case LUA_ERRMEM:                sstat = "LUA_ERRMEM"; break; // memory allocation error
            case LUA_ERRERR:                sstat = "LUA_ERRERR"; break; // error while running the error handler function
            case LUA_ERRFILE:               sstat = "LUA_ERRFILE"; break; // couldn't open the given file
            // app 10-?
            case NEB_ERR_INTERNAL:          sstat = "NEB_ERR_INTERNAL"; break;
            case NEB_ERR_BAD_CLI_ARG:       sstat = "NEB_ERR_BAD_CLI_ARG"; break;
            case NEB_ERR_BAD_LUA_ARG:       sstat = "NEB_ERR_BAD_LUA_ARG"; break;
            case NEB_ERR_BAD_MIDI_CFG:      sstat = "NEB_ERR_BAD_MIDI_CFG"; break;
            case NEB_ERR_SYNTAX:            sstat = "NEB_ERR_SYNTAX"; break;
            case NEB_ERR_MIDI_RX:           sstat = "NEB_ERR_MIDI_RX"; break;
            case NEB_ERR_MIDI_TX:           sstat = "NEB_ERR_MIDI_TX"; break;
            case NEB_ERR_API:               sstat = "NEB_ERR_API"; break;
            case NEB_ERR_RUN:               sstat = "NEB_ERR_RUN"; break;
            case NEB_ERR_FILE:              sstat = "NEB_ERR_FILE"; break;
            // default
            default:                        sstat = "UNKNOWN_ERROR"; break;
            //default:                        sstat = "UNKNOWN_ERROR"; LOG_DEBUG("Unknwon ret code:%d", stat); break;
        }

        // Maybe lua error message.
        const char* smsg = "";
        if (stat <= LUA_ERRFILE && l != NULL && lua_gettop(l) > 0)
        {
            smsg = lua_tostring(l, -1);
            lua_pop(l, 1);
            sret = String::Format(gcnew String("stat:{0} info:{1}\n{2}"), gcnew String(sstat), info, gcnew String(smsg));
        }
        else
        {
            sret = String::Format(gcnew String("stat:{0} info:{1}"), gcnew String(sstat), info);
        }
    }

    return sret;
}

// //--------------------------------------------------------//
// const char* nebcommon_FormatMidiStatus(int mstat)
// {
//     static char buff[BUFF_LEN];
//     buff[0] = 0;
//     if (mstat != MMSYSERR_NOERROR)
//     {
//         // Get the lib supplied text from mmeapi.h.
//         midiInGetErrorText(mstat, buff, BUFF_LEN);
//         if (strlen(buff) == 0)
//         {
//             snprintf(buff, BUFF_LEN - 1, "MidiStatus:%d", mstat);
//         }
//     }
//
//     return buff;
// }


// //--------------------------------------------------------//
// const char* nebcommon_FormatBarTime(int tick)
// {
//     static char buff[BUFF_LEN];
//     int bar = BAR(tick);
//     int beat = BEAT(tick);
//     int sub = SUB(tick);
//     snprintf(buff, BUFF_LEN - 1, "%d:%d:%d", bar, beat, sub);
//
//     return buff;
// }


// //--------------------------------------------------------//
// int nebcommon_ParseBarTime(const char* sbt)
// {
//     int tick = 0;
//     bool valid = false;
//     int v;
//
//     // Make writable copy and tokenize it.
//     char cp[32];
//     strncpy(cp, sbt, sizeof(cp) - 1);
//
//     char* tok = strtok(cp, ":");
//     if (tok != NULL)
//     {
//         valid = luautils_ParseInt(tok, &v, 0, 9999);
//         if (!valid) goto nogood;
//         tick += v * SUBS_PER_BAR;
//     }
//
//     tok = strtok(NULL, ":");
//     if (tok != NULL)
//     {
//         valid = luautils_ParseInt(tok, &v, 0, BEATS_PER_BAR - 1);
//         if (!valid) goto nogood;
//         tick += v * SUBS_PER_BEAT;
//     }
//
//     tok = strtok(NULL, ":");
//     if (tok != NULL)
//     {
//         valid = luautils_ParseInt(tok, &v, 0, SUBS_PER_BEAT - 1);
//         if (!valid) goto nogood;
//         tick += v;
//     }
//
//     return tick;
//
// nogood:
//     return -1;
// }
