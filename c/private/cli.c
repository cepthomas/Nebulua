
#include <time.h>
#include <sys/time.h>
#include <string.h>
#include <conio.h>
#include <stdarg.h>
#include <stdbool.h>
#include <stdio.h>
#include "cli.h"
#include "common.h"

// TODO3 put this in lbot or cbot.
// TODO3 serial port, etc?  https://en.cppreference.com/w/c/io


//---------------- Private ------------------------------//

/// CLI buffer to collect input chars.
static char p_cli_buff[CLI_BUFF_LEN];

/// CLI propmpt.
static char* p_Prompt = "";

/// TODO3 kludgy, fix.
static bool p_Stdio = true;


//---------------- API Implementation -----------------//

//--------------------------------------------------------//
int cli_Open(char type) //, cli_command_t[] cmds)
{
    int stat = 0;

    //p_cmds = cmds;

    if (type == 's')
    {
        p_Stdio = true;
        p_Prompt = "$";
    }
    else
    {
        p_Stdio = false;
        p_Prompt = "";
    }

    memset(p_cli_buff, 0, CLI_BUFF_LEN);
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
bool cli_ReadLine(char* buff, int num)
{
    bool ready = false;

    // Default.
    buff[0] = 0;

    // Process each char.
    char c = -1;
    bool done = false;

    while (!done)
    {
        if (p_Stdio)
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
                strncpy(buff, p_cli_buff, num);
                ready = true;

                // Clear buffer.
                memset(p_cli_buff, 0, CLI_BUFF_LEN);

                // Echo prompt.
                //cli_WriteLine("");
                break;

            default:
                // Echo char.
                putchar(c);
                
                // Save it.
                p_cli_buff[strlen(p_cli_buff)] = c;
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

    if (p_Stdio)
    {
        printf("%s\r\n>", buff);
    }
    else // telnet
    {
        // fputs(buff, p_CliOut);
        // fputs("\r\n", p_CliOut);
        // fputs(p_Prompt, p_CliOut);
    }

    return stat;    
}


//--------------------------------------------------------//
int cli_WriteChar(char c)
{
    int stat = 0;

    if (p_Stdio)
    {
        putchar(c);
    }
    else // telnet
    {
        // fputc();
    }

    return stat;    
}
