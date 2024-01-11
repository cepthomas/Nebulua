#include <cstdio>
#include <cstring>
#include <unistd.h>

#include "pnut.h"

extern "C"
{
#include "cli.h"
#include "logger.h"
}


/////////////////////////////////////////////////////////////////////////////
UT_SUITE(CLI_SILLY, "Test cli.")
{
    int x = 1;
    int y = 2;

    UT_EQUAL(x, y);

    LOG_INFO(CAT_INIT, "Hello!");

    return 0;
}    
