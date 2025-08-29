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
    public class ChannelControlEventArgs(ChannelSpec channelSpec, ChannelState state) : EventArgs
    {
        public ChannelState State { get; init; } = state;
        public ChannelSpec ChannelSpec { get; init; } = channelSpec;
    }

    /// <summary>Channel events and other properties.</summary>
    public class ChannelControl : UserControl
    {
        #region Fields
        readonly Container components = new();

        readonly ChannelSpec _channelSpec;
        ChannelState _state = ChannelState.Normal;

        readonly Color _selColor = Color.Blue;
        readonly Color _unselColor = Color.Red;

        readonly Label lblChannelInfo;
        readonly Label lblSolo;
        readonly Label lblMute;
        readonly Slider sldVolume;

        readonly double _volume = 0.8;
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
        public double Volume
        {
            get { return sldVolume.Value; }
            set { sldVolume.Value = value; }
        }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="channelNumber"></param>
        public ChannelControl(ChannelSpec chspec) : this()
        {
            _channelSpec = chspec;
            _selColor = Common.Settings.SelectedColor;
            _unselColor = Common.Settings.BackColor;
            lblChannelInfo.BackColor = _unselColor;
            lblSolo.BackColor = _unselColor;
            lblMute.BackColor = _unselColor;
            sldVolume.BackColor = _unselColor;
            sldVolume.ForeColor = Common.Settings.ActiveColor;
        }

        /// <summary>
        /// Designer constructor.
        /// </summary>
        public ChannelControl()
        {
            lblChannelInfo = new()
            {
                Location = new Point(2, 9),
                Size = new Size(35, 20),
                Text = "#"
            };

            lblSolo = new()
            {
                Location = new Point(lblChannelInfo.Right + 4, 9),
                Size = new Size(20, 20),
                Text = "S"
            };

            lblMute = new()
            {
                Location = new Point(lblSolo.Right + 4, 9),
                Size = new Size(20, 20),
                Text = "M"
            };

            sldVolume = new()
            {
                Location = new Point(lblMute.Right + 4, 4),
                Size = new Size(40, 30),
                Orientation = Orientation.Horizontal,
                BorderStyle = BorderStyle.FixedSingle,
                Maximum = Common.MAX_GAIN,
                Minimum = Common.VOLUME_MIN,
                Value = Common.VOLUME_DEFAULT,
                Resolution = 0.1
            };

            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Size = new Size(sldVolume.Right + 5, 38);
            BorderStyle = BorderStyle.FixedSingle;

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
            lblChannelInfo.Text = $"{_channelSpec.DeviceId}:{_channelSpec.ChannelNumber}";
            lblChannelInfo.BackColor = _unselColor;

            sldVolume.DrawColor = Common.Settings.ActiveColor;
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
        void SoloMute_Click(object? sender, EventArgs e)
        {
            var lbl = sender as Label;

            // Figure out state.
            if (sender == lblSolo)
            {
                State = lblSolo.BackColor == _selColor ? ChannelState.Normal : ChannelState.Solo;
            }
            else if (sender == lblMute)
            {
                State = lblMute.BackColor == _selColor ? ChannelState.Normal : ChannelState.Mute;
            }
            else
            {
                // ?????
            }

            ChannelControlChange?.Invoke(this, new ChannelControlEventArgs(_channelSpec, State));
        }

        /// <summary>Draw mode checkboxes etc.</summary>
        void UpdateUi()
        {
            lblSolo.BackColor = _state == ChannelState.Solo ? _selColor :  _unselColor;
            lblMute.BackColor = _state == ChannelState.Mute ? _selColor :  _unselColor;
        }
    }
}
