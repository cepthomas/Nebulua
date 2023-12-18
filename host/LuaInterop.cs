///// Warning - this file is created by gen_interop.lua, do not edit. /////

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using KeraLuaEx;
using System.Diagnostics;

namespace Ephemera.Nebulua
{
    public partial class Script
    {
        #region Functions exported from lua for execution by host
        /// <summary>Lua export function: Called to initialize Nebulator stuff.</summary>
        /// <returns>bool Required empty.></returns>
        public bool? Setup()
        {
            int numArgs = 0;
            int numRet = 1;

            // Get function.
            LuaType ltype = _l.GetGlobal("setup");
            if (ltype != LuaType.Function) { ErrorHandler(new SyntaxException($"Bad lua function: setup")); return null; }

            // Push arguments.

            // Do the actual call.
            LuaStatus lstat = _l.DoCall(numArgs, numRet);
            if (lstat >= LuaStatus.ErrRun) { ErrorHandler(new SyntaxException("DoCall() failed")); return null; }

            // Get the results from the stack.
            bool? ret = _l.ToBoolean(-1);
            if (ret is null) { ErrorHandler(new SyntaxException("Return value is not a bool")); return null; }
            _l.Pop(1);
            return ret;
        }

        /// <summary>Lua export function: Called every mmtimer increment.</summary>
        /// <param name="bar"></param>
        /// <param name="beat"></param>
        /// <param name="subbeat"></param>
        /// <returns>TableEx List of midi events.></returns>
        public TableEx? Step(int bar, int beat, int subbeat)
        {
            int numArgs = 0;
            int numRet = 1;

            // Get function.
            LuaType ltype = _l.GetGlobal("step");
            if (ltype != LuaType.Function) { ErrorHandler(new SyntaxException($"Bad lua function: step")); return null; }

            // Push arguments.
            _l.PushInteger(bar);
            numArgs++;
            _l.PushInteger(beat);
            numArgs++;
            _l.PushInteger(subbeat);
            numArgs++;

            // Do the actual call.
            LuaStatus lstat = _l.DoCall(numArgs, numRet);
            if (lstat >= LuaStatus.ErrRun) { ErrorHandler(new SyntaxException("DoCall() failed")); return null; }

            // Get the results from the stack.
            TableEx? ret = _l.ToTableEx(-1);
            if (ret is null) { ErrorHandler(new SyntaxException("Return value is not a TableEx")); return null; }
            _l.Pop(1);
            return ret;
        }

        /// <summary>Lua export function: Called when input arrives. Optional.</summary>
        /// <param name="channel"></param>
        /// <param name="note"></param>
        /// <param name="val"></param>
        /// <returns>bool Required empty.></returns>
        public bool? InputNote(string channel, int note, int val)
        {
            int numArgs = 0;
            int numRet = 1;

            // Get function.
            LuaType ltype = _l.GetGlobal("input_note");
            if (ltype != LuaType.Function) { ErrorHandler(new SyntaxException($"Bad lua function: input_note")); return null; }

            // Push arguments.
            _l.PushString(channel);
            numArgs++;
            _l.PushInteger(note);
            numArgs++;
            _l.PushInteger(val);
            numArgs++;

            // Do the actual call.
            LuaStatus lstat = _l.DoCall(numArgs, numRet);
            if (lstat >= LuaStatus.ErrRun) { ErrorHandler(new SyntaxException("DoCall() failed")); return null; }

            // Get the results from the stack.
            bool? ret = _l.ToBoolean(-1);
            if (ret is null) { ErrorHandler(new SyntaxException("Return value is not a bool")); return null; }
            _l.Pop(1);
            return ret;
        }

        /// <summary>Lua export function: Called when input arrives. Optional.</summary>
        /// <param name="channel"></param>
        /// <param name="controller"></param>
        /// <param name="value"></param>
        /// <returns>bool Required empty.></returns>
        public bool? InputController(string channel, int controller, int value)
        {
            int numArgs = 0;
            int numRet = 1;

            // Get function.
            LuaType ltype = _l.GetGlobal("input_controller");
            if (ltype != LuaType.Function) { ErrorHandler(new SyntaxException($"Bad lua function: input_controller")); return null; }

            // Push arguments.
            _l.PushString(channel);
            numArgs++;
            _l.PushInteger(controller);
            numArgs++;
            _l.PushInteger(value);
            numArgs++;

            // Do the actual call.
            LuaStatus lstat = _l.DoCall(numArgs, numRet);
            if (lstat >= LuaStatus.ErrRun) { ErrorHandler(new SyntaxException("DoCall() failed")); return null; }

            // Get the results from the stack.
            bool? ret = _l.ToBoolean(-1);
            if (ret is null) { ErrorHandler(new SyntaxException("Return value is not a bool")); return null; }
            _l.Pop(1);
            return ret;
        }

        #endregion

        #region Functions exported from host for execution by lua
        /// <summary>Host export function: Script wants to log something.
        /// Lua arg: "level">Log level.
        /// Lua arg: "msg">Log message.
        /// Lua return: bool Required empty.>
        /// </summary>
        /// <param name="p">Internal lua state</param>
        /// <returns>Number of lua return values></returns>
        int Log(IntPtr p)
        {
            Lua l = Lua.FromIntPtr(p)!;

            // Get arguments
            int? level = null;
            if (l.IsInteger(1)) { level = l.ToInteger(1); }
            else { ErrorHandler(new SyntaxException($"Bad arg type for {level}")); return 0; }
            string? msg = null;
            if (l.IsString(2)) { msg = l.ToString(2); }
            else { ErrorHandler(new SyntaxException($"Bad arg type for {msg}")); return 0; }

            // Do the work. One result.
            bool ret = Log_Work(level, msg);
            l.PushBoolean(ret);
            return 1;
        }

        #endregion

        #region Infrastructure
        // Bind functions to static instance.
        static Script? _instance;
        // Bound functions.
        static LuaFunction? _Log;
        readonly List<LuaRegister> _libFuncs = new();

        int OpenInterop(IntPtr p)
        {
            var l = Lua.FromIntPtr(p)!;
            l.NewLib(_libFuncs.ToArray());
            return 1;
        }

        void LoadInterop()
        {
            _instance = this;
            _Log = _instance!.Log;
            _libFuncs.Add(new LuaRegister("log", _Log));

            _libFuncs.Add(new LuaRegister(null, null));
            _l.RequireF("neb_api", OpenInterop, true);
        }
        #endregion
    }
}
