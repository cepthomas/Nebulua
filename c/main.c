
// #include <windows.h>
// #include <cstdio>
// #include <cstring>
// #include <stdlib.h>
// #include <unistd.h>


#include <stdio.h>
#include <string.h>
#include <time.h>
#include <stdlib.h>
#include <unistd.h>

#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"

// #include "common.h"
// #include "exec.h"

#include "luainterop.h"
#include "luainteropwork.h"

#include "logger.h"
#include "ftimer.h"
#include "stopwatch.h"


// The main Lua thread.
static lua_State* p_lmain;

// The Lua thread where the script is running.
static lua_State* p_lscript;

// The script execution status.
static bool p_script_running = false;

// Processing loop status.
static bool p_loop_running;

// Last tick time.
static unsigned long p_last_usec;

/// Helper macro to check/log stack size. TODO keep stuff in luautils.*?
#define EVAL_STACK(L, expected)  { int num = lua_gettop(L); if (num != expected) { logger_Log(LVL_DEBUG, __FILE__, __LINE__, "Expected %d stack but is %d", expected, num); } }

/// Helper macro to check then handle error..
#define PROCESS_LUA_ERROR(L, err, fmt, ...)  // if(err >= LUA_ERRRUN) { luautils_LuaError(L, __FILE__, __LINE__, err, fmt, ##__VA_ARGS__); }


/////////////////// ftimer test stuff //////////////////
static double p_last_msec = 0.0;
#define TEST_COUNT 100
double p_test_res_1[TEST_COUNT]; // driver says
double p_test_res_2[TEST_COUNT]; // I say
int p_test_index = 0;
//-------------------------------------------------------//
void PeriodicInterruptFunc(double msec)
{
    //- TODO See lua.c for a way to treat C signals, which you may adapt to your interrupts.

    if(p_test_index < TEST_COUNT)
    {
        double em = stopwatch_TotalElapsedMsec();
        p_test_res_1[p_test_index] = msec;
        p_test_res_2[p_test_index] = em - p_last_msec;
        p_last_msec = em;
        p_test_index++;
    }
    else
    {
        // Stop.
        ftimer_Run(0);
    }
}



/////////////// proto /////////////
int exec_Init(void);
int exec_Run(const char* fn);
// void PeriodicInterruptFunc(double msec);
// Sleep for msec.
// @param msec How long.
static void p_Sleep(int msec);





//----------------------------------------------------//
// Main entry for the application. Process args and start system.
// @param argc How many args.
// @param argv The args.
// @return Standard exit code.
int main(int argc, char* argv[])
{
    int ret = 0;


    /////////////////// stopwatch test stuff //////////////////
    ret = stopwatch_Init();
    double msec = stopwatch_ElapsedMsec();

    stopwatch_Reset();
    msec = stopwatch_ElapsedMsec();

    sleep(1);

    msec = stopwatch_ElapsedMsec();
    //UT_CLOSE(stopwatch_ElapsedMsec(), 1000.0, 5.0); // because sleep() is sloppy


    /////////////////// ftimer test stuff //////////////////
    ret = ftimer_Init(PeriodicInterruptFunc, 10);

    // Grab the stopwatch time.
    p_last_msec = stopwatch_TotalElapsedMsec();

    // Go.
    ret = ftimer_Run(17);

    int timeout = 3;
    while(ftimer_IsRunning())// && timeout > 0)
    {
        sleep(1);
        timeout--;
    }

    ftimer_Destroy();

    // Check what happened.
    double vmin_1 = 1000;
    double vmax_1 = 0;
    double vmin_2 = 1000;
    double vmax_2 = 0;

    for(int i = 0; i < TEST_COUNT; i++)
    {
        double v = p_test_res_1[i];
        vmin_1 = v < vmin_1 ? v : vmin_1;
        vmax_1 = v > vmax_1 ? v : vmax_1;

        v = p_test_res_2[i];
        vmin_2 = v < vmin_2 ? v : vmin_2;
        vmax_2 = v > vmax_2 ? v : vmax_2;

        //printf("%g\n", p_test_res[i]);
    }

    //printf("max_1:%g min_1:%g max_2:%g min_2:%g\n", vmax_1, vmin_1, vmax_2, vmin_2);




    ////////////////////// original start //////////////////
    if(argc == 2)
    {
        if(exec_Init() == 0)
        {
            // Blocks forever.
            if(exec_Run(argv[1]) != 0)
            {
                // Bad thing happened.
                ret = 3;
                printf("!!! exec_run() failed\n");
            }
        }
        else
        {
            ret = 2;
            printf("!!! exec_init() failed\n");
        }
    }
    else
    {
        ret = 1;
        printf("!!! invalid args\n");
    }


    return ret;
}


