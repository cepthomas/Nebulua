
local ut = require('utils')
local sx = require("stringex")

-- Create the namespace/module.
local M = {}

--- Definitions
NOTES_PER_OCTAVE = 12


--- All the builtin chord defs.
local chord_defs =
{
--  Chord    | Notes             | Description
    "M       | 1 3 5             | Named after the major 3rd interval between root and 3.",
    "m       | 1 b3 5            | Named after the minor 3rd interval between root and b3.",
    "7       | 1 3 5 b7          | Also called dominant 7th.",
    "M7      | 1 3 5 7           | Named after the major 7th interval between root and 7th major scale note.",
    "m7      | 1 b3 5 b7         |",
    "6       | 1 3 5 6           | Major chord with 6th major scale note added.",
    "m6      | 1 b3 5 6          | Minor chord with 6th major scale note added.",
    "o       | 1 b3 b5           | Diminished.",
    "o7      | 1 b3 b5 bb7       | Diminished added 7.",
    "m7b5    | 1 b3 b5 b7        | Also called minor 7b5.",
    "+       | 1 3 #5            | Augmented.",
    "7#5     | 1 3 #5 b7         |",
    "9       | 1 3 5 b7 9        |",
    "7#9     | 1 3 5 b7 #9       | The 'Hendrix' chord.",
    "M9      | 1 3 5 7 9         |",
    "Madd9   | 1 3 5 9           | Chords extended beyond the octave are called added when the 7th is not present.",
    "m9      | 1 b3 5 b7 9       |",
    "madd9   | 1 b3 5 9          |",
    "11      | 1 3 5 b7 9 11     | The 3rd is often omitted to avoid a clash with the 11th.",
    "m11     | 1 b3 5 b7 9 11    |",
    "7#11    | 1 3 5 b7 #11      | Often used in preference to 11th chords to avoid the dissonant clash between 11 and 3 .",
    "M7#11   | 1 3 5 7 9 #11     |",
    "13      | 1 3 5 b7 9 11 13  | The 11th is often omitted to avoid a clash with the 3rd.",
    "M13     | 1 3 5 7 9 11 13   | The 11th is often omitted to avoid a clash with the 3rd.",
    "m13     | 1 b3 5 b7 9 11 13 |",
    "sus4    | 1 4 5             |",
    "sus2    | 1 2 5             | Sometimes considered as an inverted sus4 (GCD).",
    "5       | 1 5               | Power chord."
}

