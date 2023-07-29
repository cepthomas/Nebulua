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
using System.Text.Json.Serialization;
using System.Text.Json;


namespace KeraLuaEx
{
    public class Utils
    {
        /// <summary>
        /// Generic get a value. Restores stack.
        /// </summary>
        /// <param name="l"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static (object? val, Type? type) GetGlobalValue(Lua l, string name)
        {
            object? val = null;
            Type? type = null;

            LuaType t = l.GetGlobal(name); // st: global
            switch (t)
            {
                case LuaType.Nil:
                    // Return defaults.
                    break;

                case LuaType.String:
                    val = l.ToString(-1);
                    type = typeof(string);
                    break;

                case LuaType.Boolean:
                    val = l.ToBoolean(-1);
                    type = typeof(bool);
                    break;

                case LuaType.Number:
                    if (l.IsInteger(-1))
                    {
                        val = (int)l.ToInteger(-1)!;
                        type = typeof(int);
                    }
                    else
                    {
                        val = l.ToNumber(-1)!;
                        type = typeof(double);
                    }
                    break;

                case LuaType.Table://TODO1

                    break;

                //case LuaType.Function:
                //case LuaType.Thread:
                //case LuaType.UserData:
                //case LuaType.LightUserData: ls.Add($"{t}:{l.l.ToPointer(i)}"); break;
                default:
                    throw new ArgumentException($"Unsupported type:{t} for {name}");
            }

            if (val != null)
            {
                l.Pop(1);
            }

            return (val, type);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static List<string> DumpGlobals(Lua l)
        {
            // Get global table.
            l.PushGlobalTable();

            var ls = DumpTable(l);

            // Remove global table(-1).
            l.Pop(1);

            return ls;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="l"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static List<string> DumpTable(Lua l, string tableName)
        {
            //lua_getglobal(lua_State * L, const char* name)

            var ttype = l.GetGlobal(tableName);

            if (ttype != LuaType.Table)
            {
                throw new ArgumentException($"Invalid table name:{tableName}");
            }

            var ls = DumpTable(l);

            return ls;
        }

        /// <summary>
        /// Dump the table on the top of stack.
        /// </summary>
        /// <returns></returns>
        public static List<string> DumpTable(Lua l)
        {
            List<string> ls = new();

            // Put a nil key on stack.
            l.PushNil();

            // key(-1) is replaced by the next key(-1) in table(-2).
            while (l.Next(-2))// != 0)
            {
                // Get key(-2) name.
                string name = l.ToString(-2)!;
                // Get type of value(-1).
                string type = l.TypeName(-1)!;
                // Get value. TODO1 process table
                string sval = l.ToString(-1)!;

                ls.Add($"{name}:{type}:{sval}");

                // Remove value(-1), now key on top at(-1).
                l.Pop(1);
            }

            return ls;
        }

        /// <summary>
        /// Push a list of ints onto the stack as function return.
        /// </summary>
        /// <param name="l"></param>
        /// <param name="ints"></param>
        public static void PushList(Lua l, List<int> ints)
        {
            //https://stackoverflow.com/a/18487635

            l.NewTable();

            for (int i = 0; i < ints.Count; i++)
            {
                l.NewTable();
                l.PushInteger(i + 1);
                l.RawSetInteger(-2, 1);
                l.PushInteger(ints[i]);
                l.RawSetInteger(-2, 2);
                l.RawSetInteger(-2, i + 1);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static List<string> DumpTraceback(Lua l)
        {
            l.Traceback(l, null, 1); // (state, string message, int level)

            return DumpStack(l);

            //List<string> parts = new()
            //{
            //    $"-1:{l.ToString(-1)}",
            //    $"-2:{l.ToString(-2)}",
            //    $"-3:{l.ToString(-3)}"
            //};
            //return string.Join(Environment.NewLine, parts);

            // Consequently, if we want a traceback, we must build it before pcall returns. To do that, Lua provides the xpcall function.
            // Besides the function to be called, it receives a second argument, an error handler function. In case of errors, Lua calls that
            // error handler before the stack unwinds, so that it can use the debug library to gather any extra information it wants about the error.
            // Two common error handlers are debug.debug, which gives you a Lua prompt so that you can inspect by yourself what was going on when
            // the error happened (later we will see more about that, when we discuss the debug library); and debug.traceback, which builds an
            // extended error message with a traceback.
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static List<string> DumpStack(Lua l)
        {
            List<string> ls = new();
            int num = l.GetTop();

            for (int i = num; i >= 1; i--)
            {
                LuaType t = l.Type(i);
                string sval = t switch
                {
                    LuaType.String => $"\"{l.ToString(i)}\"",
                    LuaType.Boolean => l.ToBoolean(i) ? "true" : "false",
                    LuaType.Number => $"{(l.IsInteger(i) ? l.ToInteger(i) : l.ToNumber(i))}",
                    LuaType.Nil => "nil",
                    LuaType.Table => l.ToString(i) ?? "null",
                    //case LuaType.Function:
                    //case LuaType.Table:
                    //case LuaType.Thread:
                    //case LuaType.UserData:
                    //case LuaType.LightUserData: ls.Add($"{t}:{l.l.ToPointer(i)}"); break;
                    _ => $"{l.ToPointer(i)}",
                };
                string s = $"[{i}] type:{t} val:{sval}";
                ls.Add(s);
            }

            return ls;
        }
        //int l_my_print(lua_State* L)
        //{
        //    int nargs = lua_gettop(L);
        //    for (int i = 1; i <= nargs; ++i)
        //    {
        //        if (lua_isnil(L, i))
        //            poststring("nil");
        //        else if (lua_isboolean(L, i))
        //            lua_toboolean(L, i) ? poststring("true") : poststring("false");
        //        else if (lua_isnumber(L, i))
        //            postfloat(static_cast<t_float>(lua_tonumber(L, i)));
        //        else if (lua_isstring(L, i))
        //            poststring(lua_tostring(L, i));
        //        else if (lua_istable(L, i))
        //            poststring("table: "); //how to print like Lua's built-in print()?
        //    }
        //    endpost();
        //    return 0;
        //}





        /// <summary>
        /// 
        /// </summary>
        /// <param name="l"></param>
        /// <param name="paths"></param>
        public static void SetLuaPath(Lua l, List<string> paths)
        {
            List<string> parts = new()
            {
                "?",
                "?.lua"
            };

            // One way.
            paths.ForEach(p => parts.Add(Path.Join(p, "?.lua").Replace('\\', '/')));
            string luapath = string.Join(';', parts);
            string s = $"package.path = \"{luapath}\"";
            l.DoString(s);

            // Other way.
            //paths.ForEach(p => parts.Add(Path.Join(p, "?.lua")));
            //parts.AddRange(paths);
            //string luapath = string.Join(';', parts);
            //Environment.SetEnvironmentVariable("LUA_PATH", luapath);
        }

        /// <summary>
        /// Get the dir name of the caller's source file.
        /// </summary>
        /// <param name="callerPath"></param>
        /// <returns>Caller source dir.</returns>
        public static string GetSourcePath([CallerFilePath] string callerPath = "") // from NBOT
        {
            var dir = Path.GetDirectoryName(callerPath)!;
            return dir;
        }


        /////////////////////////////////// TODO2 future? ///////////////////////////////////
        // Lua calls C# functions
        public object? LuaCallCsharp(IntPtr p, int numResults, params Type[] argTypes)
        {
            //object? ret = null;

            //var l = Lua.FromIntPtr(p);
            //int numArgs = l.GetTop();

            //if (argTypes.Length != numArgs)
            //{
            //    throw new SyntaxException(string.Join("|",  DumpStack())); // also "invalid func" or such
            //}

            // var noteString = l.l.ToString(1);
            // // Do the work.
            // List<int> notes = MusicDefinitions.GetNotesFromString(noteString);
            // l.PushList(notes);

            return numResults;
        }

        /// <summary>
        /// C# calls lua functions
        /// </summary>
        /// <param name="func"></param>
        /// <param name="retType"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object? CsharpCallLua(string func, Type retType, params object[] args)
        {
            object? ret = null;
            Lua l = new();

            // Get the function to be called. Check return.
            LuaType gtype = l.GetGlobal(func);
            if (gtype != LuaType.Function) // optional?
            {
                throw new SyntaxException(string.Join("|", Utils.DumpStack(l))); // also "invalid func" or such
            }

            // Push the arguments to the call.
            int numArgs = args.Length;
            for (int i = 0; i < numArgs; i++)
            {
                switch (args[i])
                {
                    case string x:
                        l.PushString(x);
                        break;

                    case bool x:
                        l.PushBoolean(x);
                        break;

                    case int x:
                        l.PushInteger(x);
                        break;

                    case double x:
                        l.PushNumber(x);
                        break;

                    case float x:
                        l.PushNumber(x);
                        break;

                    //case List<int> x:
                    //case List<double> d:
                    //case List<string> s:
                    //case List<Bag> b:
                    //    // convert to table and push.
                    //    break;

                    default:
                        throw new ArgumentException(string.Join("|", Utils.DumpStack(l)));// also "invalid func" or such
                }
            }

            // Do the actual call.
            LuaStatus lstat = l.PCall(numArgs, retType is null ? 0 : 1, 0);

            l.CheckLuaStatus(lstat);

            // Get the results from the stack.

            // l.IsBoolean(int index) => Type(index) == LuaType.Boolean;
            // l.IsInteger(int index) => NativeMethods.lua_isinteger(_luaState, index) != 0;
            // IsNil(int index) => Type(index) == LuaType.Nil;
            // IsNumber(int index) => NativeMethods.lua_isnumber(_luaState, index) != 0;
            // IsString(int index) => Type(index) == LuaType.String;
            // IsTable(int index) => Type(index) == LuaType.Table;


            // Get the results from the stack. Make generic?
            if (retType is null)
            {
                // Do nothing.
            }
            else if (l.IsBoolean(-1))
            {
                ret = l.ToBoolean(-1);
            }
            else if (l.IsInteger(-1))
            {
                ret = (int)l.ToInteger(-1)!;
            }
            else if (l.IsNil(-1))
            {
                ret = null;
            }
            else if (l.IsNumber(-1))
            {
                ret = l.ToNumber(-1);
            }
            else if (l.IsString(-1))
            {
                ret = l.ToString(-1);
            }
            else if (l.IsTable(-1))
            {
                ret = null; //turn into ?
            }
            else
            {
                throw new SyntaxException("info");
            }

            l.Pop(1);

            return ret;
        }
    }
}
