
-- An example Nebulua file that uses a DAW for host.


-- Use the debugger, trigger with a dbg() statement.
-- See https://github.com/cepthomas/LuaBagOfTricks/blob/main/debugex.lua.
local dbg = require("debugex")

-- Import modules this needs.
local api = require("script_api")
local mus = require("music_defs")
local mid = require("midi_defs")
local bt  = require("bar_time")
local ut  = require("lbot_utils")
local sx  = require("stringex")


-- Acoustica Expanded Instruments presets. TODO2 tbd_load_defs('exp_instruments.ini')
exp_instruments =
{
    AmbientWind = 000, AmbientWind2 = 001, AmbientWind3 = 002, AmbientWind4 = 003, AmbientWind5 = 004, AmbientStrings = 005, EightiesCheezeSynth = 006, 
    Jump = 007, FMChime = 008, FMBell = 009, StringPad = 010, GlassFlute = 011, SweetDreams = 012, RHString1 = 013, 
    RHString2 = 014, Streak = 015, Boom = 016, Drips = 017, WaterWhistle1 = 018, WaterWhistle2 = 019, WaterWhistle3 = 020, 
    BoyBand = 021, ShimmerVox = 022, StarChoir1 = 023, StarChoir2 = 024, SpacePiano = 025, GalaxyBell = 026, 
    OctaveStringPad = 027, OctaveStringPad2 = 028, OctaveStringPad3 = 029, OctaveStringPad4 = 030, LowNoise1 = 031, LowNoise2 = 032, 
    GreatNoise = 033, WineGlass = 034, WineGlassQ = 035, DrunkofftheVine = 036, DisorientingPad = 037, GlurbleVox = 038, 
    EtherealVox = 039, SynthGuitar1 = 040, SynthGuitar2 = 041, MetallicPad = 042, PadoftheOrient = 043, CleanandSynthGt = 044, 
    ShimmerBell = 045, MilkyWay = 046, WarmBells = 047, WarmBells2 = 048, CavernousStrings = 049, SlowElGuitar = 050, 
    BrightVox = 051, BrightVox2 = 052, OrganVox1 = 053, OrganVox2 = 054, EightiesGirl = 055, EightiesGirl2 = 056, 
    EightiesFretless = 057, EightiesFretless2 = 058, C64PulseBass = 059, C64BassandPerc = 060, C64PulseBass2 = 061, VoxPercussion = 062, 
    HallStringsFast = 063, HallStringsSlow = 064, DreamyHallStrings = 065
}


-- Aliases for imports - less typing.
local inst = mid.instruments
local drum = mid.drums
local kit  = mid.drum_kits
local ctrl = mid.controllers
local exinst = exp_instruments

-- Say hello.
api.log_info('Loading daw_host.lua...')



------------------------- Configuration -------------------------------

-- Midi channels. Adjust for your configuration.
local midi_device_in  = "loopMIDI Port 1"
-- local midi_device_in  = "MPK mini"
local hnd_ccin  = api.open_midi_input(midi_device_in, 1, "my input")

local midi_device_out  = "loopMIDI Port 2"  -- DAW host
local hnd_keys    = api.open_midi_output(midi_device_out, 1,  "keys",       inst.AcousticGrandPiano)
local hnd_bass    = api.open_midi_output(midi_device_out, 2,  "bass",       inst.AcousticBass)
local hnd_synth   = api.open_midi_output(midi_device_out, 3,  "synth",      inst.Lead1Square)
local hnd_strings = api.open_midi_output(midi_device_out, 4,  "strings",    exinst.BrightVox) --inst.StringEnsemble1)
-- PanFlute 075 StringEnsemble1 048
local hnd_drums   = api.open_midi_output(midi_device_out, 10, "drums",      kit.Jazz)


------------------------- Variables -----------------------------------

-- Get some stock chords and scales.
local my_scale = mus.get_notes_from_string("C4.o7")

-- Create custom note collection.
mus.create_definition("MY_CHORD", "1 +3 4 -b7")
local my_chord = mus.get_notes_from_string("B4.MY_CHORD")

-- Aliases for instruments.
local snare = drum.AcousticSnare
local bdrum = drum.AcousticBassDrum
local hhcl = drum.ClosedHiHat
local ride = drum.RideCymbal1
local crash = drum.CrashCymbal2
local mtom = drum.HiMidTom


-- Forward refs.
local _algo_func

-- dbg()

---------------------------------------------------------------------------
------------------------- System Functions --------------------------------
---------------------------------------------------------------------------

-----------------------------------------------------------------------------
-- Called once to initialize your script stuff. Required.
function setup()
    api.log_info("example initialization")

    -- How fast?
    api.set_tempo(88)

    -- Set master volumes.
    api.set_volume(hnd_keys, 0.7)
    api.set_volume(hnd_bass, 0.9)
    api.set_volume(hnd_synth, 0.6)
    api.set_volume(hnd_drums, 0.9)
    api.set_volume(hnd_strings, 0.9)

    -- dbg()

    -- This file uses static composition so you must call this!
    return api.process_comp()
end

-----------------------------------------------------------------------------
-- Main work loop called every subbeat/tick. Required.
function step(tick)
    -- Overhead.
    api.process_step(tick)

    -- Other work you may want to do. Like do something every new bar.
        local bar, beat, sub = bt.tick_to_bt(tick)
        if beat == 2 and sub == 0 then
            -- api.send_midi_controller(hnd_synth, ctrl.Pan, 90)
            _algo_func(tick)
        end

    return 0
