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
    #region Exceptions

    // If new properties are added to the derived exception class, ToString() should be overridden to return the added information.
    
    /// <summary>Lua script syntax error.</summary>
    [Serializable]
    public class SyntaxException : Exception
    {
        public SyntaxException() : base() { }
        public SyntaxException(string message) : base(message) { }
        public SyntaxException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>Internal error on lua side.</summary>
    [Serializable]
    public class LuaException : Exception
    {
        public LuaException() : base() { }
        public LuaException(string message) : base(message) { }
        public LuaException(string message, Exception inner) : base(message, inner) { }
    }
    #endregion

    partial class Lua
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="paths"></param>
        public void SetLuaPath(List<string> paths)
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
            DoString(s);

            // Other way.
            //paths.ForEach(p => parts.Add(Path.Join(p, "?.lua")));
            //parts.AddRange(paths);
            //string luapath = string.Join(';', parts);
            //Environment.SetEnvironmentVariable("LUA_PATH", luapath);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public (object? val, Type? type) GetGlobalValue(string name) //TODOA used?
        {
            object? val = null;
            Type? type = null;

            LuaType t = GetGlobal(name); // st: global
            switch (t)
            {
                case LuaType.String:
                    val = ToString(-1);
                    type = typeof(string);
                    break;

                case LuaType.Boolean:
                    val = ToBoolean(-1);
                    type = typeof(bool);
                    break;

                case LuaType.Number:
                    if (IsInteger(-1))
                    {
                        val = (int)ToInteger(-1);
                        type = typeof(int);
                    }
                    else
                    {
                        val = ToNumber(-1);
                        type = typeof(double);
                    }
                    break;

                case LuaType.Nil:
                    val = null;
                    break;

                case LuaType.Table://TODOA


                    break;

                //case LuaType.Function:
                //case LuaType.Thread:
                //case LuaType.UserData:
                //case LuaType.LightUserData: ls.Add($"{t}:{l.ToPointer(i)}"); break;
                default:
                    throw new ArgumentException($"Unsupported type:{t} for {name}");
            }

            if (val == null)
            {
                Pop(1); // clean up stack ??
            }

            return (val, type);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<string> DumpGlobals()
        {
            // Get global table.
            PushGlobalTable();

            var ls = DumpTable();

            // Remove global table(-1).
            Pop(1);

            return ls;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public List<string> DumpTable(string tableName)
        {
            //lua_getglobal(lua_State * L, const char* name)

            var ttype = GetGlobal(tableName);

            if (ttype != LuaType.Table)
            {
                throw new ArgumentException($"Invalid table name:{tableName}");
            }

            var ls = DumpTable();

            return ls;
        }

        /// <summary>
        /// Dump the table on the top of stack.
        /// </summary>
        /// <returns></returns>
        List<string> DumpTable()
        {
            List<string> ls = new();

            // Put a nil key on stack.
            PushNil();

            // key(-1) is replaced by the next key(-1) in table(-2).
            while (Next(-2))// != 0)
            {
                // Get key(-2) name.
                string name = ToString(-2);
                // Get type of value(-1).
                string type = TypeName(-1);
                // Get value. TODOA process table
                string sval = ToString(-1);

                ls.Add($"{name}:{type}:{sval}");

                // Remove value(-1), now key on top at(-1).
                Pop(1);
            }

            return ls;
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
        public bool CheckLuaStatus(LuaStatus lstat, string info, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            bool hasError = false;

            if (lstat >= LuaStatus.ErrRun)
            {
                hasError = true;

                string stack = string.Join(" ", DumpStack());
                string tb = string.Join(" ", DumpTraceback());

                //TODOE exceptions or error code return?
                //_logger.Error($"Lua status:{lstat} in {file}({line}) {info}");

                if (lstat == LuaStatus.ErrFile)
                {
                    throw new FileNotFoundException(info);
                }
                else
                {
                    string serr = $"{lstat}: {stack}";
                    //string serr = $"{lstat} tb:{tb} stack:{stack}";
                    throw new LuaException(serr);
                }
            }

            return hasError;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<string> DumpTraceback()
        {
            Traceback(this, null, 1); // (state, string message, int level)

            return DumpStack();

            //List<string> parts = new()
            //{
            //    $"-1:{ToString(-1)}",
            //    $"-2:{ToString(-2)}",
            //    $"-3:{ToString(-3)}"
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
        public List<string> DumpStack()
        {
            List<string> ls = new();
            int num = GetTop();

            for (int i = num; i >= 1; i--)
            {
                LuaType t = Type(i);
                string sval;
                switch (t)
                {
                    case LuaType.String:    sval = $"\"{ToString(i)}\"";      break;
                    case LuaType.Boolean:   sval = ToBoolean(i) ? "true" : "false";    break;
                    case LuaType.Number:    sval = "{(IsInteger(i) ? ToInteger(i) : ToNumber(i))}";  break;
                    case LuaType.Nil:       sval = "nil";   break;
                    case LuaType.Table:     sval = ToString(i); break;
                    //case LuaType.Function:
                    //case LuaType.Table:
                    //case LuaType.Thread:
                    //case LuaType.UserData:
                    //case LuaType.LightUserData: ls.Add($"{t}:{l.ToPointer(i)}"); break;
                    default:                sval = $"{ToPointer(i)}"; break;
                }

                string s = $"ind:{i} type:{t} val:{sval}";
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
        /// Get the dir name of the caller's source file.
        /// </summary>
        /// <param name="callerPath"></param>
        /// <returns>Caller source dir.</returns>
        public string GetSourcePath([CallerFilePath] string callerPath = "") // from NBOT
        {
            var dir = Path.GetDirectoryName(callerPath)!;
            return dir;
        }
    }
}
