# LuaEx

Explain... changes

-- Lua == 5.4.6

- .NET6 sdk project for windows.
- Error handling:
    - https://www.lua.org/manual/5.4/manual.html#2.3
    - Error(); // never returns - useful?
    - throws exceptions. CheckLuaStatus() in
        - public bool DoFile(string file)
        - public bool DoString(string chunk)
        - public LuaStatus LoadFile(string file, string mode)

- these all call native and CheckLuaStatus()
    - LuaStatus Load(LuaReader reader, IntPtr data, string chunkName, string mode)
    - LuaStatus LoadBuffer(byte[] buffer, string? name, string? mode)
    - LuaStatus LoadFile(string file, string? mode = null)
    - LuaStatus LoadString(string chunk, string? name = null) 
    - LuaStatus PCall(int arguments, int results, int errorFunctionIndex)
    - LuaStatus PCallK(int arguments, int results, int errorFunctionIndex, int context, LuaKFunction k)
    - LuaStatus Resume(Lua from, int arguments, out int results)

- Add nullable.
- These made private:
    - LuaStatus Load(LuaReader reader, IntPtr data, string chunkName, string mode)
    - LuaStatus LoadBuffer(byte[] buffer)
    - LuaStatus LoadBuffer(byte[] buffer, string name)
    - LuaStatus LoadBuffer(byte[] buffer, string name, string mode)
    - LuaStatus LoadString(string chunk, string name)
- ToNumberX() and ToIntegerX() are removed and plain ToNumber() and ToInteger() returns nullables.
- Removed lots of overloaded funcs -> uses default args instead.

- API "State" changed to "L".

- No =>: Expression-bodied members are another set of features that simply add some syntactic convenience to C# to continue streamlining type definitions.

- Removed some arg checking - should be all or none. Client will have to handle things like null argument exc.



