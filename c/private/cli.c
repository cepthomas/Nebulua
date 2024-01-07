
#include <time.h>
#include <sys/time.h>
#include <string.h>
#include <conio.h>
#include <stdarg.h>
#include <stdbool.h>
#include <stdio.h>
#include "cli.h"
#include "nebcommon.h"

// TODO2 put this in lbot or cbot (actually refactor all).
// serial port, etc?  https://en.cppreference.com/w/c/io


//---------------- Private ------------------------------//

/// Max line.
#define CLI_BUFF_LEN 128

/// CLI buffer to collect input chars.
static char _cli_buff[CLI_BUFF_LEN];

/// 
static bool _line_done = false;

/// Buff status. -1 means empty.
static int _buff_index = -1;

/// CLI prompt.
static char* _prompt = "";

/// TODO2 kludgy, fix. See sock.c
static bool _stdio = true;


//---------------- API Implementation -----------------//

//--------------------------------------------------------//
int cli_Open(char type)
{
    int stat = 0;
    _line_done = false;
    _buff_index = -1;

    if (type == 's')
    {
        _stdio = true;
        _prompt = "$";
    }
    else
    {
        _stdio = false;
        _prompt = "";
    }

    memset(_cli_buff, 0, CLI_BUFF_LEN);

    // Prompt.
    cli_WriteLine("");
    return stat;
}


//--------------------------------------------------------//
int cli_Destroy(void)
{
    int stat = 0;

    return stat;
}


//--------------------------------------------------------//
const char* cli_ReadLine(void)
{
    const char* ret = NULL;

    if (_line_done) // reset from last go-around?
    {
        // Clear buffer.
        memset(_cli_buff, 0, CLI_BUFF_LEN);
        _line_done = false;
        _buff_index = 0;
    }


    // Process each available char.
    char c = -1;
    bool done = false;

    while (!done && ret == NULL)
    {
        if (_stdio)
        {
            c = _kbhit() ? (char)_getch() : -1;
        }
        else // telnet - see sock.c
        {
            // while ((c = fgetc(p_CliIn)) != EOF)
            // if (fread(&c, 1, 1, p_CliIn) > 0)
            c = -1;
        }

        switch(c)
        {
            case -1:
                done = true;
                break;

            case '\n':
                // Ignore.
                break;

            case '\r':
                // Echo return.
                cli_WriteLine("");

                // Line done.
                ret = _cli_buff;
                _line_done = true;

                // Echo prompt.
                //cli_WriteLine("");
                break;

            default:
                // Echo char.
                putchar(c);
                
                // Save it.
                _cli_buff[_buff_index++] = c;

                // Check for overrun.
                if (_buff_index >= CLI_BUFF_LEN - 1)
                {
                    // Truncate.
                    ret = _cli_buff;
                    _line_done = true;
                }
                break;
        }
    }

    return ret;
}


//--------------------------------------------------------//
bool cli_ReadLine_orig(char* buff, int num)
{
    bool ready = false;

    // Default.
    buff[0] = 0;

    // Process each char.
    char c = -1;
    bool done = false;

    while (!done)
    {
        if (_stdio)
        {
            c = _kbhit() ? (char)_getch() : -1;
        }
        else // telnet
        {
            // while ((c = fgetc(p_CliIn)) != EOF)
            // if (fread(&c, 1, 1, p_CliIn) > 0)
            c = -1;
        }

        switch(c)
        {
            case -1:
                done = true;
                break;

            case '\n':
                // Ignore.
                break;

            case '\r':
                // Echo return.
                cli_WriteLine("");

                // Copy to client buff. Should be 0 terminated.
                strncpy(buff, _cli_buff, num);
                ready = true;

                // Clear buffer.
                memset(_cli_buff, 0, CLI_BUFF_LEN);

                // Echo prompt.
                //cli_WriteLine("");
                break;

            default:
                // Echo char.
                putchar(c);
                
                // Save it.
                _cli_buff[strlen(_cli_buff)] = c;
                break;
        }
    }

    return ready;
}


//--------------------------------------------------------//
int cli_WriteLine(const char* format, ...)
{
    int stat = NEB_OK;

    static char buff[CLI_BUFF_LEN];

    va_list args;
    va_start(args, format);
    vsnprintf(buff, CLI_BUFF_LEN-1, format, args);
    va_end(args);

    if (_stdio)
    {
        printf("%s\r\n>", buff);
    }
    else // telnet
    {
        // fputs(buff, p_CliOut);
        // fputs("\r\n", p_CliOut);
        // fputs(_prompt, p_CliOut);
    }

    return stat;    
}


//--------------------------------------------------------//
int cli_WriteChar(char c)
{
    int stat = 0;

    if (_stdio)
    {
        putchar(c);
    }
    else // telnet
    {
        // fputc();
    }

    return stat;    
}
