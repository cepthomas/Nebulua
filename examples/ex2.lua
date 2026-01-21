
--[[
An example Nebulua file that:
  - uses a DAW for host.
  - demonstrates alternate instrument definitions

Some  won't work unless you have the exact same HW/SW configuration.
]]


-- Import modules this needs.
local api = require("script_api")
-- local mus = require("music_defs")
local mid = require("midi_defs")
local def = require("defs_api")
local mt  = require("music_time")
local ut  = require("lbot_utils")
local sx  = require("stringex")


-- Alternate instrument names - for Acoustica Expanded Instruments presets.
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
local ctrl = mid.controllers
local expi  = exp_instruments

-- Say hello.
api.log_info('Loading daw_host.lua...')



------------------------- Configuration -------------------------------

-- Midi channels. Adjust for your configuration.
local midi_device_in  = "loopMIDI Port 1"
-- local midi_device_in  = "MPK mini"
local hnd_ccin  = api.open_input_channel(midi_device_in, 1, "my input")

local midi_device_out  = "loopMIDI Port 2"  -- DAW host
local hnd_keys    = api.open_output_channel(midi_device_out, 1,  "keys",     inst.Glockenspiel)
local hnd_bass    = api.open_output_channel(midi_device_out, 2,  "bass",     inst.SynthBass1)
local hnd_strings = api.open_output_channel(midi_device_out, 4,  "strings",  expi.BrightVox)
-- PanFlute 075 StringEnsemble1 048


------------------------- Variables -----------------------------------

-- Get some stock chords and scales.
local my_scale = def.get_notes_from_string("C4.o7")

-- Create custom note collection.
def.create_definition("MY_CHORD", "1 +3 4 -b7")
local my_chord = def.get_notes_from_string("B4.MY_CHORD")


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
    api.set_volume(hnd_strings, 0.9)

    return ''
end

-----------------------------------------------------------------------------
-- Main work loop called every subbeat/tick. Required.
function step(tick)
    -- Overhead.
    -- api.process_step(tick)

    -- Other work you may want to do. Like do something every new bar.
        local bar, beat, sub = mt.tick_to_mt(tick)
        if beat == 2 and sub == 0 then
            -- api.send_midi_controller(hnd_synth, ctrl.Pan, 90)
            api.log_info(string.format("step() do something"))
        end

    return 0
end

---------------------------------------------------------------------------
-- Handler for input note events. Optional.
function receive_midi_note(chan_hnd, note_num, volume)
    if chan_hnd == hnd_ccin then
        -- Play the note.
        -- api.log_debug(string.format("RCV hnd_ccin note:%d chan_hnd:%d volume:%f", note_num, chan_hnd, volume))
        api.send_note(hnd_strings, note_num, volume)--, 0)
    end
    return 0
end
