
#include <time.h>
#include <sys/time.h>
#include <string.h>
#include <conio.h>
#include <stdarg.h>
#include <stdbool.h>
#include <stdio.h>
#include "cli.h"
#include "common.h"

// TODO3 uses FILE*/stdio, could add serial port, socket, etc. Make into a generic component?
// https://en.cppreference.com/w/c/io


//---------------- Private ------------------------------//

// CLI buffer to collect input chars.
static char p_cli_buff[CLI_BUFF_LEN];

// CLI propmpt.
static const char* p_Prompt = "$";

static FILE* p_CliIn;
static FILE* p_CliOut;


//---------------- Public Implementation -----------------//

//--------------------------------------------------------//
int cli_Open(void)
{
    int stat = NEB_OK;

    // p_CliIn = stdin;
    // p_CliOut = stdout;

    memset(p_cli_buff, 0, CLI_BUFF_LEN);

    // Prompt.
    cli_WriteLine("");

    return stat;
}


//--------------------------------------------------------//
int cli_Destroy(void)
{
    int stat = NEB_OK;

    return stat;
}


//--------------------------------------------------------//
bool cli_ReadLine(char* buff, int num)
{
    bool ready = false;

    // Default.
    buff[0] = 0;

    // Process each char.
    char c;
    // while ((c = fgetc(p_CliIn)) != EOF)
    // if (fread(&c, 1, 1, p_CliIn) > 0)
    if (_kbhit())
    {
        char c = (char)_getch();
        switch(c)
        {
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

    // fputs(buff, p_CliOut);
    // fputs("\r\n", p_CliOut);
    // fputs(p_Prompt, p_CliOut);
    printf("%s\r\n>", buff);

    return stat;    
}
