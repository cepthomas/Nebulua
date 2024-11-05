-- Used to build and test all parts of the Nebulua universe.

-- Fix up the lua path first.
package.path = './lua/?.lua;./test/lua/?.lua;'..package.path

local ut = require('lbot_utils')
local sx = require('stringex')

local current_dir = io.popen("cd"):read()
local opt = arg[1]
local ret_code = 0


-- fwd refs
local _output_text


-------------------------------------------------------------------------

if opt == 'build_app' then
    local bld_exe = '"C:/Program Files/Microsoft Visual Studio/2022/Community/Msbuild/Current/Bin/MSBuild.exe"'
    -- -r = restore first
    -- -t:rebuild
    -- Verbosity levels: q[uiet], m[inimal], n[ormal] (default), d[etailed], and diag[nostic].
    vrb = '-v:m'

    _output_text('Build: Building app...')
    cmd = sx.strjoin(' ', { bld_exe, vrb, 'Nebulua.sln' } )
    res = ut.execute_and_capture(cmd)
    _output_text(res)

    _output_text('Build: Building app tests...')
    cmd = sx.strjoin(' ', { bld_exe, vrb, 'test/NebuluaTest.sln' } )
    res = ut.execute_and_capture(cmd)
    _output_text(res)

elseif opt == 'test_app' then
    _output_text('Build: Running app tests...')
    cmd = 'pushd "test/Cli/bin/x64/Debug/net8.0-windows" & TestCli.exe & popd'
    res = ut.execute_and_capture(cmd)
    _output_text(res)

    cmd = 'pushd "test/Core/bin/x64/Debug/net8.0-windows" & TestCore.exe & popd'
    res = ut.execute_and_capture(cmd)
    _output_text(res)

    cmd = 'pushd "test/Interop/bin/x64/Debug/net8.0-windows" & TestInterop.exe & popd'
    res = ut.execute_and_capture(cmd)
    _output_text(res)

    cmd = 'pushd "test/Misc/bin/x64/Debug/net8.0-windows" & TestMisc.exe & popd'
    res = ut.execute_and_capture(cmd)
    _output_text(res)

elseif opt == 'test_lua' then
    _output_text('Build: Running lua tests...')
    local pr = require('pnut_runner')

    rep = pr.do_tests('test_defs', 'test_bar_time', 'test_api')
    for _, s in ipairs(rep) do
        _output_text(s)
    end

elseif opt == 'gen_md' then
    _output_text('Build: Gen markdown from definition files...')

    mus = require('music_defs')
    mid = require('midi_defs')
    sx  = require('stringex')

    text = mus.gen_md()
    content = sx.strjoin('\n', text)
    f = io.open('docs/music_defs.md', 'w')
    f:write(content)
    f:close()

    text = mid.gen_md()
    content = sx.strjoin('\n', text)
    f = io.open('docs/midi_defs.md', 'w')
    f:write(content)
    f:close()

elseif opt == 'gen_interop' then
    -- Convert spec into interop files.
    _output_text('Build: Generating interop...')
    cmd = 'pushd "../../Libs/LuaBagOfTricks" & lua gen_interop.lua -ch '..current_dir..'/interop/interop_spec.lua '..current_dir..'/interop & popd'
    print(cmd)
    res = ut.execute_and_capture(cmd)
    _output_text(res)

elseif opt == 'dev' then
    local function do_one(name, func)
        v = {}
        table.insert(v, name)
        for i = 0, 9 do table.insert(v, string.format("%.2f", func(i)))  end
        print(sx.strjoin(', ', v))
    end

    do_one('linear', function(i) return i / 9 end)
    do_one('exp', function(i) return math.exp(i) / 8104 end)
    do_one('log', function(i) return math.log(i) / 2.2 end)
    do_one('log 10', function(i) return math.log(i, 10) / 0.95 end)
    do_one('pow 2', function(i) return i^2 / 81 end)
    do_one('pow 3', function(i) return i^3 end)
    do_one('pow 0.67', function(i) return i^0.67 / 4.36 end)
    -- do_one('pow 10', function(i) return i^10 / 81 end)
    -- do_one('pow -2', function(i) return i^-2 end)
    -- do_one('pow -10', function(i) return i^-10 end)

else
    if opt == nil then
        _output_text('Build: Error: Missing option - Select one of:')
    else
        _output_text('Build: Error: Invalid option '..opt..' - Select one of:')
    end

    _output_text('build_app  test_app  test_lua  gen_md  gen_interop')
    -- goto done
    ret_code = 1

end


-- Make pretty.
_output_text = function(text)
    -- Split into lines and colorize.

    local GRAY = string.char(27).."[95m" --"[90m"
    local RED = string.char(27).."[41m" --"[91m"
    local BLUE = string.char(27).."[94m"
    local YELLOW = string.char(27).."[33m"
    local GREEN = string.char(27).."[92m"
    local RESET = string.char(27).."[0m"

    lines = sx.strsplit(text, '\n', false)
    for _, s in ipairs(lines) do
        if sx.startswith(s, 'Build: ') then print(GREEN..s..RESET)
        elseif sx.startswith(s, '! ') then  print(RED..s..RESET)
        elseif sx.contains(s, '): error ') then print(RED..s..RESET)
        elseif sx.contains(s, '): warning ') then print(YELLOW..s..RESET)
        else  print(s)
        end
    end
end
-- spec = { ['Build: ']='green', ['! ']='red', ['): error ']='red', ['): warning ']='yellow' }

return ret_code
