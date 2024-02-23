#include <cstdio>
#include <string>
#include <cstring>
#include <sstream>
#include <vector>
#include <iostream>

#include <windows.h>
#include "pnut.h"

extern "C"
{
#include "nebcommon.h"
#include "cbot.h"
#include "devmgr.h"
#include "cli.h"
#include "logger.h"

int _DoCli(void);
}


// For mock cli.
//char _next_response[MAX_LINE_LEN];

// For mock cli.
char _next_command[MAX_LINE_LEN];
/// Collected output lines.
std::vector<std::string> response_lines = {};


// const char* _my_midi_in1  = "loopMIDI Port";
// const char* _my_midi_out1 = "Microsoft GS Wavetable Synth";


//--------------------------------------------------------//
// TODO1 test these:
//static void _MidiInHandler(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2);
//static void _MidiClockHandler(double msec);
//static int _DoCli(void);
//static int exec_Main(const char* script_fn); // entry, init, cleanup + call _Forever()


/////////////////////////////////////////////////////////////////////////////
UT_SUITE(EXEC_CLI, "Test cli functions.")
{
    int stat = 0;

    // temp - remove
    response_lines.clear();
    strncpy(_next_command, "tempo 182", MAX_LINE_LEN);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(response_lines.size(), 1);
    UT_STR_EQUAL(response_lines[0].c_str(), "$");
    UT_STR_EQUAL(response_lines[1].c_str(), "xxx\n");





    // Fat fingers.
    response_lines.clear();
    strncpy(_next_command, "bbbbb", MAX_LINE_LEN);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(response_lines.size(), 2);
    UT_STR_EQUAL(response_lines[0].c_str(), "$");
    UT_STR_EQUAL(response_lines[1].c_str(), "Invalid command\n");

    // Fat fingers.
    response_lines.clear();
    strncpy(_next_command, "z", MAX_LINE_LEN);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(response_lines.size(), 2);
    UT_STR_EQUAL(response_lines[0].c_str(), "$");
    UT_STR_EQUAL(response_lines[1].c_str(), "Invalid command\n");


    // These next two confirm proper full/short name handling.
    response_lines.clear();
    strncpy(_next_command, "help", MAX_LINE_LEN);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(response_lines.size(), 12);
    UT_STR_EQUAL(response_lines[0].c_str(), "$");
    UT_STR_EQUAL(response_lines[1].c_str(), "help|?: tell me everything\n");
    UT_STR_EQUAL(response_lines[2].c_str(), "exit|x: exit the application\n");
    UT_STR_EQUAL(response_lines[11].c_str(), "reload|l: re/load current script\n");

    response_lines.clear();
    strncpy(_next_command, "?", MAX_LINE_LEN);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(response_lines.size(), 12);
    UT_STR_EQUAL(response_lines[0].c_str(), "$");
    UT_STR_EQUAL(response_lines[1].c_str(), "help|?: tell me everything\n");

    // The rest of the commands.
    response_lines.clear();
    strncpy(_next_command, "exit", MAX_LINE_LEN);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(response_lines.size(), 1);
    UT_STR_EQUAL(response_lines[0].c_str(), "$");

    response_lines.clear();
    strncpy(_next_command, "run", MAX_LINE_LEN);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(response_lines.size(), 1);
    UT_STR_EQUAL(response_lines[0].c_str(), "$");

    response_lines.clear();
    strncpy(_next_command, "kill", MAX_LINE_LEN);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(response_lines.size(), 1);
    UT_STR_EQUAL(response_lines[0].c_str(), "$");

    response_lines.clear();
    strncpy(_next_command, "reload", MAX_LINE_LEN);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(response_lines.size(), 1);
    UT_STR_EQUAL(response_lines[0].c_str(), "$");

    response_lines.clear();
    strncpy(_next_command, "tempo", MAX_LINE_LEN);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(response_lines.size(), 2);
    UT_STR_EQUAL(response_lines[0].c_str(), "$");
    UT_STR_EQUAL(response_lines[1].c_str(), "xxx\n");

    response_lines.clear();
    strncpy(_next_command, "tempo 182", MAX_LINE_LEN);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(response_lines.size(), 1);
    UT_STR_EQUAL(response_lines[0].c_str(), "$");
    UT_STR_EQUAL(response_lines[1].c_str(), "xxx\n");

    response_lines.clear();
    strncpy(_next_command, "tempo 242", MAX_LINE_LEN);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(response_lines.size(), 2);
    UT_STR_EQUAL(response_lines[0].c_str(), "$");
    UT_STR_EQUAL(response_lines[1].c_str(), "xxx\n");

    response_lines.clear();
    strncpy(_next_command, "tempo 39", MAX_LINE_LEN);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(response_lines.size(), 2);
    UT_STR_EQUAL(response_lines[0].c_str(), "$");
    UT_STR_EQUAL(response_lines[1].c_str(), "xxx\n");

    response_lines.clear();
    strncpy(_next_command, "monitor in", MAX_LINE_LEN);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(response_lines.size(), 1);
    UT_STR_EQUAL(response_lines[0].c_str(), "$");

    response_lines.clear();
    strncpy(_next_command, "monitor out", MAX_LINE_LEN);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(response_lines.size(), 1);
    UT_STR_EQUAL(response_lines[0].c_str(), "$");

    response_lines.clear();
    strncpy(_next_command, "monitor off", MAX_LINE_LEN);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(response_lines.size(), 1);
    UT_STR_EQUAL(response_lines[0].c_str(), "$");

    response_lines.clear();
    strncpy(_next_command, "monitor junk", MAX_LINE_LEN);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(response_lines.size(), 2);
    UT_STR_EQUAL(response_lines[0].c_str(), "$");
    UT_STR_EQUAL(response_lines[1].c_str(), "xxx\n");

    response_lines.clear();
    strncpy(_next_command, "position", MAX_LINE_LEN);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(response_lines.size(), 2);
    UT_STR_EQUAL(response_lines[0].c_str(), "$");
    UT_STR_EQUAL(response_lines[1].c_str(), "xxx\n");

    response_lines.clear();
    strncpy(_next_command, "position 203:2:6", MAX_LINE_LEN);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(response_lines.size(), 1);
    UT_STR_EQUAL(response_lines[0].c_str(), "$");

    response_lines.clear();
    strncpy(_next_command, "position 111:9:6", MAX_LINE_LEN);
    stat = _DoCli();
    UT_EQUAL(stat, NEB_OK);
    UT_EQUAL(response_lines.size(), 2);
    UT_STR_EQUAL(response_lines[0].c_str(), "$");
    UT_STR_EQUAL(response_lines[1].c_str(), "xxx\n");



    //static cli_command_t _commands[] =
    //{
    //    { "tempo",      't',   0,    "get or set the tempo",                   "(bpm): 40-240",             _TempoCmd },
    //    { "monitor",    'm',   '^',  "toggle monitor midi traffic",            "(in|out|off): action",      _MonCmd },
    //    { "position",   'p',   0,    "set position to where or tell current",  "(where): bar.beat.sub",     _PositionCmd },
    //};


    // Commands that need user input.



    //for (std::vector<std::string>::iterator iter = response_lines.begin(); iter != response_lines.end(); ++iter)
    //{
    //    printf(iter->c_str());
    //}


    return 0;
}


////////////////////////////////////// mock cli ////////////////////////////////

extern "C"
{
int cli_open()
{
    //_next_response[0] = 0;
    _next_command[0] = 0;
    response_lines.clear();
    return 0;
}


int cli_close()
{
    // Nothing to do.
    return 0;
}


int cli_printf(const char* format, ...)
{
    // Format string.
    char line[MAX_LINE_LEN];
    va_list args;
    va_start(args, format);
    vsnprintf(line, MAX_LINE_LEN, format, args);
    va_end(args);

    std::string str(line);
    response_lines.push_back(str);

    return 0;
}


char* cli_gets(char* buff, int len)
{
    if (strlen(_next_command) > 0)
    {
        strncpy(buff, _next_command, len);
        return buff;
    }
    else
    {
        return NULL;
    }
}
}
