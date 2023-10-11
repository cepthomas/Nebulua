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

// For spec see process_interop.lua


namespace Ephemera.Nebulua
{
    public partial class Script
    {
        ///// Host calls lua
        // lua_func_name: my_lua_func
        // cs_func_name: HostCallLua
        // rettype: T
        // argtypes[]: SIT  req/opt?
        // gens:
        public TableEx? HostCallLua(string arg1, int arg2, TableEx arg3)
        {
            TableEx? ret;
            bool ok = true;

            // Get the function to be called. Check return.
            LuaType ltype = _l.GetGlobal(my_lua_func);
            if (ltype == LuaType.None)
            {
                ok = false;
                ErrorHandler(new SyntaxException("Bad lua function: my_lua_func"));
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
                    ok = false;
                    ErrorHandler(new SyntaxException("?????"));
                }

                // Get the results from the stack. maybe
                var tbl = _l.ToTableEx(-1); // or ToInteger() etc
                if (tbl is null)
                {
                    ok = false;
                    ErrorHandler(new SyntaxException("??????????"));
                }
                _l.Pop(num_ret); // Clean up results.
            }

            return ret;
        }

        ///// Lua calls Host functions
        // also gen registration func stuff?
        static int LuaCallHost(IntPtr p)
        {
            bool ok = true;
            int numres = 0;
            Lua? l = Lua.FromIntPtr(p);

            int arg1;
            string arg2;

            if (l is null)
            {
                ok = false;
                ErrorHandler(new LuaException("This should never happen"));
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
                    ErrorHandler(new SyntaxException($"Bad arg type: ..."));
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
                    ok = false;
                    ErrorHandler(new SyntaxException("Bad arg type: ..."));
                }
            }

            if (ok)
            {
                // Do the work.
                double ret = LuaCallHost_DoWork(arg1, arg2);

                // Return results.
                l.PushNumber(ret);
                numres = 1;
            }

            return numres;
        }
    }
}
