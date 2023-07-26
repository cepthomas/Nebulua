using System;
using System.Runtime.InteropServices;

namespace KeraLuaEx
{
    static class DelegateExtensions
    {
        // All of these wrappers throw exceptions so arg checking is not reuired.
        public static LuaFunction ToLuaFunction(this IntPtr ptr)
        {
            return Marshal.GetDelegateForFunctionPointer<LuaFunction>(ptr);
        }

        public static IntPtr ToFunctionPointer(this LuaFunction d)
        {
            return Marshal.GetFunctionPointerForDelegate<LuaFunction>(d);
        }

        public static LuaHookFunction ToLuaHookFunction(this IntPtr ptr)
        {
            return Marshal.GetDelegateForFunctionPointer<LuaHookFunction>(ptr);
        }

        public static IntPtr ToFunctionPointer(this LuaHookFunction d)
        {
            // throws
            return Marshal.GetFunctionPointerForDelegate<LuaHookFunction>(d);
        }

        public static LuaKFunction ToLuaKFunction(this IntPtr ptr)
        {
            return Marshal.GetDelegateForFunctionPointer<LuaKFunction>(ptr);
        }

        public static IntPtr ToFunctionPointer(this LuaKFunction d)
        {
            return Marshal.GetFunctionPointerForDelegate<LuaKFunction>(d);
        }

        public static LuaReader ToLuaReader(this IntPtr ptr)
        {
            return Marshal.GetDelegateForFunctionPointer<LuaReader>(ptr);
        }

        public static IntPtr ToFunctionPointer(this LuaReader d)
        {
            return Marshal.GetFunctionPointerForDelegate<LuaReader>(d);
        }

        public static LuaWriter ToLuaWriter(this IntPtr ptr)
        {
            return Marshal.GetDelegateForFunctionPointer<LuaWriter>(ptr);
        }

        public static IntPtr ToFunctionPointer(this LuaWriter d)
        {
            return Marshal.GetFunctionPointerForDelegate<LuaWriter>(d);
        }

        public static LuaAlloc ToLuaAlloc(this IntPtr ptr)
        {
            return Marshal.GetDelegateForFunctionPointer<LuaAlloc>(ptr);
        }

        public static IntPtr ToFunctionPointer(this LuaAlloc d)
        {
            return Marshal.GetFunctionPointerForDelegate<LuaAlloc>(d);
        }

        public static LuaWarnFunction ToLuaWarning(this IntPtr ptr)
        {
            return Marshal.GetDelegateForFunctionPointer<LuaWarnFunction>(ptr);
        }

        public static IntPtr ToFunctionPointer(this LuaWarnFunction d)
        {
            return Marshal.GetFunctionPointerForDelegate<LuaWarnFunction>(d);
        }
    }
}
