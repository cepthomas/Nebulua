
-- A take on Eno's "algorithmic" Music for Airports - ported from github.com/teropa/musicforairports.js

api = require("neb_api")
ut = require("utils")

devices =
{
    sound = { dev_type="midi_out", channel=1, patch="Pad2Warm" },
}


SOUND_VOL = 0.8

-- Possible loops.
local loops = {}


------------------------- Init ----------------------------------------------------
-- Called to initialize stuff.
function setup()
    api.log("airport initialization")

    -- The tape loops. Values all BarTime.
    loops = {
        -- Key is Ab.
        { snote="Ab4", duration=17.3, delay=8.1,  next_start = 0.0 },
        -- octave
        { snote="Ab5", duration=17.2, delay=3.1,  next_start = 0.0 },
        -- 3rd
        { snote="C5",  duration=21.1, delay=5.3,  next_start = 0.0 },
        -- 4th
        { snote="Db5", duration=18.2, delay=12.3, next_start = 0.0 },
        -- 5th
        { snote="Eb5", duration=20.0, delay=9.2,  next_start = 0.0 },
        -- 6th
        { snote="F4",  duration=19.3, delay=4.2,  next_start = 0.0 },
        -- octave
        { snote="F5",  duration=20.0, delay=14.1, next_start = 0.0 },
    }}
end

------------------------- Main loop -------------------------------------------

-- Called every mmtimer increment.
function step(bar, beat, subdiv)
    step_time = 1.0 -- TODO1 use something like BarTime??

    for i = 1, #loops do
        if step_time >= loops[i].next_start then
            ut.info("Starting note", loops[i].snote);
            api.send_note("sound", loops[i].snote, SOUND_VOL, loops[i].duration);
            // Calc next time.
            loops[i].next_start = step_time + loops[i].delay + loops[i].duration;
        end
    end
end
