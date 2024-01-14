
#include <time.h>
#include <sys/time.h>
#include <string.h>
#include <conio.h>
#include <stdarg.h>
#include <stdbool.h>
#include <stdio.h>
#include "cli.h"


// TODO1 refactor and put in cbot plus these:
//  - also handle e.g. immediate single space bar.
//  - Chop up the command line into args and return those.
//  - handle sock.c, serial port, etc?  https://en.cppreference.com/w/c/io
//  - unit test

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

/// ---- kludgy, fix. enum?
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

    // // Chop up the command line into args.
    // #define MAX_NUM_ARGS 20
    // const char* argv[MAX_NUM_ARGS];
    // int argc = 0;

    // // Make writable copy and tokenize it.
    // char cp[strlen(line) + 1];
    // strcpy(cp, line);
    // char* tok = strtok(cp, " ");
    // while(tok != NULL && argc < MAX_NUM_ARGS)
    // {
    //     argv[argc++] = tok;
    //     tok = strtok(NULL, " ");
    // }

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
                    // Notify.
                    ret = _cli_buff;
                    _line_done = true;
                }
                break;
        }
    }

    return ret;
}


//--------------------------------------------------------//
int cli_WriteLine(const char* format, ...)
{
    int stat = 0;

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