--- All the builtin scale defs.
local scale_defs =
{
--  Scale                    | Notes                        | Description                              | Lower tetrachord  | Upper tetrachord
    "Acoustic                | 1 2 3 #4 5 6 b7              | Acoustic scale                           | whole tone        | minor",
    "Aeolian                 | 1 2 b3 4 5 b6 b7             | Aeolian mode or natural minor scale      | minor             | Phrygian",
    "NaturalMinor            | 1 2 b3 4 5 b6 b7             | Aeolian mode or natural minor scale      | minor             | Phrygian",
    "Algerian                | 1 2 b3 #4 5 b6 7             | Algerian scale                           |                   |",
    "Altered                 | 1 b2 b3 b4 b5 b6 b7          | Altered scale                            | diminished        | whole tone",
    "Augmented               | 1 b3 3 5 #5 7                | Augmented scale                          |                   |",
    "Bebop                   | 1 2 3 4 5 6 b7 7             | Bebop dominant scale                     |                   |",
    "Blues                   | 1 b3 4 b5 5 b7               | Blues scale                              |                   |",
    "Chromatic               | 1 #1 2 #2 3 4 #4 5 #5 6 #6 7 | Chromatic scale                          |                   |",
    "Dorian                  | 1 2 b3 4 5 6 b7              | Dorian mode                              | minor             | minor",
    "DoubleHarmonic          | 1 b2 3 4 5 b6 7              | Double harmonic scale                    | harmonic          | harmonic",
    "Enigmatic               | 1 b2 3 #4 #5 #6 7            | Enigmatic scale                          |                   |",
    "Flamenco                | 1 b2 3 4 5 b6 7              | Flamenco mode                            | Phrygian          | Phrygian",
    "Gypsy                   | 1 2 b3 #4 5 b6 b7            | Gypsy scale                              | Gypsy             | Phrygian",
    "HalfDiminished          | 1 2 b3 4 b5 b6 b7            | Half diminished scale                    | minor             | whole tone",
    "HarmonicMajor           | 1 2 3 4 5 b6 7               | Harmonic major scale                     | major             | harmonic",
    "HarmonicMinor           | 1 2 b3 4 5 b6 7              | Harmonic minor scale                     | minor             | harmonic",
    "Hirajoshi               | 1 3 #4 5 7                   | Hirajoshi scale                          |                   |",
    "HungarianGypsy          | 1 2 b3 #4 5 b6 7             | Hungarian Gypsy scale                    | Gypsy             | harmonic",
    "HungarianMinor          | 1 2 b3 #4 5 b6 7             | Hungarian minor scale                    | Gypsy             | harmonic",
    "In                      | 1 b2 4 5 b6                  | In scale                                 |                   |",
    "Insen                   | 1 b2 4 5 b7                  | Insen scale                              |                   |",
    "Ionian                  | 1 2 3 4 5 6 7                | Ionian mode or major scale               | major             | major",
    "Istrian                 | 1 b2 b3 b4 b5 5              | Istrian scale                            |                   |",
    "Iwato                   | 1 b2 4 b5 b7                 | Iwato scale                              |                   |",
    "Locrian                 | 1 b2 b3 4 b5 b6 b7           | Locrian mode                             | Phrygian          | whole tone",
    "LydianAugmented         | 1 2 3 #4 #5 6 7              | Lydian augmented scale                   | whole tone        | diminished",
    "Lydian                  | 1 2 3 #4 5 6 7               | Lydian mode                              | whole tone        | major",
    "Major                   | 1 2 3 4 5 6 7                | Ionian mode or major scale               | major             | major",
    "MajorBebop              | 1 2 3 4 5 #5 6 7             | Major bebop scale                        |                   |",
    "MajorLocrian            | 1 2 3 4 b5 b6 b7             | Major Locrian scale                      | major             | whole tone",
    "MajorPentatonic         | 1 2 3 5 6                    | Major pentatonic scale                   |                   |",
    "MelodicMinorAscending   | 1 2 b3 4 5 6 7               | Melodic minor scale (ascending)          | minor             | varies",
    "MelodicMinorDescending  | 1 2 b3 4 5 b6 b7 8           | Melodic minor scale (descending)         | minor             | major",
    "MinorPentatonic         | 1 b3 4 5 b7                  | Minor pentatonic scale                   |                   |",
    "Mixolydian              | 1 2 3 4 5 6 b7               | Mixolydian mode or Adonai malakh mode    | major             | minor",
    "NeapolitanMajor         | 1 b2 b3 4 5 6 7              | Neapolitan major scale                   | Phrygian          | major",
    "NeapolitanMinor         | 1 b2 b3 4 5 b6 7             | Neapolitan minor scale                   | Phrygian          | harmonic",
    "Octatonic               | 1 2 b3 4 b5 b6 6 7           | Octatonic scale (or 1 b2 b3 3 #4 5 6 b7) |                   |",
    "Persian                 | 1 b2 3 4 b5 b6 7             | Persian scale                            | harmonic          | unusual",
    "PhrygianDominant        | 1 b2 3 4 5 b6 b7             | Phrygian dominant scale                  | harmonic          | Phrygian",
    "Phrygian                | 1 b2 b3 4 5 b6 b7            | Phrygian mode                            | Phrygian          | Phrygian",
    "Prometheus              | 1 2 3 #4 6 b7                | Prometheus scale                         |                   |",
    "Tritone                 | 1 b2 3 b5 5 b7               | Tritone scale                            |                   |",
    "UkrainianDorian         | 1 2 b3 #4 5 6 b7             | Ukrainian Dorian scale                   | Gypsy             | minor",
    "WholeTone               | 1 2 3 #4 #5 #6               | Whole tone scale                         |                   |",
    "Yo                      | 1 b3 4 5 b7                  | Yo scale                                 |                   |"
}

--- All possible note names and aliases as offset from middle C.
local note_names =
{
    ["C"]=0,  ["Db"]=1, ["D"]=2, ["Eb"]=3, ["E"]=4,  ["F"]=5,  ["Gb"]=6, ["G"]=7, ["Ab"]=8, ["A"]=9,  ["Bb"]=10, ["B"]=11,
    ["B#"]=0, ["C#"]=1,          ["D#"]=3, ["Fb"]=4, ["E#"]=5, ["F#"]=6,          ["G#"]=8,           ["A#"]=10, ["Cb"]=11,
    ["1"]=0,  ["2"]=1,  ["3"]=2, ["4"]=3,  ["5"]=4,  ["6"]=5,  ["7"]=6,  ["8"]=7, ["9"]=8,  ["10"]=9, ["11"]=1,  ["12"]=11
}

