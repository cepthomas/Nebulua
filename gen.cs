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

        ///// C# calls lua
        // lua_func_name
        // cs_func_name
        // rettype 
        // argtypes[]  req/opt?
        // like:
        //   func_name
        //   FuncName
        //   T
        //   SIT 
        // gens:
        public TableEx FuncName(string arg1, int arg2, TableEx arg3)
        {
            // Get the function to be called. Check return.
            LuaType lstat = _l.GetGlobal(func_name);
            EvalLuaStatus(lstat);

            // Push the arguments to the call.
            _l.PushType1(arg1);
            _l.PushType2(arg2);
            _l.PushType3(arg3);

            // Do the actual call.
            _l.DoCall(num_args, num_ret);

            // Get the results from the stack. maybe
            var tbl = _l.ToTableEx(-1); // or ToInteger() etc
            if (tbl is null)
            {

            }
            _l.Pop(num_ret); // Clean up results.
        }




        ///// Lua calls C# functions
        // lua_func_name
        // cs_func_name
        // rettype 
        // argtypes[]  req/opt?
        // like:
        //   func_name
        //   FuncName
        //   I
        //   IS 
        //   work lambda?
        // gens:
        static int FuncName(IntPtr p)
        {
            Lua? l = Lua.FromIntPtr(p);
            int arg1;
            string arg2;

            if (l is null)
            {
               // throw?
            }

            // Get args.
            if (l.IsInteger(1))
            {
                arg1 = l.ToInteger(1);
            }
            else
            {
               // throw?
            }
            if (l.IsString(2))
            {
                arg1 = l.ToStringL(2);
            }
            else
            {
               // throw?
            }

            // Do the work - or lambda?
            double ret = FuncName_DoWork(arg1, arg2);

            // Return results.
            l.PushNumber(ret);
            return 1;
        }

        // client supplies this work function
        static double FuncName_DoWork(int level, string msg)
        {
            _logger.Log((LogLevel)level!, msg ?? "???");
            double totalMsec = 0;
            if (on)
            {
                _startTicks = _sw.ElapsedTicks; // snap
            }
            else if (_startTicks > 0)
            {
                long t = _sw.ElapsedTicks; // snap
                totalMsec = (t - _startTicks) * 1000D / Stopwatch.Frequency;
            }
            return totalMsec;
        }

    }
}
