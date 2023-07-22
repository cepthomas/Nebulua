using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
//using NAudio.Midi;


namespace KeraLuaEx
{
    partial class Lua
    {
        //public string GetDebugTraceback()
        //{
        //    int oldTop = _luaState.GetTop();
        //    _luaState.GetGlobal("debug"); // stack: debug
        //    _luaState.GetField(-1, "traceback"); // stack: debug,traceback
        //    _luaState.Remove(-2); // stack: traceback
        //    _luaState.PCall(0, -1, 0);
        //    return _translator.PopValues(_luaState, oldTop)[0] as string;
        //}

        //        char* luacode =
        //"function f ( )                 \n"
        //"    print(\"hello\\n\")        \n"
        //"    return {  a=1.0, b=3.0 }   \n"
        //"end                            \n"
        //;

        //void CheckLuaErr(lua_State* L, int err, int line)
        //{
        //    if (err != 0)
        //    {
        //        printf("line %d: lua error %d\n", line, err);
        //        printf("%s\n", lua_tostring(L, -1));
        //        lua_pop(L, 1);  // pop error message from the stack
        //    }
        //}

        //    lua_State* L = luaL_newstate();

        //    luaL_openlibs(L);
        //    int err = luaL_loadbuffer(L, luacode, strlen(luacode), "testscript");
        //    CheckLuaErr(L, err, __LINE__);
        //    // Have to run the script once so the global variables (including the function names) are loaded
        //    // ref: http://www.troubleshooters.com/codecorn/lua/lua_c_calls_lua.htm
        //    err = lua_pcall(L, 0, 0, 0);
        //    CheckLuaErr(L, err, __LINE__);

        //    lua_getglobal(L, "f");  // Call the function f(), expecting 1 table returned
        //    err = lua_pcall(L, 0, 1, 0);
        //    CheckLuaErr(L, err, __LINE__);
        //    assert(lua_istable(L, -1));
        //    // -1 is now a reference to the table

        //    // Use gettable
        //    lua_pushstring(L, "a");
        //    lua_gettable(L, -2);
        //    float a = lua_tonumber(L, -1);
        //    lua_pop(L, 1);

        //    // Or getfield
        //    lua_getfield(L, -1, "b");
        //    float b = lua_tonumber(L, -1);

        //    lua_pop(L, 1);  // pop table from the stack


            // static int returnImageProxy(lua_State *L)
            // {
            //     Point points[3] = {{11, 12}, {21, 22}, {31, 32}};
            //     lua_newtable(L);
            //     for (int i = 0; i < 3; i++) {
            //         lua_newtable(L);
            //         lua_pushnumber(L, points[i].x);
            //         lua_rawseti(L, -2, 1);
            //         lua_pushnumber(L, points[i].y);
            //         lua_rawseti(L, -2, 2);
            //         lua_rawseti(L, -2, i+1);
            //     }
            //     return 1;  // I want to return a Lua table like :{{11, 12}, {21, 22}, {31, 32}}
            // }


        // void interop_Structinator(lua_State* L, my_data_t* din, my_data_t* dout)
        // {
        //     int lstat = 0;

        //     ///// Get the function to be called.
        //     int gtype = lua_getglobal(L, "structinator");

        //     ///// Package the input.
        //     // Create a new empty table and push it onto the stack.
        //     lua_newtable(L);

        //     lua_pushstring(L, "val");
        //     lua_pushinteger(L, din->val);
        //     lua_settable(L, -3);

        //     lua_pushstring(L, "state");
        //     lua_pushinteger(L, din->state);
        //     lua_settable(L, -3);

        //     lua_pushstring(L, "text");
        //     lua_pushstring(L, din->text);
        //     lua_settable(L, -3);

        //     ///// Use lua_pcall to do the actual call.
        //     lstat = lua_pcall(L, 1, 1, 0);

        //     PROCESS_LUA_ERROR(L, lstat, "lua_pcall structinator() failed");

        //     ///// Get the results from the stack.
        //     if(lua_istable(L, -1) > 0)
        //     {
        //         gtype = lua_getfield(L, -1, "val");
        //         lstat = luautils_GetArgInt(L, -1, &dout->val);
        //         lua_pop(L, 1); // remove field

        //         gtype = lua_getfield(L, -1, "state"); // LUA_TNUMBER
        //         lstat = luautils_GetArgInt(L, -1, (int*)&dout->state);
        //         lua_pop(L, 1); // remove field

        //         gtype = lua_getfield(L, -1, "text");
        //         lstat = luautils_GetArgStr(L, -1, &dout->text);
        //         lua_pop(L, 1); // remove field
        //     }
        //     else
        //     {
        //         int index = -1;
        //         PROCESS_LUA_ERROR(L, LUA_ERRRUN, "Invalid table argument at index %d", index);
        //     }

        //     // Remove the table.
        //     lua_pop(L, 1);
        // }
        //
        // -- Just a test for struct IO.
        // function structinator(data)
        //   state_name = state_type[data.state]
        //   slog = string.format ("demoapp: structinator got val:%d state:%s text:%s", data.val, state_name, data.text)
        //   tell(slog)

        //   -- Package return data.
        //   data.val = data.val + 1
        //   data.state = 3
        //   data.text = "Back atcha"

