﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Drawing;
using KeraLuaEx;
using Ephemera.Nebulua.Script;
using Ephemera.NBagOfTricks.PNUT;


namespace Ephemera.Nebulua.ScriptLua.Test
{
    public class XXX_ONE : TestSuite
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

            UT_INFO("Suite tests core functions.");

            UT_INFO("Test UT_INFO. Visually inspect that this appears in the output.");

            UT_INFO("Test UT_INFO with args", int1, dbl2);

            UT_INFO("Should fail on UT_STR_EQUAL.");
            UT_EQUAL(str1, str2);

            // Should pass on UT_STR_EQUAL.
            UT_EQUAL(str2, "the mulberry bush");

            UT_EQUAL("".Length, 0);

            UT_INFO("Should fail on UT_NOT_EQUAL.");
            UT_NOT_EQUAL(int1, 321);

            // Should pass on UT_NOT_EQUAL.
            UT_NOT_EQUAL(int2, int1);

            UT_INFO("Should fail on UT_LESS_OR_EQUAL.");
            UT_LESS_OR_EQUAL(int2, int1);

            // Should pass on UT_LESS_OR_EQUAL.
            UT_LESS_OR_EQUAL(int1, 321);

            // Should pass on UT_LESS_OR_EQUAL.
            UT_LESS_OR_EQUAL(int1, int2);

            UT_INFO("Should fail on UT_GREATER.");
            UT_GREATER(int1, int2);

            // Should pass on UT_GREATER.
            UT_GREATER(int2, int1);

            UT_INFO("Should fail on UT_GREATER_OR_EQUAL.");
            UT_GREATER_OR_EQUAL(int1, int2);

            // Should pass on UT_GREATER_OR_EQUAL.
            UT_GREATER_OR_EQUAL(int2, 987);

            // Should pass on UT_GREATER_OR_EQUAL.
            UT_GREATER_OR_EQUAL(int2, int1);

            // Should pass on UT_CLOSE.
            UT_CLOSE(dbl1, dbl2, dbl2 - dbl1 + dblTol);

            UT_INFO("Should fail on UT_CLOSE.");
            UT_CLOSE(dbl1, dbl1 - 2 * dblTol, dblTol);
        }
    }

    public class TestCases
    {
        public void Test1()
        {
            Lua _lMain = new Lua();
            //_lMain.Register("print", _func_print);


            //TestCommon.ExecuteLuaFile(_lMain, "interop");

            ScriptApi.Load(@"C:\Dev\repos\Nebulua\example_files\example.lua");
            //C:\Dev\repos\Nebulua\Test\scripts


            ScriptApi.Setup();
            ScriptApi.Step(4, 3, 11);
            ScriptApi.InputNote("doo", 5, 34, 100);
            ScriptApi.InputController("daa", 6, 33, 44);

            _lMain.Close();
            _lMain = null;

        }
    }
}
