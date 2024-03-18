using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks.PNUT;


namespace Nebulua.Test //TestZ
{
    public class NEBCOMMON_ONE : TestSuite
    {
        public override void RunSuite()
        {
            int int1 = 321;
            int int2 = 987;
            string str1 = "round and round";
            string str2 = "the mulberry bush";
            double dbl1 = 1.500;   
            double dbl2 = 1.600;
            double dblTol = 0.001;

            UT_INFO("Test UT_INFO with args", int1, dbl2);

            UT_EQUAL(str1, str2);

            UT_EQUAL(str2, "the mulberry bush");
        }
    }
}



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

    int bt = nebcommon_ParseBarTime("23:2:6");
    UT_EQUAL(bt, 23 * SUBS_PER_BAR + 2 * SUBS_PER_BEAT + 6);
    bt = nebcommon_ParseBarTime("146:1");
    UT_EQUAL(bt, 146 * SUBS_PER_BAR + 1 * SUBS_PER_BEAT);
    bt = nebcommon_ParseBarTime("71");
    UT_EQUAL(bt, 71 * SUBS_PER_BAR);
    bt = nebcommon_ParseBarTime("49:55:8");
    UT_EQUAL(bt, -1);
    bt = nebcommon_ParseBarTime("111:3:88");
    UT_EQUAL(bt, -1);
    bt = nebcommon_ParseBarTime("invalid");
    UT_EQUAL(bt, -1);
    const char* sbt = nebcommon_FormatBarTime(12345);
    UT_STR_EQUAL(sbt, "385:3:1");

    return 0;
}    
