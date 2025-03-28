-- Used to build and test all parts of the Nebulua universe.

-- Fix up the lua path first.
package.path = './lua/?.lua;./test/lua/?.lua;'..package.path

local ut = require('lbot_utils')
local sx = require('stringex')

local current_dir = io.popen("cd"):read()
local opt = arg[1]
local ret_code = 0



-- Make pretty.
ut.set_colorize({ ['Build: ']='green', ['! ']='red', ['): error ']='red', ['): warning ']='yellow' })
local function _output_text(text)
    for _, v in ipairs(ut.colorize_text(text)) do print(v) end
end

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
    print('If you really need to do this, see https://github.com/cepthomas/Nebulua/blob/main/docs/tech_notes.md#updating-script.')

elseif opt == 'dev' then
    local exp_neb = {'lua_interop', 'setup', 'step', 'receive_midi_note', 'receive_midi_controller' }
    extra, missing = ut.check_globals(exp_neb)
    res = ut.dump_list(extra)
    _output_text('extra:'..res)
    res = ut.dump_list(missing)
    _output_text('missing:'..res)

elseif opt == 'math' then
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


return ret_code
