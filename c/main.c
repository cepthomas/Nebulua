#include <windows.h>
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <stdlib.h>
#include <unistd.h>
#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"
#include "luainterop.h"
#include "luainteropwork.h"
#include "logger.h"
#include "ftimer.h"
#include "stopwatch.h"


// The main Lua thread - C code incl interrupts (mm/fast timer, midi events)
static lua_State* p_lmain;

// The Lua thread where the script is running.
static lua_State* p_lscript;

// The script execution status.
static bool p_script_running = false;

// Processing loop status.
static bool p_loop_running;

// Last tick time.
// static unsigned long p_last_usec;
static double p_last_msec = 0;

/// Helper macro to check/log stack size. TODO keep other stuff in luautils.*?
#define EVAL_STACK(L, expected)  { int num = lua_gettop(L); if (num != expected) { logger_Log(LVL_DEBUG, __FILE__, __LINE__, "Expected %d stack but is %d", expected, num); } }

/// Helper macro to check then handle error..
#define PROCESS_LUA_ERROR(L, err, fmt, ...)  // if(err >= LUA_ERRRUN) { luautils_LuaError(L, __FILE__, __LINE__, err, fmt, ##__VA_ARGS__); }



//-------------------------------------------------------//
// Tick corresponding to bpm. Interrupt!
void p_MidiClockFunc(double msec)
{
    // TODO yield over to script thread and call its step function.
    // TODO See lua.c for a way to treat C signals, which you may adapt to your interrupts.

}

//--------------------------------------------------------//
// Handle incoming messages. Interrupt!
void p_MidiInFunc(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2)
{
    // hMidiIn - Handle to the MIDI input device.
    // wMsg - MIDI input message.
    // dwInstance - Instance data supplied with the midiInOpen function.
    // dwParam1 - Message parameter.
    // dwParam2 - Message parameter.

    switch(message)
    {
        case MIM_DATA://MidiInterop.MidiInMessage.Data:
            // parameter 1 is packed MIDI message
            // parameter 2 is milliseconds since MidiInStart
            MessageReceived(this, new MidiInMessageEventArgs(messageParameter1.ToInt32(), messageParameter2.ToInt32()));
            break;

        case MIM_ERROR://MidiInterop.MidiInMessage.Error:
            // parameter 1 is invalid MIDI message
            ErrorReceived(this, new MidiInMessageEventArgs(messageParameter1.ToInt32(), messageParameter2.ToInt32()));
            break;

        case MIM_OPEN://MidiInterop.MidiInMessage.Open:
        case MIM_CLOSE://MidiInterop.MidiInMessage.Close:
        case MIM_LONGDATA://MidiInterop.MidiInMessage.LongData:
        case MIM_LONGERROR://MidiInterop.MidiInMessage.LongError:
        case MIM_MOREDATA:// MidiInterop.MidiInMessage.MoreData:
            break;
    }
};


//--------------------------------------------------------//
// Blocking sleep.
void p_Sleep(int msec)
{
    struct timespec ts;
    ts.tv_sec = msec / 1000;
    ts.tv_nsec = (msec % 1000) * 1000000;
    nanosleep(&ts, NULL);
}


//----------------------------------------------------//
int p_Init(void)
{
    int ret = 0;

    // Init internal stuff.
    p_loop_running = false;
    p_lmain = luaL_newstate();
    EVAL_STACK(p_lmain, 0);

    // Load std libraries.
    luaL_openlibs(p_lmain);
    EVAL_STACK(p_lmain, 0);

    // Stopwatch.
    ret = stopwatch_Init();
    p_last_msec = stopwatch_TotalElapsedMsec();

    // Tempo timer and interrupt.
    ret = ftimer_Init(p_MidiClockFunc, 10);
    ret = ftimer_Run(17); // tempo from ???

    // Midi event interrupt.

    return ret;
}


