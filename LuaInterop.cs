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
        /// <returns>bool Required empty.></returns>
        public bool? Step(int bar, int beat, int subbeat)
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
            bool? ret = _l.ToBoolean(-1);
            if (ret is null) { ErrorHandler(new SyntaxException("Return value is not a bool")); return null; }
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

        /// <summary>Host export function: If volume is 0 note_off else note_on. If dur is 0 dur = note_on with dur = 0.1 (for drum/hit).
        /// Lua arg: "channel">Channel name.
        /// Lua arg: "notenum">Note number.
        /// Lua arg: "volume">Volume between 0.0 and 1.0.
        /// Lua arg: "dur">Duration as bar.beat.
        /// Lua return: bool Required empty.>
        /// </summary>
        /// <param name="p">Internal lua state</param>
        /// <returns>Number of lua return values></returns>
        int SendNote(IntPtr p)
        {
            Lua l = Lua.FromIntPtr(p)!;

            // Get arguments
            string? channel = null;
            if (l.IsString(1)) { channel = l.ToString(1); }
            else { ErrorHandler(new SyntaxException($"Bad arg type for {channel}")); return 0; }
            int? notenum = null;
            if (l.IsInteger(2)) { notenum = l.ToInteger(2); }
            else { ErrorHandler(new SyntaxException($"Bad arg type for {notenum}")); return 0; }
            double? volume = null;
            if (l.IsNumber(3)) { volume = l.ToNumber(3); }
            else { ErrorHandler(new SyntaxException($"Bad arg type for {volume}")); return 0; }
            double? dur = null;
            if (l.IsNumber(4)) { dur = l.ToNumber(4); }
            else { ErrorHandler(new SyntaxException($"Bad arg type for {dur}")); return 0; }

            // Do the work. One result.
            bool ret = SendNote_Work(channel, notenum, volume, dur);
            l.PushBoolean(ret);
            return 1;
        }

        /// <summary>Host export function: Send an explicit note on immediately. Caller is responsible for sending note off later.
        /// Lua arg: "channel">Channel name.
        /// Lua arg: "notenum">Note number.
        /// Lua arg: "volume">Volume between 0.0 and 1.0.
        /// Lua return: bool Required empty.>
        /// </summary>
        /// <param name="p">Internal lua state</param>
        /// <returns>Number of lua return values></returns>
        int SendNoteOn(IntPtr p)
        {
            Lua l = Lua.FromIntPtr(p)!;

            // Get arguments
            string? channel = null;
            if (l.IsString(1)) { channel = l.ToString(1); }
            else { ErrorHandler(new SyntaxException($"Bad arg type for {channel}")); return 0; }
            int? notenum = null;
            if (l.IsInteger(2)) { notenum = l.ToInteger(2); }
            else { ErrorHandler(new SyntaxException($"Bad arg type for {notenum}")); return 0; }
            double? volume = null;
            if (l.IsNumber(3)) { volume = l.ToNumber(3); }
            else { ErrorHandler(new SyntaxException($"Bad arg type for {volume}")); return 0; }

            // Do the work. One result.
            bool ret = SendNoteOn_Work(channel, notenum, volume);
            l.PushBoolean(ret);
            return 1;
        }

        /// <summary>Host export function: Send an explicit note off immediately.
        /// Lua arg: "channel">Channel name.
        /// Lua arg: "notenum">Note number.
        /// Lua return: bool Required empty.>
        /// </summary>
        /// <param name="p">Internal lua state</param>
        /// <returns>Number of lua return values></returns>
        int SendNoteOff(IntPtr p)
        {
            Lua l = Lua.FromIntPtr(p)!;

            // Get arguments
            string? channel = null;
            if (l.IsString(1)) { channel = l.ToString(1); }
            else { ErrorHandler(new SyntaxException($"Bad arg type for {channel}")); return 0; }
            int? notenum = null;
            if (l.IsInteger(2)) { notenum = l.ToInteger(2); }
            else { ErrorHandler(new SyntaxException($"Bad arg type for {notenum}")); return 0; }

            // Do the work. One result.
            bool ret = SendNoteOff_Work(channel, notenum);
            l.PushBoolean(ret);
            return 1;
        }

        /// <summary>Host export function: Send a controller immediately.
        /// Lua arg: "channel">Channel name.
        /// Lua arg: "ctlr">Specific controller.
        /// Lua arg: "value">Specific value.
        /// Lua return: bool Required empty.>
        /// </summary>
        /// <param name="p">Internal lua state</param>
        /// <returns>Number of lua return values></returns>
        int SendController(IntPtr p)
        {
            Lua l = Lua.FromIntPtr(p)!;

            // Get arguments
            string? channel = null;
            if (l.IsString(1)) { channel = l.ToString(1); }
            else { ErrorHandler(new SyntaxException($"Bad arg type for {channel}")); return 0; }
            int? ctlr = null;
            if (l.IsInteger(2)) { ctlr = l.ToInteger(2); }
            else { ErrorHandler(new SyntaxException($"Bad arg type for {ctlr}")); return 0; }
            int? value = null;
            if (l.IsInteger(3)) { value = l.ToInteger(3); }
            else { ErrorHandler(new SyntaxException($"Bad arg type for {value}")); return 0; }

            // Do the work. One result.
            bool ret = SendController_Work(channel, ctlr, value);
            l.PushBoolean(ret);
            return 1;
        }

        /// <summary>Host export function: Send a midi patch immediately.
        /// Lua arg: "channel">Channel name.
        /// Lua arg: "patch">Specific patch.
        /// Lua return: bool Required empty.>
        /// </summary>
        /// <param name="p">Internal lua state</param>
        /// <returns>Number of lua return values></returns>
        int SendPatch(IntPtr p)
        {
            Lua l = Lua.FromIntPtr(p)!;

            // Get arguments
            string? channel = null;
            if (l.IsString(1)) { channel = l.ToString(1); }
            else { ErrorHandler(new SyntaxException($"Bad arg type for {channel}")); return 0; }
            int? patch = null;
            if (l.IsInteger(2)) { patch = l.ToInteger(2); }
            else { ErrorHandler(new SyntaxException($"Bad arg type for {patch}")); return 0; }

            // Do the work. One result.
            bool ret = SendPatch_Work(channel, patch);
            l.PushBoolean(ret);
            return 1;
        }

        #endregion

        #region Infrastructure
        // Bind functions to static instance.
        static Script? _instance;
        // Bound functions.
        static LuaFunction? _Log;
        static LuaFunction? _SendNote;
        static LuaFunction? _SendNoteOn;
        static LuaFunction? _SendNoteOff;
        static LuaFunction? _SendController;
        static LuaFunction? _SendPatch;
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
            _SendNote = _instance!.SendNote;
            _libFuncs.Add(new LuaRegister("send_note", _SendNote));
            _SendNoteOn = _instance!.SendNoteOn;
            _libFuncs.Add(new LuaRegister("send_note_on", _SendNoteOn));
            _SendNoteOff = _instance!.SendNoteOff;
            _libFuncs.Add(new LuaRegister("send_note_off", _SendNoteOff));
            _SendController = _instance!.SendController;
            _libFuncs.Add(new LuaRegister("send_controller", _SendController));
            _SendPatch = _instance!.SendPatch;
            _libFuncs.Add(new LuaRegister("send_patch", _SendPatch));

            _libFuncs.Add(new LuaRegister(null, null));
            _l.RequireF("neb_api", OpenInterop, true);
        }
        #endregion
    }
}
