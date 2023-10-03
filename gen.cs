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

        ///// C# calls lua functions //TODOS

// FuncName
// argtypes[]
// rettype N I S B T V
// >>>
// step
// III
// T

        public void Step(int bar, int beat, int subbeat)
        {
            // Get the function to be called. Check return.
            _l.GetGlobal("step");

            // Push the arguments to the call.
            _l.PushInteger(bar);
            _l.PushInteger(beat);
            _l.PushInteger(subbeat);

            // Do the actual call.
            _l.DoCall(3, 1);

            // Get the results from the stack.
            var tbl = _l.ToTableEx(-1);
            _l.Pop(1); // Clean up results.
        }

        //================================================

// FuncName
// argtypes[]
// rettype N I S B T V
// >>>
// step
// III
// T

        //////// Lua calls C# functions TODOS all these need implementation and arg int/string handling
        /// <summary> </summary>
        static int Log(IntPtr p)
        {
            Lua l = Lua.FromIntPtr(p)!;

            var s = l.DumpStack();

            // Get args.
            var level = l.ToInteger(1);
            var msg = l.ToStringL(2);

            // Do the work.
            _logger.Log((LogLevel)level!, msg ?? "???");

            // Do the work.
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

            // Return results.
            l.PushNumber(totalMsec);
            return 1;
        }
    }
}
