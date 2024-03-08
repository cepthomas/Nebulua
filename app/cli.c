// system
#include <windows.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
// lua
// cbot
#include "logger.h"
// application
#include "nebcommon.h"
#include "cli.h"


//--------------------------------------------------------//
int cli_open()
{
    // Nothing to do.
    return 0;
}


//--------------------------------------------------------//
int cli_close()
{
    // Nothing to do.
    return 0;
}


//--------------------------------------------------------//
int cli_printf(const char* format, ...)
{
    // Format string.
    char s[MAX_LINE_LEN];
    va_list args;
    va_start(args, format);
    vsnprintf(s, MAX_LINE_LEN - 1, format, args);
    va_end(args);

    return fputs(s, stdout);
}


//--------------------------------------------------------//
char* cli_gets(char* buff, int len)
{
    char* s = fgets(buff, len, stdin);
    buff[strcspn(buff, "\n")] = 0;
    return s;
}
