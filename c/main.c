
#include <stdio.h>
#include "lua.h"
// #include "common.h"
// #include "exec.h"



// The main Lua thread.
static lua_State* p_lmain;

// The Lua thread where the script is running.
static lua_State* p_lscript;

// The script execution status.
static bool p_script_running = false;

// Processing loop status.
static bool p_loop_running;

// Last tick time.
static uint64_t p_last_usec;

// Sleep for msec.
// @param msec How long.
static void p_Sleep(int msec);

int exec_Init(void);

int exec_Run(const char* fn);





//----------------------------------------------------//
// Main entry for the application. Process args and start system.
// @param argc How many args.
// @param argv The args.
// @return Standard exit code.
int main(int argc, char* argv[])
{
    int ret = 0;

    if(argc == 2)
    {
        if(exec_Init() == RS_PASS)
        {
            // Blocks forever.
            if(exec_Run(argv[1]) != RS_PASS)
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
    int stat = RS_PASS;

    // Init stuff.
    logger_Init(".\\cel_log.txt");
    logger_SetFilters(LVL_DEBUG);
    p_loop_running = false;
    p_lmain = luaL_newstate();
    EVAL_STACK(p_lmain, 0);

    // Load std libraries.
    luaL_openlibs(p_lmain);
    EVAL_STACK(p_lmain, 0);

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

    return stat;
}


//---------------------------------------------------//
int exec_Run(const char* fn)
{
    int stat = RS_PASS;
    int lua_stat = 0;
    EVAL_STACK(p_lmain, 0);

    // Let her rip!
    board_EnableInterrupts(true);
    p_loop_running = true;

    p_Usage();

    // Set up a second Lua thread so we can background execute the script.
    p_lscript = lua_newthread(p_lmain);
    EVAL_STACK(p_lscript, 0);
    lua_pop(p_lmain, 1); // from lua_newthread()
    EVAL_STACK(p_lmain, 0);

    // Open std libs.
    luaL_openlibs(p_lscript);

    // Load app stuff. This table gets pushed on the stack and into globals.
    interop_Load(p_lscript);
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
            lua_stat = lua_resume(p_lscript, 0, 0);

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
            stat = board_CliReadLine(p_cli_buf, CLI_BUFF_LEN);
            if(stat == RS_PASS && strlen(p_cli_buf) > 0)
            {
                // LOG_DEBUG("|||got:%s", p_cli_buf);
                stat = p_ProcessCommand(p_cli_buf);
            }
            p_Sleep(100);
        } while (p_script_running);

        ///// Script complete now. /////
        board_CliWriteLine("Finished script.");
    }
    else
    {
        PROCESS_LUA_ERROR(p_lscript, lua_stat, "exec_Run() error");
    }

    ///// Done, close up shop. /////
    EVAL_STACK(p_lmain, 0);
    EVAL_STACK(p_lscript, 0);

    board_CliWriteLine("Goodbye - come back soon!");
    board_EnableInterrupts(false);
    lua_close(p_lmain);

    return stat == RS_ERR ? 1 : RS_PASS;
}


//--------------------------------------------------------//
void p_Sleep(int msec)
{
    struct timespec ts;
    ts.tv_sec = msec / 1000;
    ts.tv_nsec = (msec % 1000) * 1000000;
    nanosleep(&ts, NULL);
}