--- Intervals as used in chord and scale defs.
local intervals =
{
    ["1"]=0,   ["b2"]=1, ["2"]=2,  ["b3"]=3,  ["3"]=4,   ["4"]=5,    ["b5"]=6,  ["5"]=7, ["#5"]=8, ["b6"]=8, ["6"]=9, ["bb7"]=9,
    ["b7"]=10, ["7"]=11, ["9"]=14, ["#9"]=15, ["11"]=17, ["#11"]=18, ["13"]=21
}

------ Init stuff ------

--- The chord and scale note definitions. Key is chord/scale name, value is list of constituent intervals as strings.
M.chords_and_scales = {}

for _, sc in ipairs(scale_defs) do
    local parts = sx.strsplit(sc, "|")
    M.chords_and_scales[parts[1]] = sx.strsplit(parts[2], " ")
end

for _, sc in ipairs(chord_defs) do
    local parts = sx.strsplit(sc, "|")
    M.chords_and_scales[parts[1]] = sx.strsplit(parts[2], " ")
end

-----------------------------------------------------------------------------
--- Add a named chord or scale definition. Doesn't check for validity - that's done in get_notes_from_string().
-- Like "MY_SCALE", "1 +3 4 -b7"
-- @param name string which
-- @param notes string space separated note names
function M.create_notes(name, notes)
    M.chords_and_scales[name] = sx.strsplit(notes, " ")
end

-----------------------------------------------------------------------------
--- Parse note or notes from input value. Could look like:
--   F4 - named note
--   Bb2.dim7 - named key/chord
--   E#5.major - named key/scale
--   A3.MY_SCALE - user defined key/chord or scale
-- @param nstr string Standard string to parse.
-- @return List of note numbers or nil if invalid nstr.
function M.get_notes_from_string(nstr)
    local notes = {}

    -- Break it up.
    local parts = sx.strsplit(nstr, ".")
    local snote = parts[1]
    local scon = parts[2] -- may be nil

    -- Start with root and octave.
    local soct = snote[#snote]
    local octave = tonumber(soct)
    if not octave then -- not specified
        octave = 4 -- default is middle C
    else -- trim octave
        snote = snote.sub(1, #snote - 1)
    end
    local root_note_num = M.note_name_to_number(snote)

    -- Transpose octave.
    root_note_num = root_note_num + (octave + 1) * NOTES_PER_OCTAVE

    if scon then
        -- It's a chord or scale. Determine the constituents.
        local const_intervals = M.chords_and_scales[scon]
        if const_intervals then
            for cint in const_intervals do
                local nint = intervals[cint]
                if nint then
                    table.insert(notes, nint + root_note_num)
                else
                    -- error
                    return nil
                end
            end
        else
            -- error
            return nil
        end
    else
        -- Just the root.
        table.insert(notes, root_note_num)
    end

    return notes
end

-----------------------------------------------------------------------------
--- Convert note name into note number offset from middle C.
-- @param snote string The root of the note with optional +- octave shift. TODO3 multiple octaves?
-- @return The number or nil if invalid.
function M.note_name_to_number(snote)
    local inote = nil

    if snote ~= nil then
        local ch1 = snote:sub(1, 1)
        local up = false
        local dn = false
        if ch1 == '+' then
            up = true
            snote = snote:sub(2)
        elseif ch1 == '-' then
            dn = true
            snote = snote:sub(2)
        end
        inote = note_names[snote]

        -- Adjust for octave shift.
        if inote and up then inote = inote + NOTES_PER_OCTAVE end
        if inote and dn then inote = inote - NOTES_PER_OCTAVE end
    end

    return inote
end

-----------------------------------------------------------------------------
--- Split a midi note number into root note and octave.
-- @param notenum Absolute note number.
-- @return ints of root, octave or nil if invalid
function M.split_note_number(notenum)
    local root = nil
    local octave = nil
    if notenum ~= nil then
        root = notenum % NOTES_PER_OCTAVE
        octave = (notenum // NOTES_PER_OCTAVE) + 1
    end        
    return root, octave
end

-----------------------------------------------------------------------------
--- Make markdown content from the definitions.
-- @return list of strings
function M.format_doc()
    local docs = {}

    table.insert(docs, "# Chords")
    table.insert(docs, "These are the built-in chords.")
    table.insert(docs, "Chord   | Notes             | Description")
    table.insert(docs, "------- | ----------------- | -----------")
    for _, s in ipairs(chord_defs) do
        table.insert(docs, s)
    end

    table.insert(docs, "# Scales")
    table.insert(docs, "These are the built-in scales.")
    table.insert(docs, "Scale   | Notes             | Description       | Lower tetrachord  | Upper tetrachord")
    table.insert(docs, "------- | ----------------- | ----------------- | ----------------  | ----------------")
    for _, s in ipairs(scale_defs) do
        table.insert(docs, s)
    end

    return docs
end


-- Return the module.
return M
