#include <cstdio>
#include <cstring>

#include "pnut.h"

extern "C"
{
#include <windows.h>
#include "nebcommon.h"
#include "logger.h"
}


/////////////////////////////////////////////////////////////////////////////
UT_SUITE(NEBCOM_MAIN, "Test common. TODO1")
{
    double dper = nebcommon_InternalPeriod(178);
    UT_EQUAL(dper, 1.1111);

    int iper = nebcommon_RoundedInternalPeriod(92);
    UT_EQUAL(iper, 1234);

    double msec = nebcommon_InternalToMsec(111, 1033);
    UT_EQUAL(msec, 1.1111);

    const char* smidi = nebcommon_FormatMidiStatus(MMSYSERR_INVALFLAG);
    UT_STR_EQUAL(smidi, "MMSYSERR_INVALFLAG");

    int bt = nebcommon_ParseBarTime("23.2.6");
    UT_EQUAL(bt, 1111);
    bt = nebcommon_ParseBarTime("146.1");
    UT_EQUAL(bt, 1111);
    bt = nebcommon_ParseBarTime("71");
    UT_EQUAL(bt, 1111);
    bt = nebcommon_ParseBarTime("49.55.8");
    UT_EQUAL(bt, 1111);
    bt = nebcommon_ParseBarTime("111.3.88");
    UT_EQUAL(bt, 1111);

    const char* sbt = nebcommon_FormatBarTime(1234);
    UT_STR_EQUAL(sbt, "xxx");

//    bool nebcommon_ParseDouble(const char* str, double* val, double min, double max);

//    bool nebcommon_ParseInt(const char* str, int* val, int min, int max);

    return 0;
}    