end

---------------------------------------------------------------------------
-- Handler for input note events. Optional.
function receive_midi_note(chan_hnd, note_num, volume)
    if chan_hnd == hnd_ccin then
        -- Play the note.
        -- api.log_debug(string.format("RCV hnd_ccin note:%d chan_hnd:%d volume:%f", note_num, chan_hnd, volume))
        api.send_midi_note(hnd_strings, note_num, volume)--, 0)
    end
    return 0
end

---------------------------------------------------------------------------
-- Handlers for input controller events. Optional.
function receive_midi_controller(chan_hnd, controller, value)
    if chan_hnd == hnd_ccin then
        -- api.log_debug(string.format("RCV controller:%d chan_hnd:%d value:%d", controller, chan_hnd, value))
        -- Do something.
    end
    return 0
end

---------------------------------------------------------------------------
----------------------- Local Functions -----------------------------------
---------------------------------------------------------------------------

-- Function called from sequence.
_algo_func = function(tick)
    if my_scale ~= nil then
        local note_num = math.random(1, #my_scale)
        api.send_midi_note(hnd_synth, my_scale[note_num], 0.8, 3)
    end
end

---------------------------------------------------------------------------
------------------------- Composition -------------------------------------
---------------------------------------------------------------------------

-- Sequences -- each sequence is 8 beats = 2 bars, section is 8 bars

local quiet =
{
    { "|        |        |        |        |        |        |        |        |", 0 }
}

local example_seq =
{
    -- | beat 0 | beat 1 | beat 2 | beat 3 | beat 4 | beat 5 | beat 6 | beat 7 |,  WHAT_TO_PLAY
    -- |........|........|........|........|........|........|........|........|
    { "|6-------|--      |        |        |7-------|--      |        |        |", "G4.m7" },
    { "|7-------|--      |        |        |7-------|--      |        |        |",  84 },
    { "|        |        |        |5---    |        |        |        |5-8---  |", "D6" },
}

local drums_verse =
{
    -- |........|........|........|........|........|........|........|........|
    { "|8       |        |8       |        |8       |        |8       |        |", bdrum },
    { "|    8   |        |    8   |    8   |    8   |        |    8   |    8   |", snare },
    { "|        |     8 8|        |     8 8|        |     8 8|        |     8 8|", hhcl }
}

local drums_chorus =
{
    -- |........|........|........|........|........|........|........|........|
    { "|6       |        |6       |        |6       |        |6       |        |", bdrum },
    { "|        |7 7     |        |7 7     |        |7 7     |        |        |", ride },
    { "|        |    4   |        |        |        |    4   |        |        |", mtom },
    { "|        |        |        |        |        |        |        |8       |", crash },
}

local keys_verse =
{
    -- |........|........|........|........|........|........|........|........|
    { "|7-------|--      |        |        |7-------|--      |        |        |", "G4.m7" },
    { "|        |        |        |5---    |        |        |        |5-      |", "G4.m6" },
    { "|        |        |        |5---    |        |        |        |  8---  |", "B4.MY_CHORD" },
}

local keys_chorus =
{
    -- |........|........|........|........|........|........|........|........|
    { "|6-      |        |        |        |        |        |        |        |", "F4" },
    { "|    5-  |        |        |        |        |        |        |        |", "D#4" },
    { "|        |6-      |        |        |        |        |        |        |", "C4" },
    { "|        |    6-  |        |        |        |        |        |        |", "B4.m7" },
}

local bass_verse =
{
    -- |........|........|........|........|........|........|........|........|
    { "|9-------|        |        |        |        |        |        |        |", "C2" },
    { "|        |        |        |    7---|        |        |        |        |", "E2" },
    { "|        |        |        |        |        |        |        |    9---|", "A#2" },
}

local bass_chorus =
{
    -- |........|........|........|........|........|........|........|........|
    { "|5   8   |        |5   8   |        |5   8   |        |5   8   |        |", "C2" },
}


-- Sections -- each section is 8 bars
--                       0             2             4             6
api.sect_start("beginning")
api.sect_chan(hnd_keys,  keys_verse,   keys_verse,   keys_verse,   keys_verse)
api.sect_chan(hnd_drums, drums_verse,  drums_verse,  drums_verse,  drums_verse)
api.sect_chan(hnd_bass,  bass_verse,   bass_verse,   bass_verse,   bass_verse)

api.sect_start("middle")
api.sect_chan(hnd_keys,  keys_chorus,  keys_chorus,  keys_chorus,  keys_chorus)
api.sect_chan(hnd_drums, drums_chorus, drums_chorus, drums_chorus, drums_chorus)
api.sect_chan(hnd_bass,  bass_chorus,  bass_chorus,  bass_chorus,  bass_chorus)

api.sect_start("ending")
api.sect_chan(hnd_keys,  keys_verse,   keys_verse,   keys_verse,   keys_verse)
api.sect_chan(hnd_drums, drums_verse,  drums_verse,  drums_verse,  drums_verse)
api.sect_chan(hnd_bass,  bass_verse,   bass_verse,   bass_verse,   bass_verse)
