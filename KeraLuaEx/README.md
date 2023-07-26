# LuaEx

Explain... changes

- .NET6 sdk project for windows.
- Error handling -> throws exceptions.
- Add nullable.
- These made private:
    - LuaStatus Load(LuaReader reader, IntPtr data, string chunkName, string mode)
    - LuaStatus LoadBuffer(byte[] buffer)
    - LuaStatus LoadBuffer(byte[] buffer, string name)
    - LuaStatus LoadBuffer(byte[] buffer, string name, string mode)
    - LuaStatus LoadString(string chunk, string name)
- ToNumberX() and ToIntegerX() are removed and plain ToNumber() and ToInteger() returns nullables.


TODOE Lua errors: https://www.lua.org/manual/5.4/manual.html#2.3
Error(); // never returns
check all CheckLuaStatus

These throw for bad LuaStat/CheckLuaStatus:
- public bool DoFile(string file)
- public bool DoString(string chunk)
- public LuaStatus LoadFile(string file, string mode)

