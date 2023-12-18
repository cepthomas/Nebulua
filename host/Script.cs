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
                        // TODO0 refactor this mess. GP elegant way to deal with optional lua fields.
                        string? device_id = props.Names.Contains("device_id") ? props["device_id"].ToString() : null;
                        int? channel_num = props.Names.Contains("channel") ? int.Parse(props["channel"].ToString()) : null;
                        int? patch = props.Names.Contains("patch") ? int.Parse(props["patch"].ToString()) : 0;
                        // virt_key options
                        bool show_note_names = props.Names.Contains("show_note_names") && bool.Parse(props["show_note_names"].ToString());
                        // bing_bong options   min_note, max_note, min_control, max_control?
                        bool draw_note_grid = props.Names.Contains("draw_note_grid") && bool.Parse(props["draw_note_grid"].ToString());
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
    }
}
