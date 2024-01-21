
--- Core generic functions for this app. Matches/requires the C libs.


local M = {}


---------------------------- okokokok -------------------------------
---------------------------- okokokok -------------------------------
---------------------------- okokokok -------------------------------

--- Log levels - must match those in the host C code.
M.LOG_LEVEL = { DBG = 1, INF = 2, ERR = 3 }

--- Convenience functions.
function M.error(msg) api.log(M.LOG_LEVEL.ERR, msg) end
function M.info(msg)  api.log(M.LOG_LEVEL.INF, msg) end
function M.debug(msg) api.log(M.LOG_LEVEL.DBG, msg) end


---------------------------- maybe -------------------------------
---------------------------- maybe -------------------------------
---------------------------- maybe -------------------------------

--- Creates a function that returns false until the arg is exceeded.
-- @param msec Number of milliseconds to wait.
-- @return status Function that returns state of the timer.
function M.delay_timer(msec)
  -- Init our copies of the args.
  local timeout = msec

  -- Grab the start time.
  local start = li.msec()
  
  -- The accessor function.
  local status =
    function()
      -- Are we there yet?
      return (li.msec() - start) > timeout
    end  
      
  return { status = status }
end

-- Blocking wait.
-- @param time Sleep time in msec.
function M.sleep(time)
  local timer = M.delay_timer(time)
  while not timer.status() do coroutine.yield() end
end


-----------------------------TODO1-NEB all event/data handling below ----------------------
-----------------------------------------------------------------------------
-----------------------------------------------------------------------------


-----------------------------------------------------------------------------
--- Process all script info into discrete steps.
-- @param name type desc
-- @return list of step_info ordered by subbeat
function M.process_all(sequences, sections)

    -- index is seq name, value is steps list.
    local seq_step_infos = {}

    for seq_name, seq_steps in ipairs(sequences) do
        -- test types?

        local step_infos = {}

        for _, seq_step in ipairs(seq_steps) do
            local gr_steps = nil

            if #seq_steps == 2 then
                gr_steps = parse_graphic_steps(seq_steps)
            elseif #seq_steps >= 3 then
                gr_steps = parse_explicit_notes(seq_steps)
            end

            if gr_steps == nil then
                log.error("input_note") -- string.format("%s", variable_name), channel_name, note, vel)
            else
                step_infos[seq_name] = gr_steps
            end
        end
    end

    table.sort(seq_step_infos, function (left, right) return left.subbeat < right.subbeat end)

    return seq_step_infos
end


