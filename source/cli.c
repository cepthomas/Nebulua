
#include <time.h>
#include <sys/time.h>
#include <string.h>
#include <conio.h>
#include <stdarg.h>
#include <stdbool.h>
#include <stdio.h>
#include "cli.h"


// TODO1 refactor and put in cbot plus these:
// TOCO1 handle e.g. immediate single space bar.
// TODO1 unit test

//---------------- Private ------------------------------//

/// Max raw line.
#define CLI_BUFF_LEN 128

/// CLI buffer to collect input chars.
static char _cli_buff[MAX_CLI_ARGS * MAX_CLI_ARG_LEN];

/// Buff status. -1 means empty.
static int _buff_index = -1;

/// CLI prompt.
static char* _prompt = "";

/// TODO1 kludgy, fix. enum? sock.c, serial port
static bool _stdio = true;


//---------------- API Implementation -----------------//

//--------------------------------------------------------//
int cli_Open(char type)
{
    int stat = 0;
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
const bool cli_ReadLine(cli_args_t* args)
{
    bool line_done = false;
    char* serr = NULL;


    // Process each new char.
    char c = -1;
    bool process_done = false;

    while (!process_done)// && ret == false)
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
                process_done = true;
                break;

            case '\n':
                // Ignore.
                break;

            case '\r':
                // Echo return.
                cli_WriteLine("");

                // Line process_done.
                line_done = true;

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
                    serr = "Command line too long";
                    line_done = true;
                }
                break;
        }

        if (line_done)
        {
            memset(args, 0, sizeof(cli_args_t));

            if (serr == NULL)
            {
                int argc = 0;

                // Chop up the raw command line into args.
                char* tok = strtok(_cli_buff, " ");
                while(tok != NULL && argc < MAX_CLI_ARGS && serr == NULL)
                {
                    if (strlen(tok) >= MAX_CLI_ARG_LEN)
                    {
                        serr = "Argument too long";
                    }
                    else
                    {
                        strncpy(args->arg_values[argc], tok, MAX_CLI_ARG_LEN-1);
                        argc++;
                        tok = strtok(NULL, " ");
                    }
                }
            }

            if (serr != NULL)
            {
                strncpy(args->arg_values[0], serr, MAX_CLI_ARG_LEN-1);
                args->arg_count = 0;
            }

            // Clear buffer.
            memset(_cli_buff, 0, CLI_BUFF_LEN);
            line_done = false;
            _buff_index = 0;

        }

    }

    return line_done;
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
