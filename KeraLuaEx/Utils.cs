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
        /// Generic get a simple stack value. Restores stack. TODO also for function args?
        /// </summary>
        /// <param name="l"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static (object? val, Type? type) GetGlobalValue(Lua l, string name)
        {
            object? val = null;
            Type? type = null;

            LuaType t = l.GetGlobal(name);
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

                case LuaType.Table:






                //case LuaType.Function:
                //case LuaType.Thread:
                //case LuaType.UserData:
                //case LuaType.LightUserData:

                default:
                    throw new ArgumentException($"Unsupported type {t} for {name}");
            }

            // Restore stack from get.
            l.Pop(1);

            return (val, type);
        }


    
//

        public static Table GetTable(Lua l)
        {
            Dictionary<string, object> values = new();

            List<string> ls = new();

            // Put a nil key on stack.
            l.PushNil();

            // key(-1) is replaced by the next key(-1) in table(-2).
            while (l.Next(-2))// != 0)
            {
                // Get key(-2) name.
                string name = l.ToString(-2)!;

                // Get type of value(-1).
                LuaType type = l.Type(-1)!;

                if (type == LuaType.Table)
                {
                    ls.Add($"{name}({type}):");
                    ls.AddRange(DumpTable(l)); // recursion!
                }
                else
                {
                    // Get value.
                    string sval = l.ToString(-1)!;
                    ls.Add($"{name}({type}):{sval}");
                }

                // Remove value(-1), now key on top at(-1).
                l.Pop(1);
            }

            return ls;
        }




        //////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Dump a table at global scope into a readable list.
        /// </summary>
        /// <param name="l"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static List<string> DumpGlobalTable(Lua l, string tableName)
        {
            var ttype = l.GetGlobal(tableName);

            if (ttype != LuaType.Table)
            {
                throw new ArgumentException($"Invalid table name:{tableName}");
            }

            var ls = DumpTable(l);

            // Remove pushed table.
            l.Pop(1);

            return ls;
        }

        /// <summary>
        /// Dump the table on the top of stack into a readable list.
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
                LuaType type = l.Type(-1)!;

                if (type == LuaType.Table)
                {
                    ls.Add($"{name}({type}):");
                    ls.AddRange(DumpTable(l)); // recursion!
                }
                else
                {
                    // Get value.
                    string sval = l.ToString(-1)!;
                    ls.Add($"{name}({type}):{sval}");
                }

                // Remove value(-1), now key on top at(-1).
                l.Pop(1);
            }

            return ls;
        }

        /// <summary>
        /// Dump the traceback as list.
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
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static List<string> DumpStack(Lua l)
        {
            List<string> ls = new();
            int num = l.GetTop();

            if (num > 0)
            {
                for (int i = num; i >= 1; i--)
                {
                    LuaType t = l.Type(i);
                    string sval = t switch
                    {
                        LuaType.String => $"({t}):{l.ToString(i)}",
                        LuaType.Boolean => $"({t}):{l.ToBoolean(i)}",
                        LuaType.Number => $"({t}):{(l.IsInteger(i) ? l.ToInteger(i) : l.ToNumber(i))}",
                        LuaType.Nil => $"({t})",
                        LuaType.Table => $"({t}){l.ToString(i) ?? "null"}",
                        //case LuaType.Function:
                        //case LuaType.Table:
                        //case LuaType.Thread:
                        //case LuaType.UserData:
                        //case LuaType.LightUserData:
                        _ => $"({t}):{l.ToPointer(i)}",
                    };
                    string s = $"[{i}] type:{t} val:{sval}";
                    ls.Add(s);
                }
            }
            else
            {
                ls.Add("Empty");
            }

            return ls;
        }

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
                    case string x:   l.PushString(x);  break;
                    case bool x:     l.PushBoolean(x);   break;
                    case int x:    l.PushInteger(x);    break;
                    case double x:   l.PushNumber(x);   break;
                    case float x:   l.PushNumber(x);   break;

                    //case List<int> x:
                    //case List<double> d:
                    //case List<string> s:
                    //case List<Table> b:
                    //    // convert to table and push.
                    //    break;

                    default: throw new ArgumentException(string.Join("|", Utils.DumpStack(l)));// also "invalid func" or such
                }
            }

            // Do the actual call.
            LuaStatus lstat = l.PCall(numArgs, retType is null ? 0 : 1, 0);

            l.CheckLuaStatus(lstat);

            // Get the results from the stack. Make generic???
            //object val = retType switch
            //{
            //    null => null,
            //    int => l.ToInteger(-1),
            //    double => l.IsNumber(-1),
            //    string => l.ToString(-1),
            //    bool => l.ToBoolean(-1),
            //    // TODO1 table
            //    // ?? LuaType.Function:
            //    _ => throw new SyntaxException($"Invalid type:{retType}")
            //};

            // Restore stack from get.
            l.Pop(1);

            return ret;
        }
    }
}
