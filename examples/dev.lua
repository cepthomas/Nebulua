-- Playing with lua/nebulua features.

-- Import modules this needs.
local api = require("script_api")
local mus = require("music_defs")
local mid = require("midi_defs")
local bt  = require("bar_time")
local ut  = require('lbot_utils')
local sx  = require("stringex")


-- Use arbitrary lua files. require needs path fixup.
local _, _, dir = ut.get_caller_info(2)
if not sx.contains(package.path, dir) then -- already there?
    package.path = dir..'/?.lua;'..package.path
end
local oo = require("other")


-- Setup for debug.
-- ut.config_debug(true)
-- dbg()


-- Say hello.
api.log_info('Loading dev.lua in '..dir)


-- Aliases
local inst = mid.instruments
local drum = mid.drums
local kit  = mid.drum_kits
local ctrl = mid.controllers

local snare = drum.AcousticSnare
local bdrum = drum.AcousticBassDrum
local hhcl = drum.ClosedHiHat
local ride = drum.RideCymbal1
local crash = drum.CrashCymbal2
local mtom = drum.HiMidTom


-- Example of how to check for extraneous and missing globals.
local function _gcheck()
    local exp_neb = {'luainterop', 'setup', 'step', 'receive_midi_note', 'receive_midi_controller'}
    local extra, missing = ut.check_globals(exp_neb)
    api.log_debug('extra:'..ut.dump_list(extra))
    api.log_debug('missing:'..ut.dump_list(missing))
end


local fp = io.open('C:/Dev/repos/Apps/Nebulua/_dump.txt', 'w+')
fp:write(ut.dump_table_string(package, 0, 'package!!!')..'\n')
fp:write(ut.dump_table_string(package.loaded, 0, 'package.loaded')..'\n')
fp:write(ut.dump_table_string(_G, 0, '_G')..'\n')
fp:close()


------------------------- Configuration -------------------------------

-- Specify midi channels.
local hin = api.open_midi_input("ccMidiGen", 1)

-- DAW or VST host.
local use_host = false

local midi_out = ut.tern(use_host, "loopMIDI Port", "VirtualMIDISynth #1")
local hnd_keys  = api.open_midi_output(midi_out, 1, ut.tern(use_host, mid.NO_PATCH, inst.AcousticGrandPiano))
local hnd_bass  = api.open_midi_output(midi_out, 2, ut.tern(use_host, mid.NO_PATCH, inst.AcousticBass))
local hnd_synth = api.open_midi_output(midi_out, 3, ut.tern(use_host, mid.NO_PATCH, inst.VoiceOohs))
local hnd_drums = api.open_midi_output(midi_out, 10, ut.tern(use_host, mid.NO_PATCH, kit.Jazz))


------------------------- Variables -----------------------------------

-- Misc vars.
local valid = true

-- Forward refs.
local seq_func


------------------------- Canned Sequences ----------------------------

local drums_seq =
{
    -- | beat 0 | beat 1 | beat 2 | beat 3 | beat 4 | beat 5 | beat 6 | beat 7 |,  WHAT_TO_PLAY
    -- | beat 0 | beat 1 | beat 2 | beat 3 | beat 0 | beat 1 | beat 2 | beat 3 |,  WHAT_TO_PLAY
    { "|8       |        |8       |        |8       |        |8       |        |", bdrum },
    { "|    8   |        |    8   |    8   |    8   |        |    8   |    8   |", snare },
    { "|        |     8 8|        |     8 8|        |     8 8|        |     8 8|", hhcl }
}

local keys_seq =
{
    -- | beat 0 | beat 1 |........|........|........|........|........|........|,  WHAT_TO_PLAY
    { "|6-------|--      |        |        |7-------|--      |        |        |", "G4.m7" },
    { "|7-------|--      |        |        |7-------|--      |        |        |",  84 },
    { "|        |        |        |5---    |        |        |        |5-8---  |", "D6" },
}

local bass_seq =
{
    -- | beat 0 | beat 1 |........|........|........|........|........|........|,  WHAT_TO_PLAY
    { "|8-------|        |        |        |8-------|        |        |        |", "D3" },
}

local drums_seq_steps = api.parse_sequence_steps(hnd_drums, drums_seq)
local keys_seq_steps = api.parse_sequence_steps(hnd_keys, keys_seq)
local bass_seq_steps = api.parse_sequence_steps(hnd_bass, bass_seq)

local my_scale = mus.get_notes_from_string("G3.Algerian")



------------------------- System Functions ----------------------------------

-----------------------------------------------------------------------------
-- Called once to initialize your script stuff. Required.
function setup()

    _gcheck()

    -- How fast you wanna go?
    api.set_tempo(80)

    return 'nada'
end

