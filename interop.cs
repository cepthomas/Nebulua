using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Collections.Generic;
//using System.Linq;
//using NAudio.Midi;
//using Ephemera.NBagOfTricks.Slog;
//using Ephemera.NBagOfTricks;
//using Ephemera.MidiLib;
using KeraLuaEx;

// For spec see C:\Dev\repos\Lua\LuaBagOfTricks\Test\Files\interop_test_spec.lua


namespace Ephemera.Nebulua
{
    public partial class Script
    {
        // TODOGEN generator fills these in place:
        static string my_lua_func_name_1 = "call_my_lua_func";
        static string my_lua_func_name_2 = "call_my_host_func";
        static string lib_name = "neb_api";
        static int num_args;
        static int num_ret;


        //---------------- Call lua functions from host -------------//
        public TableEx? interop_HostCallLua(string arg1, int arg2, Dictionary<string, object> arg3)
        {
            TableEx? ret = null;
            bool ok = true;

            // Get the function to be called. Check return.
            LuaType ltype = _l.GetGlobal(my_lua_func_name_1);
            if (ltype != LuaType.Function)
            {
                ok = false;
                ErrorHandler(new SyntaxException($"Bad lua function: {my_lua_func_name_1}"));
            }

            if (ok)
            {
                // Push the arguments to the call.
                _l.PushString(arg1);
                _l.PushInteger(arg2);
                _l.PushDictionary(arg3);

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

        //---------------- Call host functions from Lua -------------//

        static int interop_LuaCallHost(IntPtr p)
        {
            bool ok = true;
            int num_ret = 0;
            Lua? l = Lua.FromIntPtr(p);

            int? arg1 = null;
            string? arg2 = null;

            if (l is null)
            {
                ok = false;
                ErrorHandler(new LuaException("This should never happen"));
            }

            // Get args.
            if (ok)
            {
                if (l!.IsInteger(1))
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
                if (l!.IsString(2))
                {
                    arg2 = l.ToStringL(2);
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
                num_ret = 1;
            }

            return num_ret;
        }


        //------------------ Infrastructure ----------------------//

        public void Script_init()
        {
            // Load C# impl functions. This table gets pushed on the stack and into globals.
            _l.RequireF(lib_name, OpenMyLib, true);
        }

        int OpenMyLib(IntPtr p)
        {
            // Open lib into global table.
            var l = Lua.FromIntPtr(p)!;
            l.NewLib(_libFuncs);

            return 1;
        }

        // Bind the C# functions lua can call.
        readonly LuaRegister[] _libFuncs = new LuaRegister[]
        {
            new LuaRegister(my_lua_func_name_2, LuaCallHost),
            //etc.
            // new LuaRegister("send_controller", _fSendController),
            // new LuaRegister("send_note", _fSendNote),
            // new LuaRegister("send_note_on", _fSendNoteOn),
            // new LuaRegister("send_note_off", _fSendNoteOff),
            // new LuaRegister("send_patch", _fSendPatch),
            new LuaRegister(null, null)
        };

        // Bound static functions.
        //static readonly LuaFunction lua_func_2 = LuaCallHost;

    }
}