        //   return data
        // end



            //// Now load the script/file we are going to run.
            //// lua_load() pushes the compiled chunk as a Lua function on top of the stack.
            //lua_stat = luaL_loadfile(p_lscript, fn);
            //// Give it some data. 
            //lua_pushstring(p_lscript, "Hey diddle diddle");
            //lua_setglobal(p_lscript, "script_string");
            //lua_pushinteger(p_lscript, 90309);
            //lua_setglobal(p_lscript, "script_int");
            //// Priming run of the loaded Lua script to create the script's global variables
            //lua_stat = lua_pcall(p_lscript, 0, 0, 0);
            //if (lua_stat != LUA_OK)
            //{
            //    LOG_ERROR("lua_pcall() error code %i: %s", lua_stat, lua_tostring(p_lscript, -1));
            //}



        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public (object?, Type?) GetGlobalValue(string name)
        {
            object? val = null;
            Type? tret = null;

            LuaType t = GetGlobal(name); // st: global
            switch (t)
            {
                case LuaType.String:
                    val = ToString(-1);
                    tret = typeof(string);
                    break;

                case LuaType.Boolean:
                    val = ToBoolean(-1);
                    tret = typeof(bool);
                    break;

                case LuaType.Number:
                    if (IsInteger(-1))
                    {
                        val = (int)ToInteger(-1);
                        tret = typeof(int);
                    }
                    else
                    {
                        val = ToNumber(-1);
                        tret = typeof(double);
                    }
                    break;

                case LuaType.Nil:
                    val = null;
                    break;

                case LuaType.Table://TODOA
                    break;

                case LuaType.Function://TODOA
                    break;

                //case LuaType.Thread:
                //case LuaType.UserData:
                //case LuaType.LightUserData: ls.Add($"{t}:{l.ToPointer(i)}"); break;
                //default: ls.Add($"{t}:{l.ToPointer(i)}"); break;
            }

            if (val == null)
            {
                Pop(1); // clean up stack
            }

            return (val, tret);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string DumpGlobals()
        {
            // Get global table.
            PushGlobalTable();

            string? s = DumpTable("GLOBALS");

            // Remove global table(-1).
            Pop(1);

            return s;
        }

        /// <summary>
        /// Push a list of ints onto the stack as function return.
        /// </summary>
        /// <param name="ints"></param>
        public void PushList(List<int> ints)
        {
            //https://stackoverflow.com/a/18487635

            NewTable();

            for (int i = 0; i < ints.Count(); i++)
            {
                NewTable();
                PushInteger(i + 1);
                RawSetInteger(-2, 1);
                PushInteger(ints[i]);
                RawSetInteger(-2, 2);
                RawSetInteger(-2, i + 1);
            }
        }

        /// <summary>
        /// Check lua status.
        /// </summary>
        /// <param name="lstat"></param>
        /// <param name="info"></param>
        /// <param name="file">Ignore - compiler use.</param>
        /// <param name="line">Ignore - compiler use.</param>
        public void CheckLuaStatus(LuaStatus lstat, string info = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            if (lstat >= LuaStatus.ErrRun)
            {
                //TODOE !!!!!!!! general logging and error processing _logger.Error($"Lua status:{lstat} in {file}({line}) {info}");

                // Dump trace. TODOE
                // luaL_traceback(L, L, NULL, 1);
                // snprintf(buff, BUFF_LEN-1, "%s | %s | %s", lua_tostring(L, -1), lua_tostring(L, -2), lua_tostring(L, -3));
                // logger_Log(LVL_DEBUG, fn, line, "   %s", buff);


                //Error(); // never returns
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string DumpStack()
        {
            List<string> ls = new();
            int num = GetTop();

            for (int i = num; i >= 1; i--)
            {
                LuaType t = Type(i);

                switch(t)
                {
                    case LuaType.String:    ls.Add($"\"{ToString(i)}\"");      break;
                    case LuaType.Boolean:   ls.Add(ToBoolean(i) ? "true" : "false");    break;
                    case LuaType.Number:    ls.Add($"{(IsInteger(i) ? ToInteger(i) : ToNumber(i))}");  break;
                    case LuaType.Nil:       ls.Add("nil");   break;
                    //case LuaType.None:      ls.Add("none");  break;
                    //case LuaType.Function:
                    //case LuaType.Table:
                    //case LuaType.Thread:
                    //case LuaType.UserData:
                    //case LuaType.LightUserData: ls.Add($"{t}:{l.ToPointer(i)}"); break;
                    default:                ls.Add($"{t}:{ToPointer(i)}"); break;
                }
            }

            return string.Join("|", ls);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public string DumpTable(string tableName)
        {
            List<string> ls = new() { tableName };

            // Put a nil key on stack.
            PushNil();

            // key(-1) is replaced by the next key(-1) in table(-2).
            while (Next(-2))// != 0)
            {
                // Get key(-2) name.
                string name = ToString(-2);

                // Get type of value(-1).
                string type = TypeName(-1);

                ls.Add($"{name}:{type}");

                // Remove value(-1), now key on top at(-1).
                Pop(1);
            }

            return string.Join(" ", ls);
        }
    }
}
