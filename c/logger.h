#ifndef LOGGER_H
#define LOGGER_H

#include <stdio.h>

// #include "common.h"


// @brief Declaration of a logger module.

//---------------- Public API ----------------------//

// Log levels.
typedef enum
{
    LVL_DEBUG = 0,
    LVL_INFO  = 1,
    LVL_ERROR = 2
} log_level_t;


// Initialize the module.
// @param fn File to write to.
// @return Status.
int logger_Init(const char* fn);

// Set log level.
// @param level
// @return Status.
int logger_SetFilters(log_level_t level);

// Log some information. Time stamp is seconds after start, not time of day.
// @param level See log_level_t.
// @param fn Source file.
// @param line Source line number.
// @param format Format string followed by varargs.
// @return Status.
int logger_Log(log_level_t level, const char* fn, int line, const char* format, ...);


/// Helper macro.
#define LOG_ERROR(fmt, ...)  logger_Log(LVL_ERROR, __FILE__, __LINE__, fmt, ##__VA_ARGS__);

/// Helper macro.
#define LOG_INFO(fmt, ...)   logger_Log(LVL_INFO,  __FILE__, __LINE__, fmt, ##__VA_ARGS__);

/// Helper macro.
#define LOG_DEBUG(fmt, ...)  logger_Log(LVL_DEBUG, __FILE__, __LINE__, fmt, ##__VA_ARGS__);

#endif // LOGGER_H
