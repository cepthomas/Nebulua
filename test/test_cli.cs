using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks.PNUT;


namespace Nebulua.Test
{
    // UT_SUITE(CLI_MAIN, "Test the simpler cli functions.")

    public class CLI_MAIN : TestSuite
    {
        public override void RunSuite()
        {
            int stat = Defs.NEB_OK;
            string capture = "";
            var app = new App();
            app.HookCli();
            //app.Run(fn);

            ///// Fat fingers.
            app.CliOut.Capture.Clear();
            app.CliIn.NextLine = "bbbbb";
            stat = app.DoCli();
            UT_EQUAL(stat, Defs.NEB_OK);
            capture = app.CliOut.Capture.ToString();
            UT_EQUAL(capture.Length, 18);
            UT_EQUAL(capture, $"$Invalid command{Environment.NewLine}");

            app.CliOut.Capture.Clear();
            app.CliIn.NextLine = "z";
            stat = app.DoCli();
            UT_EQUAL(stat, Defs.NEB_OK);
            capture = app.CliOut.Capture.ToString();
            UT_EQUAL(capture.Length, 18);
            UT_EQUAL(capture, $"$Invalid command{Environment.NewLine}");



        }
    }
}


/*

/////////////////////////////////////////////////////////////////////////////
UT_SUITE(CLI_MAIN, "Test the simpler cli functions.")
{
    ///// These next two confirm proper full/short name handling.
    _response_lines.clear();
    strncpy(_next_command, "help", MAX_LINE_LEN - 1);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(_response_lines.size(), 12);
    UT_STR_EQUAL(_response_lines[0].c_str(), "$");
    UT_STR_EQUAL(_response_lines[1].c_str(), "help|?: tell me everything\n");
    UT_STR_EQUAL(_response_lines[2].c_str(), "exit|x: exit the application\n");
    UT_STR_EQUAL(_response_lines[11].c_str(), "reload|l: re/load current script\n");

    _response_lines.clear();
    strncpy(_next_command, "?", MAX_LINE_LEN - 1);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(_response_lines.size(), 12);
    UT_STR_EQUAL(_response_lines[0].c_str(), "$");
    UT_STR_EQUAL(_response_lines[1].c_str(), "help|?: tell me everything\n");

    ///// The rest of the commands.
    _response_lines.clear();
    strncpy(_next_command, "exit", MAX_LINE_LEN - 1);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(_response_lines.size(), 1);
    UT_STR_EQUAL(_response_lines[0].c_str(), "$");

    _response_lines.clear();
    strncpy(_next_command, "run", MAX_LINE_LEN - 1);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(_response_lines.size(), 1);
    UT_STR_EQUAL(_response_lines[0].c_str(), "$");

    _response_lines.clear();
    strncpy(_next_command, "reload", MAX_LINE_LEN - 1);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(_response_lines.size(), 1);
    UT_STR_EQUAL(_response_lines[0].c_str(), "$");

    _response_lines.clear();
    strncpy(_next_command, "tempo", MAX_LINE_LEN - 1);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(_response_lines.size(), 2);
    UT_STR_EQUAL(_response_lines[0].c_str(), "$");
    UT_STR_EQUAL(_response_lines[1].c_str(), "100\n");

    _response_lines.clear();
    strncpy(_next_command, "tempo 182", MAX_LINE_LEN - 1);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(_response_lines.size(), 1);
    UT_STR_EQUAL(_response_lines[0].c_str(), "$");

    _response_lines.clear();
    strncpy(_next_command, "tempo 242", MAX_LINE_LEN - 1);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_ERR_BAD_CLI_ARG);
    UT_EQUAL(_response_lines.size(), 2);
    UT_STR_EQUAL(_response_lines[0].c_str(), "$");
    UT_STR_EQUAL(_response_lines[1].c_str(), "invalid tempo: 242\n");

    _response_lines.clear();
    strncpy(_next_command, "tempo 39", MAX_LINE_LEN - 1);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_ERR_BAD_CLI_ARG);
    UT_EQUAL(_response_lines.size(), 2);
    UT_STR_EQUAL(_response_lines[0].c_str(), "$");
    UT_STR_EQUAL(_response_lines[1].c_str(), "invalid tempo: 39\n");

    _response_lines.clear();
    strncpy(_next_command, "monitor in", MAX_LINE_LEN - 1);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(_response_lines.size(), 1);
    UT_STR_EQUAL(_response_lines[0].c_str(), "$");

    _response_lines.clear();
    strncpy(_next_command, "monitor out", MAX_LINE_LEN - 1);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(_response_lines.size(), 1);
    UT_STR_EQUAL(_response_lines[0].c_str(), "$");

    _response_lines.clear();
    strncpy(_next_command, "monitor off", MAX_LINE_LEN - 1);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(_response_lines.size(), 1);
    UT_STR_EQUAL(_response_lines[0].c_str(), "$");

    _response_lines.clear();
    strncpy(_next_command, "monitor junk", MAX_LINE_LEN - 1);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_ERR_BAD_CLI_ARG);
    UT_EQUAL(_response_lines.size(), 2);
    UT_STR_EQUAL(_response_lines[0].c_str(), "$");
    UT_STR_EQUAL(_response_lines[1].c_str(), "invalid option: junk\n");

    lua_close(_l);

    return 0;
}


/////////////////////////////////////////////////////////////////////////////
UT_SUITE(CLI_CONTEXT, "Test cli functions that require a lua context.")
{
    int stat = 0;
    int iret = 0;

    ///// Need to load a real file for position stuff.
    lua_State* _l = luaL_newstate();

    // Load std libraries.
    luaL_openlibs(_l);

    // Load host funcs into lua space. This table gets pushed on the stack and into globals.
    luainterop_Load(_l);

    // Pop the table off the stack as it interferes with calling the module functions.
    lua_pop(_l, 1);

    // Load/compile the script file. Pushes the compiled chunk as a Lua function on top of the stack or pushes an error message.
    stat = luaL_loadfile(_l, "script_happy.lua");
    UT_EQUAL(stat, NEB_OK);
    const char* e = nebcommon_EvalStatus(_l, stat, "load script");
    UT_NULL(e);

    // Execute the loaded script to init everything.
    stat = lua_pcall(_l, 0, LUA_MULTRET, 0);
    UT_EQUAL(stat, NEB_OK);
    e = nebcommon_EvalStatus(_l, stat, "run script");
    UT_NULL(e);

    // Script setup function.
    stat = luainterop_Setup(_l, &iret);
    UT_EQUAL(stat, NEB_OK);
    e = nebcommon_EvalStatus(_l, stat, "setup()");
    UT_NULL(e);

    ///// Good to go now.
    _response_lines.clear();
    strncpy(_next_command, "position", MAX_LINE_LEN - 1);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(_response_lines.size(), 2);
    UT_STR_EQUAL(_response_lines[0].c_str(), "$");
    UT_STR_EQUAL(_response_lines[1].c_str(), "0:0:0\n");

    _response_lines.clear();
    strncpy(_next_command, "position 203:2:6", MAX_LINE_LEN - 1);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(_response_lines.size(), 2);
    UT_STR_EQUAL(_response_lines[0].c_str(), "$");
    UT_STR_EQUAL(_response_lines[1].c_str(), "203:2:6\n");

    _response_lines.clear();
    strncpy(_next_command, "position", MAX_LINE_LEN - 1);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(_response_lines.size(), 2);
    UT_STR_EQUAL(_response_lines[0].c_str(), "$");
    UT_STR_EQUAL(_response_lines[1].c_str(), "203:2:6\n");

    _response_lines.clear();
    strncpy(_next_command, "position 111:9:6", MAX_LINE_LEN - 1);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_ERR_BAD_CLI_ARG);
    UT_EQUAL(_response_lines.size(), 2);
    UT_STR_EQUAL(_response_lines[0].c_str(), "$");
    UT_STR_EQUAL(_response_lines[1].c_str(), "invalid position: 111:9:6\n");

    _response_lines.clear();
    strncpy(_next_command, "kill", MAX_LINE_LEN - 1);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(_response_lines.size(), 1);
    UT_STR_EQUAL(_response_lines[0].c_str(), "$");

    lua_close(_l);

    return 0;
}
*/
