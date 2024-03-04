# Nebulua TODO2 update all + parts of Nebulator\README.md

- An experimental version of Nebulator using Lua as the script flavor.
- Windows only.
- Windows 64 bit. Build with VS or mingw64. I'm using 64 bit Lua 5.4.2 from https://luabinaries.sourceforge.net/download.html.

- Test code is Windows 64 bit build using CMake. Your PATH must include `...\mingw64\bin`. <<<<<?????

- Uses [C code conventions](https://github.com/cepthomas/c_bag_of_tricks/blob/master/conventions.md).



char buff[MAX_STRING]; // MAX_STRING kinda klunky.

## timing
Midi DeltaTicksPerQuarterNote aka sub parts per beat/qtr_note = 8 = 32nd note resolution
Gives 32nd note resolution.
Fast timer resolution set to 1 msec.
int bpm = 40 -> 188 msec period.
int bpm = 240 -> 31 msec period.


## field types:
S = string
N = number
I = integer
F = function
M = map index(0-9)
T = TableEx
V = void
B = boolean
X = bar time(Number?)
E = expression?


## Glossary
L2C - Lua to C aka script calls host functions.
C2L - C to Lua aka host calls script functions

int chan_num - 
int dev_index - internal index into device table
char* dev_name - from Windows system
not: int dev_id - 
    int chan_hnd (16bit: dev_index << 8 | chan_num)
    int controller (name - midi_defs)
    int value (controller payload)
    int note_num (0-127)
    double volume (0.0-1.0) 0 means note off
    int velocity (0-127) 0 means note off
    int bar (absolute)
    int beat (in bar)
    int sub (in beat) - "musical"
    int subs ??
    int tick (absolute time - maybe in sequence/section/composition) - same size as sub

Script defs:
   BAR is 0->N, BEAT is 0->neb.BEATS_PER_BAR-1, SUB is 0->neb.SUBS_PER_BEAT-1
   WHAT_TO_PLAY is a string (see neb.get_notes_from_string(s)) or integer or function.
   BAR_TIME is a string of "BAR.BEAT.SUB" e.g. "1.2.3" or "1.2" or "1".
   VOLUME 0->9

--[[
Each section is 8 beats.
Each sequence is 4 sections => 32 beats.
A 4 minute song at 80bpm is 320 beats => 10 sequences => 40 sequences.

If each sequence has average 8 notes => total of 320 notes per instrument.
A 4 minute song at 80bpm is 320 beats => 10 sequences => 40 sequences => 320 notes.
A "typical" song would have about 4000 on/off events.
]]


## error/status/streams  --- Some should be in lbot maybe.

New flavor:
-- If one still insists on a dogma though, here is what I would say:
-- - Use errors for things which can be fixed at the time of writing the code (i.e. invalid pattern in string.match)
-- - return nil in case of errors which can always occur at runtime (i.e. couldn't open file in io.open)
-- and use pcall to overrule a decision to make something error (i.e. pcall(require, "luarocks.loader"))...

-- https://www.lua.org/gems/lpg113.pdf
-- if a failure situation is most often handled by the immediate caller of your function, signal it by return value.
-- Otherwise, consider the failure to be a first-class error and throw an exception.



Maybe:

custom stream for error output? use stderr? https://www.gnu.org/software/libc/manual/html_node/Custom-Streams.html

===== log
- traditional to FILE* (fp or stdout or)

lua-C:
int logger_Init(FILE* fp); // File stream to write to. Can be stdout.
int logger_Log(log_level_t level, int line, const char* format, ...);

lua-L:
host_api.log(level, msg) => calls the lua-C functions. there is no standalone lua-L logger.
> The I/O library provides two different styles for file manipulation. The first one uses implicit file handles; that is, there are operations to set a default input file and a default output file, and all input/output operations are done over these default files. The second style uses explicit file handles.
> When using implicit file handles, all operations are supplied by table io. When using explicit file handles, the operation io.open returns a file handle and then all operations are supplied as methods of the file handle.
> The table io also provides three predefined file handles with their usual meanings from C: io.stdin, io.stdout, and io.stderr. The I/O library never closes these files.
> Unless otherwise stated, all I/O functions return fail on failure, plus an error message as a second result and a system-dependent error code as a third result, and some non-false value on success.


===== print/printf
- also dump/Dump - like print/printf but larger size
- ok in standalone scripts like gen_interop.lua  pnut_runner.lua  etc
- ok in C main functions
- ok in test code
- quicky debug - don't leave them in
> use fp or stdout only
> lua-L print => io.write() -- default is stdout, change with io.output()
> lua-C printf => fprintf(FILE*) -- default is stdout, change with lautils_SetOutput(FILE* fout); user supplies FILE* fout


===== error
- means fatal here.
-   => originate in lua-L code (like user/script syntax errors) or in lua-C code for similar situations.
- lua-L:
    - Only the app (top level - user visible) calls error(message [, level]) to notify the user of e.g. app syntax errors.
    - internal libs should never call error(), let the client deal.
> use stdout or kustom only + maybe log_error()

-- lua-L print => io.write() -- default is stdout, change with io.output(). probably print() is fine for debugging, no need for special stream.

! lua-L error(message [, level])  Raises an error (see §2.3) with message as the error object. This function never returns.
... these trickle up to the caller via luaex_docall/lua_pcall return

! lua-C host does not call luaL_error(lua_State *L, const char *fmt, ...);
only call luaL_error() in code that is called from the lua side. C side needs to handle host-call-lua() manually via status codes, error msgs, etc.


- => collected/handled by:
- lua-C lua_pcall (lua_State *L, int nargs, int nresults, int msgh);
Calls a function (or a callable object) in protected mode.
    => only exec.c - probably? use luaex_docall()
! lua-C app luaex_docall(lua_State* l, int narg, int nres)
    => calls lua_pcall()
    => has _handler which calls luaL_traceback(l, l, msg, 1);

? lua-L pcall (f [, arg1, ···]) Calls the function f with the given arguments in protected mode.
    => only used for test, debugger, standalone scripts
    => not currently used: xpcall (f, msgh [, arg1, ···])  sets a new message handler msgh.


## sequences
-- Graphical format:
-- "|7-------|" is one beat with 8 subs
-- note velocity is 1-9 (map) or - which means sustained
-- note/chord, velocity/volume
-- List format:
-- times are beat.sub where beat is 0-N sub is 0-7
-- note/chord, velocity/volume is 0.0 to 1.0, duration is 0.1 to N.7

## API



## Files
- `source_code` folder:
    - exec.c/h - Does all the top-level work.
    - etc...
- `lua_lib` folder:
- `lua_code` folder:
    - nebulua.lua - Lua script for a simplistic multithreaded coroutine application. Uses luatoc.
    - utils.lua - Used by demoapp.lua.
- `test_code` folder:
    - lua source code for this application...
- `examples` folder:
    - lua...
- `mingw` folder:
- `app` and `app\test` folder:

