using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NAudio.Midi;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfTricks;
using Ephemera.MidiLib;
using KeraLuaEx;
//using static Ephemera.Nebulua.Common;


namespace Ephemera.Nebulua
{
    public partial class Script : IDisposable
    {
        #region Properties that can be accessed in the user script
        /// <summary>Sound is playing. Lua: "playing".</summary>
        public bool Playing { set { _l.PushBoolean(value); _l.SetGlobal("playing"); } }

        /// <summary>Actual time since start pressed. Lua: "real_time".</summary>
        public double RealTime { set { _l.PushNumber(value); _l.SetGlobal("real_time"); } }

        /// <summary>Nebulator Speed in bpm. Lua: "tempo".</summary>
        public int Tempo
        {
            set
            {
                _tempo = value;
                _l.PushInteger(value);
                _l.SetGlobal("tempo");
            }
        }
        int _tempo;

        /// <summary>Nebulator master Volume. Lua: "master_volume".</summary>
        public double MasterVolume
        {
            set
            {
                _masterVolume = value;
                _l.PushNumber(value);
                _l.SetGlobal("master_volume");
            }
        }
        double _masterVolume;
        #endregion

        #region Fields
        /// <summary>Main logger.</summary>
        static readonly Logger _logger = LogManager.CreateLogger("Script");

        /// <summary>Main execution lua state.</summary>
        readonly Lua _l = new();

        /// <summary>All sections.</summary>
        internal List<Section> _sections = new();

        /// <summary>All the events defined in the script.</summary>
        internal List<MidiEventDesc> _scriptEvents = new();

        /// <summary>Resource clean up.</summary>
        internal bool _disposed = false;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public Script()
        {
            // Load interop bindings.
            LoadInterop();
            _instance = this;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _l.Close();
            }
        }
        #endregion

        /// <summary>
        /// Load file and init everything.
        /// This may throw an exception - client needs to handle them.
        /// </summary>
        /// <param name="fn">Lua file to open.</param>
        /// <param name="luaPaths">Optional additional lua paths.</param>
        public void LoadScript(string fn, List<string>? luaPaths = null)
        {
            // Load the script file.
            luaPaths ??= new();

            _l.SetLuaPath(luaPaths);

            // Load/parse the file.
            _l.LoadFile(fn);

            // Execute/init the script.
            _l.DoCall(0, Lua.LUA_MULTRET);

            // Get and init the channels.
            GetChannels();

            // Get the sequences and sections.
            GetComposition();
        }

        /// <summary>
        /// Get and init the channels.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        void GetChannels()
        {
            Common.OutputChannels.Clear();
            Common.InputChannels.Clear();

            _l.GetGlobal("channels");
            var channels = _l.ToTableEx(-1);
            _l.Pop(1);

            if (channels is not null)
            {
                foreach (var chname in channels.Names)
                {
                    var props = channels[chname] as TableEx;
                    var valid = props is not null;

                    if (valid)
                    {
                        // refactor this mess. GP elegant way to deal with optional lua fields.
                        string? device_id = props.Names.Contains("device_id") ? props["device_id"].ToString() : null;
                        int? channel_num = props.Names.Contains("channel") ? int.Parse(props["channel"].ToString()) : null;
                        int? patch = props.Names.Contains("patch") ? int.Parse(props["patch"].ToString()) : 0;
                        //bool show_note_names = props.Names.Contains("show_note_names") && bool.Parse(props["show_note_names"].ToString());
                        //bool draw_note_grid = props.Names.Contains("draw_note_grid") && bool.Parse(props["draw_note_grid"].ToString());

                        // required
                        valid = device_id is not null && channel_num is not null;

                        if (valid)
                        {
                            // Fill in the channel info with what this knows - main will fill in the blanks.
                            Channel channel = new()
                            {
                                ChannelName = chname,
                                ChannelNumber = (int)channel_num,
                                DeviceId = device_id,
                                Patch = (int)patch,
                                IsDrums = (int)channel_num! == MidiDefs.DEFAULT_DRUM_CHANNEL,
                            };

                            if (Common.OutputDevices.ContainsKey(device_id))
                            {
                                Common.OutputChannels.Add(chname, channel);
                            }
                            else if (Common.InputDevices.ContainsKey(device_id))
                            {
                                Common.InputChannels.Add(chname, channel);
                            }
                            else
                            {
                                //throw new InvalidOperationException($"Invalid device id {device_id} for {chname}");
                            }
                        }
                    }

                    if (!valid)
                    {
                        throw new InvalidOperationException($"Invalid channel spec for {chname}");
                    }
                }
            }
        }



        ///////////// everything below could/should? be in the script ///////////



        /// <summary>
        /// Get the sequences and sections.
        /// </summary>
        void GetComposition()
        {
            //_l.GetGlobal("_G");// "sequences"
            //var keys = GetKeys();
            //_l.Pop(1); // GetGlobal

            //foreach(var k in keys)
            //{
            //    if (k.StartsWith("seq_"))
            //    {
            //        _l.GetGlobal(k);
            //        var s = _l.ToStringL(-1);
            //        var parts = s!.SplitByTokens(Environment.NewLine);
            //    }
            //}
        }




        // CreateSequence(int beats, SequenceElements elements) -- -> Sequence
        // CreateSection(int beats, string name, SectionElements elements) -- -> Section

        /// <summary>
        /// Convert script sequences etc to internal events.
        /// </summary>
        public void BuildSteps()
        {
            // Build all the events.
            int sectionBeat = 0;

            foreach (Section section in _sections)
            {
                foreach (SectionElement sectel in section.Elements)
                {
                    if (sectel.Sequences.Length > 0)
                    {
                        // Current index in the sequences list.
                        int seqIndex = 0;

                        // Current beat in the section.
                        int beatInSect = 0;

                        while (beatInSect < section.Beats)
                        {
                            var seq = sectel.Sequences[seqIndex];
                            //was AddSequence(sectel.Channel, seq, sectionBeat + beatInSect);
                            var ch = Common.OutputChannels[sectel.ChannelName];
                            int beat = sectionBeat + beatInSect;
                            var ecoll = ConvertToEvents(ch, seq, beat);
                            _scriptEvents.AddRange(ecoll);

                            beatInSect += seq.Beats;
                            if (seqIndex < sectel.Sequences.Length - 1)
                            {
                                seqIndex++;
                            }
                        }
                    }
                }

                // Update accumulated time.
                sectionBeat += section.Beats;
            }
        }

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
    }
}
