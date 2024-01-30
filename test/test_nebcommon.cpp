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
UT_SUITE(NEBCOM_MAIN, "Test nebulua common.")
{
    // double dper = nebcommon_InternalPeriod(178);
    // UT_EQUAL(dper, 1.1111);

    // int iper = nebcommon_RoundedInternalPeriod(92);
    // UT_EQUAL(iper, 1234);

    // double msec = nebcommon_InternalToMsec(111, 1033);
    // UT_EQUAL(msec, 1.1111);

    const char* smidi = nebcommon_FormatMidiStatus(MMSYSERR_INVALFLAG);
    UT_STR_EQUAL(smidi, "An invalid flag was passed to a system function.");

    smidi = nebcommon_FormatMidiStatus(90909);
    UT_STR_EQUAL(smidi, "MidiStatus:90909");

    int bt = nebcommon_ParseBarTime("23.2.6");
    UT_EQUAL(bt, 23 * SUBBEATS_PER_BAR + 2 * SUBBEATS_PER_BEAT + 6);
    bt = nebcommon_ParseBarTime("146.1");
    UT_EQUAL(bt, 146 * SUBBEATS_PER_BAR + 1 * SUBBEATS_PER_BEAT);
    bt = nebcommon_ParseBarTime("71");
    UT_EQUAL(bt, 71 * SUBBEATS_PER_BAR);
    bt = nebcommon_ParseBarTime("49.55.8");
    UT_EQUAL(bt, -1);
    bt = nebcommon_ParseBarTime("111.3.88");
    UT_EQUAL(bt, -1);
    bt = nebcommon_ParseBarTime("invalid");
    UT_EQUAL(bt, -1);
    const char* sbt = nebcommon_FormatBarTime(12345);
    UT_STR_EQUAL(sbt, "385.3.1");

    double dval;
    int ival;
    bool ok;

    ok = nebcommon_ParseDouble("1859.371", &dval, 300.1, 1900.9);
    UT_TRUE(ok);
    UT_CLOSE(dval, 1859.371, 0.0001);

    ok = nebcommon_ParseDouble("-204.91", &dval, -300.1, 300.9);
    UT_TRUE(ok);
    UT_CLOSE(dval, -204.91, 0.001);

    ok = nebcommon_ParseDouble("555.55", &dval, 300.1, 500.9);
    UT_FALSE(ok);

    ok = nebcommon_ParseDouble("invalid", &dval, 300.1, 500.9);
    UT_FALSE(ok);

    ok = nebcommon_ParseInt("1859", &ival, 300, 1900);
    UT_TRUE(ok);
    UT_EQUAL(ival, 1859);

    ok = nebcommon_ParseInt("-204", &ival, -300, 300);
    UT_TRUE(ok);
    UT_EQUAL(ival, -204);

    ok = nebcommon_ParseInt("555", &ival, 300, 500);
    UT_FALSE(ok);

    ok = nebcommon_ParseInt("invalid", &ival, 300, 500);
    UT_FALSE(ok);

    return 0;
}    