-----------------------------------------------------------------------------
--- Parse a pattern.
-- @param notes_src like: [ "|M-------|--      |        |        |7-------|--      |        |        |", "G4.m7" ]
-- @return partially filled-in step_info list
function parse_graphic_notes(notes_src)

    -- [ "|        |        |        |5---    |        |        |        |5-8---  |", "D6" ],
    -- [ "|M-------|--      |        |        |7-------|--      |        |        |", "G4.m7" ],
    -- [ "|7-------|--      |        |        |7-------|--      |        |        |", 84 ],
    -- [ "|7-------|--      |        |        |7-------|--      |        |        |", drum.AcousticSnare ],
    -- [ "|        |        |        |5---    |        |        |        |5-8---  |", sequence_func ]

    local step_infos = {}

    local note = notes_src[2]
    local tnote = type(notes_src[2])
    local notes = {}
    local func = nil

    if tnote == "number" then
        -- use as is
        table.insert(notes, note)
    elseif tnote == "function" then
        -- use as is
        func = note
    elseif tnote == "string" then
        notes = md.get_notes(src)
    else
        step_infos = nil
    end        

    -- Remove visual markers from pattern.
    local pattern = notes_src[1].Replace("|", "")

    local current_vol = 0 -- default, not sounding
    local start_offset = 0 -- in pattern for the start of the current event

    for i = 1, #pattern do
        local c = pattern[i]

        if c == '-' then
            -- Continue current note.
            if current_vol > 0 then
                -- ok, do nothing
            else
                -- invalid condition
                throw new InvalidOperationException("Invalid \'-\'' in pattern string");
            end
        elseif c >= '1' and c <= '9' then
            -- A new note starting.
            -- Do we need to end the current note?
            if current_vol > 0 then
                make_note_event(i - 1)
            end
            -- Start new note.
            current_vol = pattern[i] - '0'
            start_offset = i - 1
        elseif c == ' ' or c == '.' then
            -- No sound.
            -- Do we need to end the current note?
            if current_vol > 0 then
                make_note_event(i - 1)
            end
            current_vol = 0
        else
            -- Invalid char.
            throw new InvalidOperationException("Invalid char in pattern string [{pattern[i]}]")
        end
    end

    -- Straggler?
    if current_vol > 0 then
        make_note_event(#pattern - 1)
    end

    -- Local function to package an event.
    function make_note_event(offset)
        -- offset is 0-based.
        local volmod = current_vol / 10
        local dur = offset - start_offset
        local when = start_offset
        local si = nil

        if func then
            si = { step_type=STEP_TYPE.FUNCTION, subbeat=when, function=func, volume=volmod, duration=dur }
        else
            si = { step_type=STEP_TYPE.NOTE, subbeat=when, notenum=src, volume=volmod, duration=dur }
        end
        table.insert(step_infos, si)
    end
end

-----------------------------------------------------------------------------
--- Description
-- @param notes_src like: [ 0.4, 44, 5, 0.4 ]
-- @return partially filled-in type_info list
function parse_explicit_notes(notes_src)

    -- [ 0.0, drum.AcousticBassDrum,  4, 0.1 ],
    -- [ 0.4, 44,                     5, 0.4 ],
    -- [ 7.4, "A#min",                7, 1.2 ],
    -- [ 4.0, sequence_func,          7      ],

    local step_infos = {}

    local start = to_subbeats(notes_src[1])
    local note = notes_src[2]
    local tnote = type(notes_src[2])
    local volume = notes_src[3]
    local duration = notes_src[4] or 0.1
    local si = nil

    if tnote == "number" then
        -- use as is
        si = { step_type=STEP_TYPE.NOTE, subbeat=start, notenum=src, volume=volume / 10 }
        table.insert(step_infos, si)
    elseif tnote == "function" then
        -- use as is
        si = { step_type=STEP_TYPE.FUNCTION, subbeat=start, function=src, volume=volume / 10 }
        table.insert(step_infos, si)
    elseif tnote == "string" then
        local notes = md.get_notes(src)
        for n in notes do
            si = { step_type=STEP_TYPE.NOTE, subbeat=start, notenum=n, volume=volume / 10 }
            table.insert(step_infos, si)
    else
        step_infos = nil
    end        
    return step_infos
end

-----------------------------------------------------------------------------
--- Process notes at this tick.
-- @param name type desc
-- @return type desc
function M.do_step(send_stuff, bar, beat, subbeat)
    -- calc total subbeat
    -- get all 
    -- return status?


end


-----------------------------------------------------------------------------
--- Construct a subbeat from beat.subbeat representation as a double.
-- @param d number value to convert
-- @return type desc
function M.to_subbeats(dbeat)

    local integral = math.truncate(dbeat)
    local fractional = dbeat - integral
    local beats = (int)integral
    local subbeats = (int)math.round(fractional * 10.0)

    if (subbeats >= LOW_RES_PPQ)
        --throw new Exception($"Invalid subbeat value: {beat}")
    end

    -- Scale subbeats to native.
    subbeats = subbeats * INTERNAL_PPQ / LOW_RES_PPQ
    total_subbeats = beats * SUBBEATS_PER_BEAT + subbeats
end


-- Return the module.
return M


--[[ old stuff to recycle? TODO1-NEB


--- return table:
-- index = subbeat
-- value = msg_info list to play
function M.process_sequence(seq)
    -- Length in beats.
    local seq_beats = 1
    -- All notes in an instrument sequence.
    local elements = {}
    -- Parse seq string.
    local seq_name = "???"
    local seq_lines = sx.strsplit(seq, "\n")
    for i = 1, #seq_lines do
        local seq_line = seq_lines[i]
        -- One note or chord or function etc in the sequence. Essentially something that gets played.
        local elem = {}

        -- process line here
        -- public void Add(string pattern, string what, double volume)
        -- Notes = MusicDefinitions.GetNotesFromString(s);
        -- if(Notes.Count == 0)
        -- {
        --     // It might be a drum.
        --     try
        --     {
        --         int idrum = MidiDefs.GetDrumNumber(s);
        --         Notes.Add(idrum);
        --     }
        --     catch { }
        -- }
        -- Individual note volume.
        elem.vol = 0.8
        -- When to play in Sequence. BarTime?
        elem.when = 3.3
        -- Time between note on/off. 0 (default) means not used. BarTime?
        elem.dur = 1.4
        -- The 0th is the root note and other values comprise possible chord notes.
        elem.notes = {} -- ints
        -- or call a script function.
        elem.func = nil
        -- Store.
        table.insert(elements, elem)
    end
    -- sequences[seq_name] = elements
    -- Return sequence info.
    return { elements = elements, seq_beats = seq_beats }
}

-- For viewing pleasure. ToString()
--     return $"Sequence: Beats:{Beats} Elements:{Elements.Count}";
--     return $"SequenceElement: When:{When} NoteNum:{Notes[0]:F2} Volume:{Volume:F2} Duration:{Duration}";

-- sect is a list of lists.
function M.process_section(sect)
-- Length in beats.
-- public string Name { get; set; } = "";
-- Collection of sequences in this section.
-- public SectionElements Elements { get; set; } = new SectionElements();
-- Length in beats.
-- public int Beats { get; set; } = 0;

-- For viewing pleasure. ToString()
--     return $"Section: Beats:{Beats} Name:{Name} Elements:{Elements.Count}";
--     return $"SectionElement: ChannelName:{ChannelName}";

        /// <summary>
        /// Get all section names and when they start. The end marker is also added.
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, string> GetSectionMarkers()
        {
            Dictionary<int, string> info = new();
            int when = 0;

            foreach (Section sect in _sections)
            {
                info.Add(when, sect.Name);
                when += sect.Beats;
            }

            // Add the dummy end marker.
            info.Add(when, "");

            return info;
        }

        /// <summary>
        /// Get all events.
        /// </summary>
        /// <returns>Enumerator for all events.</returns>
        public IEnumerable<MidiEventDesc> GetEvents()
        {
            return _scriptEvents;
        }

        /// <summary>
        /// Generate events from sequence notes.
        /// </summary>
        /// <param name="channel">Which channel to send it on.</param>
        /// <param name="seq">Which notes to send.</param>
        /// <param name="startBeat">Which beat to start sequence at.</param>
        List<MidiEventDesc> ConvertToEvents(Channel channel, Sequence seq, int startBeat)
        {
            List<MidiEventDesc> events = new();

            foreach (SequenceElement seqel in seq.Elements)
            {
                // Create the note start and stop times.
                BarTime startNoteTime = new BarTime(startBeat * MidiSettings.LibSettings.SubbeatsPerBeat) + seqel.When;
                BarTime stopNoteTime = startNoteTime + (seqel.Duration.TotalSubbeats == 0 ? new(1) : seqel.Duration); // 1 is a short hit

                // Is it a function?
                if (seqel.ScriptFunction is not null)
                {
                    FunctionMidiEvent evt = new(startNoteTime.TotalSubbeats, channel.ChannelNumber, seqel.ScriptFunction);
                    events.Add(new(evt, channel.ChannelName));
                }
                else // plain ordinary
                {
                    // Process all note numbers.
                    foreach (int noteNum in seqel.Notes)
                    {
                        ///// Note on.
                        double vel = channel.NextVol(seqel.Volume) * _masterVolume;
                        int velPlay = (int)(vel * MidiDefs.MAX_MIDI);
                        velPlay = MathUtils.Constrain(velPlay, MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);

                        NoteOnEvent evt = new(startNoteTime.TotalSubbeats, channel.ChannelNumber, noteNum, velPlay, seqel.Duration.TotalSubbeats);
                        events.Add(new(evt, channel.ChannelName));
                    }
                }
            }

            return events;
        }

]]