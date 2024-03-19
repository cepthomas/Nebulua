using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks.PNUT;


namespace Nebulua.Test
{
    public class APP_ONE : TestSuite
    {
        public override void RunSuite()
        {
            int int1 = 321;
            //string str1 = "round and round";
            string str2 = "the mulberry bush";
            double dbl2 = 1.600;

            UT_INFO("Test UT_INFO with args", int1, dbl2);
            UT_EQUAL(str2, "the mulberry bush");
        }
    }
}

/*

void _MidiInHandler(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2);
void _MidiClockHandler(double msec);
int exec_Main(const char* script_fn);

// const char* _my_midi_in1  = "loopMIDI Port";
// const char* _my_midi_out1 = "Microsoft GS Wavetable Synth";


/////////////////////////////////////////////////////////////////////////////
UT_SUITE(EXEC_FUNCS, "Test exec functions. TODO2 Doesn't do anything yet")
{
    int stat = 0;

    //////
    HMIDIIN hMidiIn = 0;
    UINT wMsg = 0;
    DWORD_PTR dwInstance = 0;
    DWORD_PTR dwParam1 = 0;
    DWORD_PTR dwParam2 = 0;
    _MidiInHandler(hMidiIn, wMsg, dwInstance, dwParam1, dwParam2);

    //////
    double msec = 12.34;
    _MidiClockHandler(msec);

    //////
    // stat = exec_Main(script_fn);
    // Needs luapath.

    lua_close(_l);

    return 0;
}
*/