/////////////////////////////////////////////////////////////////////

//----------------------------------------------------//
int exec_Init(void)
{
    int stat = 0;

    // Init stuff.
    logger_Init(".\\cel_log.txt");
    logger_SetFilters(LVL_DEBUG);
    p_loop_running = false;
    p_lmain = luaL_newstate();
    EVAL_STACK(p_lmain, 0);

    // Load std libraries.
    luaL_openlibs(p_lmain);
    EVAL_STACK(p_lmain, 0);

/* this:
    // Set up all board-specific stuff.
    stat = board_Init();
    stat = board_CliOpen(0);
    stat = board_RegDigInterrupt(p_DigInputHandler);

    // Init outputs.
    stat = board_WriteDig(DIG_OUT_1, true);
    stat = board_WriteDig(DIG_OUT_2, false);
    stat = board_WriteDig(DIG_OUT_3, true);

    p_last_usec = board_GetCurrentUsec();
    EVAL_STACK(p_lmain, 0);
*/

    return stat;
}


//---------------------------------------------------//
int exec_Run(const char* fn)
{
    int stat = 0;
    int lua_stat = 0;
    EVAL_STACK(p_lmain, 0);

    // Let her rip!
//    board_EnableInterrupts(true);
    p_loop_running = true;

//    p_Usage();

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

    // Give it some data. 
    lua_pushstring(p_lscript, "Hey diddle diddle");
    lua_setglobal(p_lscript, "script_string");
    lua_pushinteger(p_lscript, 90309);
    lua_setglobal(p_lscript, "script_int");
    EVAL_STACK(p_lscript, 1);

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
                    LOG_DEBUG("===LUA_YIELD.");
                    break;

                case LUA_OK:
                    // Script complete now.
                    break;

                default:
                    // Unexpected error.
                    PROCESS_LUA_ERROR(p_lscript, lua_stat, "exec_Run() error");
                    break;
            }

            p_Sleep(200);
        }
        while (lua_stat == LUA_YIELD);

        ///// Then loop forever doing cli requests. /////
        do
        {
            // stat = board_CliReadLine(p_cli_buf, CLI_BUFF_LEN);
            // if(stat == 0 && strlen(p_cli_buf) > 0)
            // {
            //     // LOG_DEBUG("|||got:%s", p_cli_buf);
            //     stat = p_ProcessCommand(p_cli_buf);
            // }
            // p_Sleep(100);
        } while (p_script_running);

        ///// Script complete now. /////
//        board_CliWriteLine("Finished script.");
    }
    else
    {
        PROCESS_LUA_ERROR(p_lscript, lua_stat, "exec_Run() error");
    }

    ///// Done, close up shop. /////
    EVAL_STACK(p_lmain, 0);
    EVAL_STACK(p_lscript, 0);

//    board_CliWriteLine("Goodbye - come back soon!");
//    board_EnableInterrupts(false);
    lua_close(p_lmain);

    return stat == 0;// RS_ERR ? 1 : 0;
}



//--------------------------------------------------------//
void p_Sleep(int msec)
{
    struct timespec ts;
    ts.tv_sec = msec / 1000;
    ts.tv_nsec = (msec % 1000) * 1000000;
    nanosleep(&ts, NULL);
}


///////////////////////////////////////////////////////////////////////////////////