//---------------------------------------------------//
int p_Run(const char* fn)
{
    int stat = 0;
    int lua_stat = 0; check all!
    EVAL_STACK(p_lmain, 0);

    p_loop_running = true;

    // Set up a second Lua thread so we can background execute the script.
    p_lscript = lua_newthread(p_lmain);
    EVAL_STACK(p_lscript, 0);
    lua_pop(p_lmain, 1); // from lua_newthread()
    EVAL_STACK(p_lmain, 0);

    // Open std libs.
    luaL_openlibs(p_lscript);

    // Load app stuff. This table gets pushed on the stack and into globals.
    luainterop_Load(p_lscript);
    EVAL_STACK(p_lscript, 1);

    // Pop the table off the stack as it interferes with calling the module function.
    lua_pop(p_lscript, 1);
    EVAL_STACK(p_lscript, 0);

    // Now load the script/file we are going to run.
    // lua_load() pushes the compiled chunk as a Lua function on top of the stack.
    lua_stat = luaL_loadfile(p_lscript, fn);

    // // Give it some data. 
    // lua_pushstring(p_lscript, "Hey diddle diddle");
    // lua_setglobal(p_lscript, "script_string");
    // lua_pushinteger(p_lscript, 90309);
    // lua_setglobal(p_lscript, "script_int");
    // EVAL_STACK(p_lscript, 1);

    // Priming run of the loaded Lua script to create the script's global variables
    lua_stat = lua_pcall(p_lscript, 0, 0, 0);
    EVAL_STACK(p_lscript, 0);

    if (lua_stat != LUA_OK)
    {
        LOG_ERROR("lua_pcall() error code %i: %s", lua_stat, lua_tostring(p_lscript, -1));
    }

    if(lua_stat == LUA_OK)
    {
        // Init the script. This also starts blocking execution.
        p_script_running = true;

        int gtype = lua_getglobal(p_lscript, "do_it");
        EVAL_STACK(p_lscript, 1);

        ///// First do some yelding. /////
        do
        {
            lua_stat = lua_resume(p_lscript, p_lmain, 0, 0);

            switch(lua_stat)
            {
                case LUA_YIELD:
                    // LOG_DEBUG("===LUA_YIELD.");
                    break;

                case LUA_OK:
                    // Script complete now.
                    break;

                default:
                    // Unexpected error.
                    PROCESS_LUA_ERROR(p_lscript, lua_stat, "p_Run() error");
                    break;
            }

            p_Sleep(200);
        }
        while (lua_stat == LUA_YIELD);

        /// Then loop forever doing cli requests. /////
        do
        {
            stat = board_CliReadLine(p_cli_buf, CLI_BUFF_LEN);
            if(stat == 0 && strlen(p_cli_buf) > 0)
            {
                // LOG_DEBUG("|||got:%s", p_cli_buf);
                stat = p_ProcessCommand(p_cli_buf);
            }
            p_Sleep(100);
        } while (p_script_running);

        ///// Script complete now. /////
        LOG_INFO("Finished script");
    }
    else
    {
        PROCESS_LUA_ERROR(p_lscript, lua_stat, "p_Run() error");
    }


    return stat;
}


//----------------------------------------------------//
// Main entry for the application. Process args and start system.
// @param argc How many args.
// @param argv The args.
// @return Standard exit code.
int main(int argc, char* argv[])
{
    int ret = 0;

    logger_Init(".\\cel_log.txt");
    logger_SetFilters(LVL_DEBUG);


    ret = p_Init();

    // Go.
    char* serr = NULL;
    char* sfn = NULL;

    if(argc == 2)
    {
        sfn = argv[1];
    }
    else
    {
        serr = "Invalid args";
    }

    if (serr == NULL && sfn != NULL)
    {
        if(p_Init() != 0)
        {
            serr = "Init failed";
        }
    }

    if (serr == NULL && sfn != NULL)
    {
        // Run the script file. Blocks forever. TODO need elegant way to stop - part of user interaction or ctrl-C?
        if(p_Run(argv[1]) != 0)
        {
            serr = "Run failed";
        }
    }

    // How did we do?
    if (serr != NULL)
    {
        LOG_ERROR(serr);
        printf("Epic fail!! %s\n", serr);
    }

    // Clean up and go home.
    // EVAL_STACK(p_lmain, 0);
    // EVAL_STACK(p_lscript, 0);
    ftimer_Run(0);
    ftimer_Destroy();
    lua_close(p_lscript);
    lua_close(p_lmain);

    return serr == NULL ? 0 : 1;
}
