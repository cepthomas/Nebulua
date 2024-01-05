
#include <stdio.h>
#include <string.h>
#include <stdarg.h>
#include <time.h>
#include <sys/time.h>

#include "logger.h"


//--------------------------------------------------------//
// Logging support items.
static log_level_t _level = LVL_INFO;
static FILE* _fp = NULL;
static double _start_sec;

#define LOG_LINE_LEN 100

// Current time.
static double _CurrentSec();


//--------------------------------------------------------//
void logger_Init(FILE* fp)
{
    _fp = fp;
    _start_sec = _CurrentSec();

    // Banner.
    time_t now = time(NULL);
    char snow[32];
    strftime(snow, 32, "%Y-%m-%d %H:%M:%S", localtime(&now));
    fprintf(_fp, "================ Log start %s =====================\n", snow);
}

//--------------------------------------------------------//
void logger_SetFilters(log_level_t level)
{
    _level = level;
}

//--------------------------------------------------------//
void logger_Log(log_level_t level, const char* format, ...)
{
    static char buff[LOG_LINE_LEN];

    // Check filters.
    if(level >= _level)
    {
        va_list args;
        va_start(args, format);
        vsnprintf(buff, LOG_LINE_LEN-1, format, args);
        va_end(args);

        const char* slevel = "???";
        switch(level)
        {
            case LVL_DEBUG: slevel = "DBG"; break;
            case LVL_INFO:  slevel = "INF"; break;
            case LVL_ERROR: slevel = "ERR"; break;
        }

        fprintf(_fp, "%03.6f,%s,%s\n", _CurrentSec() - _start_sec, slevel, buff);
        // const char* pfn = strrchr(fn, '\\');
        // pfn = pfn == NULL ? fn : pfn + 1;
        // fprintf(_fp, "%03.6f,%s,%s(%d),%s\n", _CurrentSec() - _start_sec, slevel, pfn, line, buff);
        fflush(_fp);
    }
}


//--------------------------------------------------------//
double _CurrentSec()
{
    struct timeval tv;
    struct timezone tz;
    gettimeofday(&tv, &tz);
    double sec = (double)tv.tv_sec + (double)tv.tv_usec / 1000000.0;
    return sec;
}