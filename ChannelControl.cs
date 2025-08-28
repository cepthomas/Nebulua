using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace Nebulua
{
    /// <summary>Notify host of changes.</summary>
    public class ChannelControlEventArgs : EventArgs
    {
        public ChannelState State { get; set; }
        public int DeviceNumber { get; set; } = -1;
        public int ChannelNumber { get; set; } = -1;
    }

    /// <summary>Channel events and other properties.</summary>
    public class ChannelControl : UserControl
    {
        #region Fields
        readonly Container components = new();

        readonly int _channelNumber = -1;
        readonly int _deviceNumber = -1;
        ChannelState _state = ChannelState.Normal;
        readonly double _volume = 0.8;

        readonly Label lblChannelInfo;
        readonly Label lblSolo;
        readonly Label lblMute;
        readonly Slider sldVolume;
        #endregion

        #region Events
        /// <summary>Notify host of asynchronous changes from user.</summary>
        public event EventHandler<ChannelControlEventArgs>? ChannelControlChange;
        #endregion

        #region Properties
        /// <summary>For muting/soloing.</summary>
        public ChannelState State
        {
            get { return _state; }
            set { _state = value; UpdateUi(); }
        }

        /// <summary>Current volume.</summary>
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public double Volume
        {
            get { return sldVolume.Value; }
            set { sldVolume.Value = value; }
        }

        /// <summary>Indicate user selected.</summary>
        public Color SelectedColor { get; set; } = Color.Aquamarine;

        /// <summary>Indicate user not selected.</summary>
        public Color UnselectedColor { get; set; } = DefaultBackColor;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="channelNumber"></param>
        /// <param name="deviceNumber"></param>
        public ChannelControl(int channelNumber, int deviceNumber) : this()
        {
            _channelNumber = channelNumber;
            _deviceNumber = deviceNumber;
        }

        /// <summary>
        /// Designer constructor.
        /// </summary>
        public ChannelControl()
        {
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Size = new Size(345, 38);

            lblChannelInfo = new()
            {
                // AutoSize = true,
                Location = new Point(2, 8),
                Size = new Size(20, 20),
                Text = "#"
            };

            lblSolo = new()
            {
                Location = new Point(290, 7),
                Size = new Size(20, 20),
                Text = "S"
            };

            lblMute = new()
            {
                Location = new Point(315, 7),
                Size = new Size(20, 20),
                Text = "M"
            };

            sldVolume = new()
            {
                Location = new Point(194, 3),
                Size = new Size(83, 30),
                Orientation = Orientation.Horizontal,
                BorderStyle = BorderStyle.FixedSingle,
                //DrawColor = Color.White,
                //Label = "",
                Maximum = Defs.MAX_GAIN,
                Minimum = Defs.VOLUME_MIN,
                Value = Defs.VOLUME_DEFAULT,
                Resolution = 0.1
            };

            Controls.Add(sldVolume);
            Controls.Add(lblMute);
            Controls.Add(lblSolo);
            Controls.Add(lblChannelInfo);
        }

        /// <summary>
        /// Final UI init.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            lblChannelInfo.Text = $"{_deviceNumber}:{_channelNumber}";
            lblChannelInfo.BackColor = UnselectedColor;

            sldVolume.DrawColor = SelectedColor;
            //sldVolume.ValueChanged += Volume_ValueChanged;
            lblSolo.Click += SoloMute_Click;
            lblMute.Click += SoloMute_Click;

            UpdateUi();

            base.OnLoad(e);
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion


        /// <summary>Handles solo and mute.</summary>
        void SoloMute_Click(object? sender, EventArgs e)  // notify
        {
            var lbl = sender as Label;

            // Figure out state.
            if (sender == lblSolo)
            {
                State = lblSolo.BackColor == SelectedColor ? ChannelState.Normal : ChannelState.Solo;
            }
            else if (sender == lblMute)
            {
                State = lblMute.BackColor == SelectedColor ? ChannelState.Normal : ChannelState.Mute;
            }
            else
            {
                // ?????
            }

            ChannelControlChange?.Invoke(this, new() { State = State, DeviceNumber = _deviceNumber, ChannelNumber = _channelNumber });
        }

        /// <summary>Draw mode checkboxes etc.</summary>
        void UpdateUi()
        {
            lblSolo.BackColor = _state == ChannelState.Solo ? SelectedColor :  UnselectedColor;
            lblMute.BackColor = _state == ChannelState.Mute ? SelectedColor :  UnselectedColor;
        }

        /// <summary>Readable.</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"ChannelControl: DeviceNumber:{_deviceNumber} ChannelNumber:{_channelNumber} State:{State}";
        }
    }
}