-----------------------------------------------------------------------------
-- Main work loop called every subbeat/tick. Required.
function step(tick)
    if valid then
        local bar, beat, sub = bt.tick_to_bt(tick)

        if bar == 1 and beat == 0 and sub == 0 then
            -- _gcheck()
            api.send_sequence_steps(keys_seq_steps, tick)
        end

        if beat == 0 and sub == 0 then
            api.send_sequence_steps(drums_seq_steps, tick)
            oo.do_something()
        end

        -- Every 2 bars
        if (bar == 0 or bar == 2) and beat == 0 and sub == 0 then
            api.send_sequence_steps(bass_seq_steps, tick)
        end
    end

    -- Overhead.
    api.process_step(tick)

    return 0
end

-----------------------------------------------------------------------------
-- Handler for input note events. Optional.
function receive_midi_note(chan_hnd, note_num, volume)
    -- api.log_debug(string.format("RCV note:%d hnd:%d vol:%f", note_num, chan_hnd, volume))

    if chan_hnd == hin then
        -- Play the note.
        api.send_midi_note(hnd_synth, note_num, volume)
    end
    return 0
end

-----------------------------------------------------------------------------
-- Handlers for input controller events. Optional.
function receive_midi_controller(chan_hnd, controller, value)
    if chan_hnd == hin then
        -- Do something.
        api.log_debug(string.format("RCV controller:%d hnd:%d val:%d", controller, chan_hnd, value))
    end
    return 0
end


------------------------- Local Functions -----------------------------------

-----------------------------------------------------------------------------
-- Do something.
seq_func = function(tick)
    local note_num = math.random(1, #my_scale)
    api.send_midi_note(hnd_synth, my_scale[note_num], 0.9, 8)
    api.send_sequence_steps(keys_seq_steps, tick)
end


------------------------- Lab Functions -----------------------------------


-- play area for higher level constructs.

-- Main work loop called every subbeat/tick. Required.
function step(tick)
    if valid then
        -- Do something. TODO1 switch/pattern matching like F#/C#? >>>
        -- https://stackoverflow.com/questions/37447704
        -- http://lua-users.org/wiki/SwitchStatement


        local bar, beat, sub = bt.tick_to_bt(tick)

        if bar == 1 and beat == 0 and sub == 0 then
            -- _gcheck()
            api.send_sequence_steps(keys_seq_steps, tick)
        end

        if beat == 0 and sub == 0 then
            api.send_sequence_steps(drums_seq_steps, tick)
            oo.do_something()
        end

        -- Every 2 bars
        if (bar == 0 or bar == 2) and beat == 0 and sub == 0 then
            api.send_sequence_steps(bass_seq_steps, tick)
        end

    end

    -- Overhead.
    api.process_step(tick)

    return 0
end


--[[
//Patterns and Switch
//public static string GetInstrumentName(int which)
string ret = which switch
{
    -1 => "NoPatch",
    >= 0 and < MAX_MIDI => _instrumentNames[which],
    _ => throw new ArgumentOutOfRangeException(nameof(which)),
};
return ret;

//IEnumerable<EventDesc> GetFilteredEvents(string patternName, List<int> channels, bool sortTime)
IEnumerable<EventDesc> descs = ((uint)patternName.Length, (uint)channels.Count) switch
{
    ( 0,  0) => AllEvents.AsEnumerable(),
    ( 0, >0) => AllEvents.Where(e => channels.Contains(e.ChannelNumber)),
    (>0,  0) => AllEvents.Where(e => patternName == e.PatternName),
    (>0, >0) => AllEvents.Where(e => patternName == e.PatternName && channels.Contains(e.ChannelNumber))
};
// Always order.
return sortTime ? descs.OrderBy(e => e.AbsoluteTime) : descs;

//
_ = key switch
{
    Keys.Key_Reset  => ProcessEvent(E.Reset, key),
    Keys.Key_Set    => ProcessEvent(E.SetCombo, key),
    Keys.Key_Power  => ProcessEvent(E.Shutdown, key),
    _               => ProcessEvent(E.DigitKeyPressed, key)
};

//
tmsec = snap switch
{
    SnapType.Coarse => MathUtils.Clamp(tmsec, MSEC_PER_SECOND, true), // second
    SnapType.Fine => MathUtils.Clamp(tmsec, MSEC_PER_SECOND / 10, true), // tenth second
    _ => tmsec, // none
};

//
string s = ArrayType switch
{
    Type it when it == typeof(int) => "",
    Type it when it == typeof(int) => "",
};



//
switch (e.Button, ControlPressed(), ShiftPressed())
{
    case (MouseButtons.None, true, false): // Zoom in/out at mouse position
        break;
    case (MouseButtons.None, false, true): // Shift left/right
        break;
}

//
switch (ArrayType)
{
    case Type it when it == typeof(int):
        List<string> lvals = new();
        _elements.ForEach(f => lvals.Add(f.Value.ToString()!));
        ls.Add($"{sindent}{tableName}(IntArray):[ {string.Join(", ", lvals)} ]");
        break;
    case Type dt when dt == typeof(double):
        stype = "DoubleArray";
        break;
    case Type ds when ds == typeof(string):
        stype = "StringArray";
        break;
    default:
        stype = "Dictionary";
        break;
}
]]

