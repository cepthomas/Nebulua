using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks.PNUT;


namespace Nebulua.Test
{
    public class API_ONE : TestSuite
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

/////////////////////////////////////////////////////////////////////////////
UT_SUITE(EXEC_MAIN, "Test happy path. TODO2")
{
    int stat = 0;

    // Load/compile the script file. Pushes the compiled chunk as a Lua function on top of the stack or pushes an error message.
    stat = luaL_loadfile(_ltest, "script_happy.lua");
    UT_EQUAL(stat, NEB_OK);
    const char* e = nebcommon_EvalStatus(_ltest, stat, "load script");
    UT_NULL(e);

    // Execute the loaded script to init everything.
    stat = lua_pcall(_ltest, 0, LUA_MULTRET, 0);
    UT_EQUAL(stat, NEB_OK);
    e = nebcommon_EvalStatus(_ltest, stat, "run script");
    UT_NULL(e);

    // Script setup function.
    stat = luainterop_Setup(_ltest, &iret);
    UT_EQUAL(stat, NEB_OK);
    e = nebcommon_EvalStatus(_ltest, stat, "setup()");
    UT_NULL(e);

    ///// Good to go now.
    UT_STOP_ON_FAIL(true);

    // Get some nebulator script globals.
    scriptinfo_Init(_ltest);

    const char* sect_name = scriptinfo_GetSectionName(0);
    int sect_start = scriptinfo_GetSectionStart(0);
    UT_NOT_NULL(sect_name);
    UT_STR_EQUAL(sect_name, "beginning");
    UT_EQUAL(sect_start, 0);

    sect_name = scriptinfo_GetSectionName(1);
    sect_start = scriptinfo_GetSectionStart(1);

    UT_NOT_NULL(sect_name);
    UT_STR_EQUAL(sect_name, "middle");
    UT_EQUAL(sect_start, 646);

    sect_name = scriptinfo_GetSectionName(2);
    sect_start = scriptinfo_GetSectionStart(2);
    UT_NOT_NULL(sect_name);
    UT_STR_EQUAL(sect_name, "ending");
    UT_EQUAL(sect_start, 2118);

    sect_name = scriptinfo_GetSectionName(3);
    sect_start = scriptinfo_GetSectionStart(3);
    UT_NULL(sect_name);
    UT_EQUAL(sect_start, -1);

    lua_close(_ltest);

    return 0;
}


/////////////////////////////////////////////////////////////////////////////
UT_SUITE(EXEC_ERR1, "Test basic failure modes.")
{
    int stat = 0;
    int iret = 0;

    // Load lua.
    lua_State* _ltest = luaL_newstate();
    luaL_openlibs(_ltest);
    luainterop_Load(_ltest);
    lua_pop(_ltest, 1);

    ///// General syntax error during load.
    const char* s1 =
        "local neb = require(\"nebulua\")\n"
        "this is a bad statement\n";
    stat = luaL_loadstring(_ltest, s1);
    UT_EQUAL(stat, LUA_ERRSYNTAX);
    const char* e = nebcommon_EvalStatus(_ltest, stat, "ERR1");
    UT_STR_CONTAINS(e, "syntax error near 'is'");

    ///// General syntax error - lua_pcall(_ltest, 0, LUA_MULTRET, 0);
    s1 =
        "local neb = require(\"nebulua\")\n"
        "res1 = 345 + nil_value\n";
    stat = luaL_loadstring(_ltest, s1);
    UT_EQUAL(stat, LUA_OK);
    stat = lua_pcall(_ltest, 0, LUA_MULTRET, 0);
    UT_EQUAL(stat, LUA_ERRRUN); // runtime error
    e = nebcommon_EvalStatus(_ltest, stat, "ERR2");
    UT_STR_CONTAINS(e, "attempt to perform arithmetic on a nil value");


    ///// Missing required C2L api element - luainterop_Setup(_ltest, &iret);
    s1 =
        "local neb = require(\"nebulua\")\n"
        "resx = 345 + 456\n";
    stat = luaL_loadstring(_ltest, s1);
    UT_EQUAL(stat, LUA_OK);
    stat = lua_pcall(_ltest, 0, LUA_MULTRET, 0);
    UT_EQUAL(stat, LUA_OK);
    stat = luainterop_Setup(_ltest, &iret);
    UT_EQUAL(stat, INTEROP_BAD_FUNC_NAME);
    e = nebcommon_EvalStatus(_ltest, stat, "ERR3");
    UT_STR_CONTAINS(e, "INTEROP_BAD_FUNC_NAME");


    ///// Bad L2C api function
    s1 =
        "local neb = require(\"nebulua\")\n"
        "function setup()\n"
        "    neb.no_good(95)\n"
        "    return 0\n"
        "end\n";
    stat = luaL_loadstring(_ltest, s1);
    UT_EQUAL(stat, LUA_OK);
    stat = lua_pcall(_ltest, 0, LUA_MULTRET, 0);
    UT_EQUAL(stat, LUA_OK);
    stat = luainterop_Setup(_ltest, &iret);
    UT_EQUAL(stat, LUA_ERRRUN);
    e = nebcommon_EvalStatus(_ltest, stat, "ERR4");
    UT_STR_CONTAINS(e, "attempt to call a nil value (field 'no_good')");

    lua_close(_ltest);

    return 0;
}


/////////////////////////////////////////////////////////////////////////////
UT_SUITE(EXEC_ERR2, "Test error() failure modes.")
{
    int stat = 0;
    int iret = 0;

    // Load lua.
    lua_State* _ltest = luaL_newstate();
    luaL_openlibs(_ltest);
    luainterop_Load(_ltest);
    lua_pop(_ltest, 1);

    ///// General explicit error.
    const char* s1 =
        "local neb = require(\"nebulua\")\n"
        "function setup()\n"
        "    error(\"setup() raises error()\")\n"
        "    return 0\n"
        "end\n";
    stat = luaL_loadstring(_ltest, s1);
    UT_EQUAL(stat, LUA_OK);
    stat = lua_pcall(_ltest, 0, LUA_MULTRET, 0);
    UT_EQUAL(stat, LUA_OK);
    stat = luainterop_Setup(_ltest, &iret);
    UT_EQUAL(stat, LUA_ERRRUN);
    const char* e = nebcommon_EvalStatus(_ltest, stat, "ERR5");
    UT_STR_CONTAINS(e, "setup() raises error()");

    lua_close(_ltest);

    return 0;
}


/////////////////////////////////////////////////////////////////////////////
UT_SUITE(EXEC_ERR3, "Test fatal internal failure modes.")
{
    int stat = 0;
    int iret = 0;

    // Load lua.
    lua_State* _ltest = luaL_newstate();
    luaL_openlibs(_ltest);
    luainterop_Load(_ltest);
    lua_pop(_ltest, 1);

    ///// Runtime error.
    const char* s1 =
        "local neb = require(\"nebulua\")\n"
        "function setup()\n" 
        "    local bad = 123 + ng\n"
        "    return 0\n"
        "end\n";
     stat = luaL_loadstring(_ltest, s1);
    UT_EQUAL(stat, LUA_OK);
    stat = lua_pcall(_ltest, 0, LUA_MULTRET, 0);
    UT_EQUAL(stat, LUA_OK);
    stat = luainterop_Setup(_ltest, &iret);
    UT_EQUAL(stat, LUA_ERRRUN);
    const char* e = nebcommon_EvalStatus(_ltest, stat, "ERR6");
    UT_STR_CONTAINS(e, "attempt to perform arithmetic on a nil value (global 'ng')");

    lua_close(_ltest);

    return 0;
}
*/