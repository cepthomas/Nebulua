
-- Unit tests for music_defs.lua. TODO1-TEST

local md = require("music_defs")
local ut = require("utils")

-- Create the namespace/module.
local M = {}

-- ut.config_error_handling(true, true)


-----------------------------------------------------------------------------
function M.setup(pn)
    -- pn.UT_INFO("setup()!!!")
end


-----------------------------------------------------------------------------
function M.teardown(pn)
    -- pn.UT_INFO("teardown()!!!")
end


-----------------------------------------------------------------------------
function M.suite_music_defs(pn)
    pn.UT_INFO("Test all functions in music_defs.lua")

    ----- note_name_to_number()
    res = md.note_name_to_number("G#")
    pn.UT_EQUAL(res, 8)
    res = md.note_name_to_number("+D")
    pn.UT_EQUAL(res, 14)
    res = md.note_name_to_number("+Cb")
    pn.UT_EQUAL(res, 23)
    res = md.note_name_to_number("6")
    pn.UT_EQUAL(res, 5) --17
    res = md.note_name_to_number("-Db")
    pn.UT_EQUAL(res, -11) --1
    res = md.note_name_to_number("XXX")
    pn.UT_NIL(res)
    res = md.note_name_to_number(nil)
    pn.UT_NIL(res)


    ----- split_note_number().
    root, octave = md.split_note_number(5)
    pn.UT_EQUAL(root, 5)
    pn.UT_EQUAL(octave, 1)
    root, octave = md.split_note_number(34)
    pn.UT_EQUAL(root, 10)
    pn.UT_EQUAL(octave, 3)
    root, octave = md.split_note_number(68)
    pn.UT_EQUAL(root, 8)
    pn.UT_EQUAL(octave, 6)
    root, octave = md.split_note_number(nil)
    pn.UT_EQUAL(root, nil)
    pn.UT_EQUAL(octave, nil)


    ----- Create custom scales and chords.
    res = md.create_definition("MY_SCALE", "1 +3 4 -b7")
    pn.UT_NOT_NIL(res)
    res = md.create_definition("KRAZY_CHORD", "1 2 +#6 7 8 -9 b3 4 5 +2")
    pn.UT_NOT_NIL(res)
    res = md.create_definition("INVALID_CHORD", "1 +3 ABC -b7")
    pn.UT_NIL(res, serr)


    ----- Get scales using get_notes_from_string().
    -- "Lydian                  | 1 2 3 #4 5 6 7               | Lydian mode                              | whole tone        | major",
    res = md.get_notes_from_string("D#2.Lydian") -- stock
    if not pn.UT_NOT_NIL(res) then error("Fatal") end
    pn.UT_EQUAL(#res, 7)
    pn.UT_EQUAL(res[4], 45) -- should be  D# = 3 + 2*12 = 27
    pn.UT_EQUAL(res[7], 50)

    res = md.get_notes_from_string("Bb5.MY_SCALE") -- custom
    if not pn.UT_NOT_NIL(res) then error("Fatal") end
    pn.UT_EQUAL(#res, 4)
    pn.UT_EQUAL(res[2], 98)
    pn.UT_EQUAL(res[4], 80)

    ----- Get chords using get_notes_from_string();
    res = md.get_notes_from_string("C3.M7#11") -- stock
        -- "   | 1 3 5 7 9 #11     |",
    if not pn.UT_NOT_NIL(res) then error("Fatal") end
    pn.UT_EQUAL(#res, 6)
    pn.UT_EQUAL(res[2], 52)
    pn.UT_EQUAL(res[6], 66)

    res = md.get_notes_from_string("F#5.KRAZY_CHORD") -- custom
    if not pn.UT_NOT_NIL(res) then error("Fatal") end
    pn.UT_EQUAL(#res, 10)
    pn.UT_EQUAL(res[4], 89)
    pn.UT_EQUAL(res[9], 85)

    res, serr = md.get_notes_from_string("C.INVALID_CHORD") -- invalid
    pn.UT_NIL(res, serr)

    res, serr = md.get_notes_from_string("C.NONEXISTENT_CHORD") -- invalid
    pn.UT_NIL(res, serr)

    ----- format_doc().
    res = md.format_doc()
    pn.UT_EQUAL(#res, 83)

end

-----------------------------------------------------------------------------
-- Return the module.
return M
