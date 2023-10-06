using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NAudio.Midi;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfTricks;
using Ephemera.MidiLib;
using KeraLuaEx;


namespace Ephemera.Nebulua
{
    public class Gen
    {
        ///// gen funcs from sig TODO1

        // General
        void ErrorHandler(string s)
        {
            // Do something with this.
        }

        ///// C# calls lua
        // lua_func_name: my_lua_func
        // cs_func_name: CsharpCallLua
        // rettype: T
        // argtypes[]: SIT  TODO2 req/opt?
        // gens:
        public TableEx? CsharpCallLua(string arg1, int arg2, TableEx arg3)
        {
            TableEx? ret;
            bool ok = true;

            // Get the function to be called. Check return.
            LuaType ltype = _l.GetGlobal(my_lua_func);
            if (ltype == LuaType.None)
            {
                // ERR Bad lua function name: my_lua_func
                if (_l.ThrowOnError)
                {
                    throw (new SyntaxException("Bad lua function: my_lua_func"));
                }
                ok = false;
            }

            if (ok)
            {
                // Push the arguments to the call.
                _l.PushType1(arg1);
                _l.PushType2(arg2);
                _l.PushType3(arg3);

                // Do the actual call.
                LuaStatus lstat = _l.DoCall(num_args, num_ret); // optionally throws
                if (lstat >= LuaStatus.ErrRun)
                {
                    // ERR Bad lua function: my_lua_func
                    ok = false;
                }

                // Get the results from the stack. maybe
                var tbl = _l.ToTableEx(-1); // or ToInteger() etc
                if (tbl is null)
                {

                }
                _l.Pop(num_ret); // Clean up results.
            }

            return ret;
        }




        ///// Lua calls C# functions


// lua_func_name: my_lua_func
        // cs_func_name: LuaCallCsharp
        // rettype: I
        // argtypes[]: IS  TODO2 req/opt?
        // work lambda?
        // gens:
        static int LuaCallCsharp(IntPtr p)
        {
            bool ok = true;
            int numres = 0;
            Lua? l = Lua.FromIntPtr(p);

            int arg1;
            string arg2;

            if (l is null)
            {
                throw (new LuaException("This should never happen"));
                ok = false;
            }

            // Get args.
            if (ok)
            {
                if (l.IsInteger(1))
                {
                    arg1 = l.ToInteger(1);
                }
                else
                {
                    ok = false;
                    if (l.ThrowOnError)
                    {
                        throw (new SyntaxException($"Bad arg type: ..."));
                    }
                }
            }

            if (ok)
            {
                if (l.IsString(2))
                {
                    arg1 = l.ToStringL(2);
                }
                else
                {
                    throw (new SyntaxException("Bad arg type: ..."));
                    ok = false;
                }
            }

            if (ok)
            {
                // Do the work.
                double ret = FuncName_DoWork(arg1, arg2);

                // Return results.
                l.PushNumber(ret);
                numres = 1;
            }

            return numres;
        }

        // client supplies this work function - or lambda?
        static double FuncName_DoWork(int level, string msg)
        {
            double ret = level * msg.Length;
            return ret;
        }
    }
}
