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
    public class ChannelControlEventArgs() : EventArgs;

    /// <summary>Channel events and other properties.</summary>
    public class ChannelControl : UserControl
    {
        #region Fields
        readonly Container components = new();

        ChannelState _state = ChannelState.Normal;

        readonly Color _selColor = Color.Blue;
        readonly Color _unselColor = Color.Red;

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
        public ChannelSpec Spec { get; init; }

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
            Spec = chspec;
            // Colors.
            _selColor = UserSettings.Current.SelectedColor;
            _unselColor = UserSettings.Current.BackColor;
            lblChannelInfo.BackColor = _unselColor;
            lblSolo.BackColor = _unselColor;
            lblMute.BackColor = _unselColor;
            sldVolume.BackColor = _unselColor;
            sldVolume.ForeColor = UserSettings.Current.ActiveColor;

            lblChannelInfo.Click += LblChannelInfo_Click;
        }

        void LblChannelInfo_Click(object? sender, EventArgs e)
        {
            List<string> info = [];
            info.Add("TODO");// Spec.DeviceName);
            info.Add("TODO");// $"patch: {ChannelSpec.Patch}");
            MessageBox.Show(string.Join(Environment.NewLine, info));
        }

        /// <summary>
        /// Designer constructor.
        /// </summary>
        public ChannelControl()
        {
            // Dummy to keep the designer happy.
            Spec = new(ChannelDirection.Input, -1, -1);

            lblChannelInfo = new()
            {
                Location = new Point(2, 9),
                Size = new Size(40, 20),
                //AutoSize = false,
                Text = "?"
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
            //lblChannelInfo.BorderStyle = BorderStyle.FixedSingle;
            //lblSolo.BorderStyle = BorderStyle.FixedSingle;
            //lblMute.BorderStyle = BorderStyle.FixedSingle;

            lblChannelInfo.Text = $"{Spec.DeviceId}:{Spec.ChannelNumber}";
            lblChannelInfo.BackColor = _unselColor;

            sldVolume.DrawColor = UserSettings.Current.ActiveColor;
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

            ChannelControlChange?.Invoke(this, new ChannelControlEventArgs());
        }

        /// <summary>Draw mode checkboxes etc.</summary>
        void UpdateUi()
        {
            lblSolo.BackColor = _state == ChannelState.Solo ? _selColor :  _unselColor;
            lblMute.BackColor = _state == ChannelState.Mute ? _selColor :  _unselColor;
        }
    }
}
