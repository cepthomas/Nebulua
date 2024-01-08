#ifndef LOGGER_H
#define LOGGER_H

#include <stdio.h>

// TODO2 harmonize with cbot version.

//---------------- Public API ----------------------//

/// Log levels.
typedef enum
{
    LVL_DEBUG = 1,
    LVL_INFO  = 2,
    LVL_ERROR = 3
} log_level_t;


/// Initialize the module.
/// @param fp Stream to write to.
void logger_Init(FILE* fp);

/// Set log level.
/// @param level
void logger_SetFilters(log_level_t level);

/// Log some information. Time stamp is seconds after start, not time of day.
/// @param level See log_level_t.
/// @param format Format string followed by varargs.
void logger_Log(log_level_t level, const char* format, ...);

#endif // LOGGER_H
