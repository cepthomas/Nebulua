
local api = require("neb_api_sim") -- TODO1 do better
ut = require("utils")

-- Seed the randomizer.
local seed = os.time()
math.randomseed(seed)


function create_scale(scale, root, octDown, octUp)
    sc = {}
    sc.scale_notes = api.GetNotesFromString("{sroot}.{sscale}") --array of int
    sc.note_weights = {} --array of int
    sc.total_weight = 0
    sc.down = octDown --int
    sc.up = octUp --int

    -- Set default weights.
    for i = 1, #sc.note_weights do
        sc.note_weights[i] = 100 / #sc.scaleNotes
    end

    function sc.set_weight(index, weight)
        if(index < #sc.scaleNotes) then
            sc.note_weights[index] = weight
        end

        -- Recalc total weight.
        sc.total_weight = 0
        for i = 1, #sc.scaleNotes do
            sc.total_weight = sc.total_weight + sc.note_weights[i];
        end
    end

    function sc.random_note()
        note = 0
        offset = 0

        for i = 1, #sc.scaleNotes do
            offset = offset + sc.note_weights[i]
            if r < offset then
                note = sc.scale_notes[i]
            end
            -- Which octave?
            oct = math.random(0, 3) - 1
            note = note + oct * 12
        end

        return note;
    end

    return sc
end
