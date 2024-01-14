#include <cstdio>
#include <cstring>
#include <unistd.h>

#include "pnut.h"

extern "C"
{
#include "nebcommon.h"
#include "logger.h"
}


/////////////////////////////////////////////////////////////////////////////
UT_SUITE(NCOM_SILLY, "Test stuff.")
{
    int x = 1;
    int y = 2;

    UT_EQUAL(x, y);

    LOG_INFO(CAT_INIT, "Hello!");

    return 0;
}    
