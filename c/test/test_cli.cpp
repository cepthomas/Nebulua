#include <cstdio>
#include <cstring>
#include <unistd.h>

#include "pnut.h"

extern "C"
{
#include "common.h"
#include "logger.h"
}


/////////////////////////////////////////////////////////////////////////////
UT_SUITE(LOGGER_FILE, "Test logger to file.")
{
    // Turn on all.
    UT_EQUAL(logger_SetFilters(LVL_DEBUG, CAT_ALL), 0);

    // Start logging stuff.
    LOG_ERROR(CAT_INIT, "This should be an ERROR 99:%d", 99);

    sleep(1);
    LOG_INFO(CAT_DESTROY, "INFO One second later... :%d", 555);

    sleep(1);
    LOG_DEBUG(CAT_LOOK, "DEBUG Two seconds later... :%d", 71717);

    // Specific filters.
    UT_EQUAL(logger_SetFilters(LVL_INFO, CAT_LOOK), 0);

    LOG_INFO(CAT_DESTROY, "This should not appear");
    LOG_INFO(CAT_LOOK, "This should appear");
    LOG_ERROR(CAT_LOOK, "This should appear");

    return 0;
}    
