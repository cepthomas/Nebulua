using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Midi;
using Ephemera.NBagOfTricks;


namespace Nebulua
{
    /// <summary>
    /// A midi input device.
    /// </summary>
    public class MidiInput : IDisposable
    {
        #region Fields
        /// <summary>NAudio midi input device.</summary>
        readonly MidiIn? _midiIn = null;
        #endregion

        #region Properties
        /// <summary>Device name as defined by the system.</summary>
        public string DeviceName { get; }

        /// <summary>Key is channel number, 1-based. Value is enabled.</summary>
        public Dictionary<int, bool> ChannelStates = [];

        /// <summary>T=enabled F=disabled null=unused.</summary>
    //    /*public*/  bool?[] ChannelState { get; } = new bool?[Common.NUM_MIDI_CHANNELS];

        // /// <summary>Device capture on/off.</summary>
        // public bool Enable
        // {
        //     get { return _enable; }
        //     set { if (value) _midiIn?.Start(); else _midiIn?.Stop(); _enable = value; }
        // }
        // bool _enable = false;
        #endregion

        #region Events
        /// <summary>Client needs to deal with this.</summary>
        public event EventHandler<MidiEvent>? ReceiveEvent;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor. OK to throw in here.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        /// <exception cref="SyntaxException"></exception>
        public MidiInput(string deviceName)
        {
            bool realInput = false;
            DeviceName = deviceName;
    //        ChannelState.ForEach(b => b = null);
            ChannelStates.Clear();
            //CaptureEnable = true;

            // Figure out which midi input device.
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                if (deviceName == MidiIn.DeviceInfo(i).ProductName)
                {
                    _midiIn = new MidiIn(i);
                    _midiIn.MessageReceived += MidiIn_MessageReceived;
                    _midiIn.ErrorReceived += MidiIn_ErrorReceived;
                    realInput = true;
                    break;
                }
            }

            // Assume internal type.
            if (!realInput)
            {
                _midiIn = null;
            }
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        public void Dispose()
        {
            _midiIn?.Stop();
            _midiIn?.Dispose();
        }
        #endregion

        #region Traffic
        /// <summary>
        /// Process real midi input event. Don't throw in this thread!
        /// </summary>
        void MidiIn_MessageReceived(object? sender, MidiInMessageEventArgs e)
        {
            // Decode the message. We only care about a few.
            MidiEvent evt = MidiEvent.FromRawMessage(e.RawMessage);

            // Is it in our registered inputs?
            if (ChannelStates.TryGetValue(evt.Channel, out bool chst) && chst)
            {
                // Invoke takes care of cross-thread issues.
                ReceiveEvent?.Invoke(this, evt);
            }
            // else ignore.


            //// Is it in our registered inputs?
            //int chan_num = evt.Channel;
            //if (Channels[chan_num - 1]) //-> ChannelState
            //{
            //    // Invoke takes care of cross-thread issues.
            //    ReceiveEvent?.Invoke(this, evt);
            //}
            //// else ignore.
        }

        /// <summary>
        /// Process error midi event - parameter 1 is invalid. Do I care?
        /// </summary>
        void MidiIn_ErrorReceived(object? sender, MidiInMessageEventArgs e)
        {
            // string ErrorInfo = $"Message:0x{e.RawMessage:X8}";
        }
        #endregion
    }

    /// <summary>
    /// A midi output device.
    /// </summary>
    public class MidiOutput : IDisposable
    {
        #region Fields
        /// <summary>NAudio midi output device.</summary>
        readonly MidiOut? _midiOut = null;
        #endregion

        #region Properties
        /// <summary>Device name as defined by the system.</summary>
        public string DeviceName { get; }

        // /// <summary>Enable output.</summary>
        // public bool Enable { get; set; } = true;

        /// <summary>T=enabled F=disabled null=unused.</summary>
   //     /*public*/ bool?[] ChannelState { get; } = new bool?[Common.NUM_MIDI_CHANNELS];

        /// <summary>Current patch. null=unknown.</summary>
//        /*public*/ int?[] Patch { get; } = new int?[Common.NUM_MIDI_CHANNELS];


        /// <summary>Key is channel number, 1-based. Value is enabled.</summary>
        public Dictionary<int, bool> ChannelStates = [];

        /// <summary>Key is channel number, 1-based. Value is current patch.</summary>
        public Dictionary<int, int> Patches = [];



        // /// <summary>Key is channel number, 1-based. Value is current patch.</summary>
        // public bool?[] ChannelState { get; } = new bool?[Common.NUM_MIDI_CHANNELS];
        // public Dictionary<int, int> Patch = [];
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor. OK to throw in here.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        /// <exception cref="SyntaxException"></exception>
        public MidiOutput(string deviceName)
        {
            DeviceName = deviceName;
            //Channels.ForEach(b => b = false);

            // Figure out which midi output device.
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                if (deviceName == MidiOut.DeviceInfo(i).ProductName)
                {
                    _midiOut = new MidiOut(i);
                    break;
                }
            }

            if (_midiOut is null)
            {
                List<string> devs = ["Valid midi outputs:"];
                for (int i = 0; i < MidiOut.NumberOfDevices; i++)
                {
                    devs.Add($"[{MidiOut.DeviceInfo(i).ProductName}]");
                }
                throw new SyntaxException($"Invalid output device name: {deviceName}. {string.Join(" ", devs)}");
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        public void Dispose()
        {
            // Resources.
            _midiOut?.Dispose();
        }
        #endregion

        #region Traffic
        /// <summary>
        /// Send midi event. OK to throw in here.
        /// </summary>
        public void Send(MidiEvent evt)
        {
            // Is it in our registered outputs?
            if (ChannelStates.TryGetValue(evt.Channel, out bool chst) && chst)
            {
                _midiOut?.Send(evt.GetAsShortMessage());
            }
            // else ignore.




            //// Is it in our registered inputs?
            //if (Channels[evt.Channel - 1])
            //{
            //    _midiOut?.Send(evt.GetAsShortMessage());
            //}
        }
        #endregion
    }
}
