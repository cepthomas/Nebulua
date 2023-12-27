
#include <time.h>
#include <sys/time.h>
#include <string.h>
#include <conio.h>
#include <stdarg.h>
#include <stdbool.h>
#include <stdio.h>
#include "cli.h"
#include "common.h"

// TODO3 uses stdio, could add serial port, socket, etc.


//---------------- Private ------------------------------//

// CLI buffer to collect input chars.
static char p_cli_buff[CLI_BUFF_LEN];


//---------------- Public Implementation -----------------//

//--------------------------------------------------------//
int cli_Init(void)
{
    int stat = NEB_OK;

    memset(p_cli_buff, 0, CLI_BUFF_LEN);

    return stat;
}

//--------------------------------------------------------//
int cli_Destroy(void)
{
    int stat = NEB_OK;

    return stat;
}

//--------------------------------------------------------//
int cli_Open(int channel)
{
    (void)channel;

    int stat = NEB_OK;

    // Prompt.
    cli_WriteLine("\r\n>");

    return stat;
}

//--------------------------------------------------------//
bool cli_ReadLine(char* buff, int num)
{
    bool stat = false;

    // Default.
    buff[0] = 0;

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
                stat = true;

                // Clear buffer.
                memset(p_cli_buff, 0, CLI_BUFF_LEN);

                // Echo prompt.
                cli_WriteLine("");
                break;

            default:
                // Echo char.
                putchar(c);
                
                // Save it.
                p_cli_buff[strlen(p_cli_buff)] = c;
                break;
        }
    }

    return stat;
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

    // Add a prompt.
    printf("%s\r\n>", buff);

    return stat;    
}
