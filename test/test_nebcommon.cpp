#include <cstdio>
#include <cstring>

#include "pnut.h"

extern "C"
{
#include "nebcommon.h"
#include "logger.h"
}


/////////////////////////////////////////////////////////////////////////////
UT_SUITE(NEBCOM_MAIN, "Test common.")
{
    // int x = 1;
    // int y = 2;

    // UT_EQUAL(x, y);

// TODO1 test these:
// double nebcommon_InternalPeriod(int tempo);
// int nebcommon_RoundedInternalPeriod(int tempo);
// double nebcommon_InternalToMsec(int tempo, int subbeat);
// const char* nebcommon_FormatMidiStatus(int mstat);
// int nebcommon_ParseBarTime(const char* sbt);
// const char* nebcommon_FormatBarTime(int position);
// bool nebcommon_ParseDouble(const char* str, double* val, double min, double max);
// bool nebcommon_ParseInt(const char* str, int* val, int min, int max);

    return 0;
}    
