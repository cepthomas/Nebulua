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
        readonly ToolTip toolTip;

        PlayState _state = PlayState.Normal;

        readonly Color _selColor = Color.Blue;
        readonly Color _unselColor = Color.Red;

        readonly Label lblChannelInfo;
        readonly Label lblSolo;
        readonly Label lblMute;
        readonly Slider sldVolume;
        #endregion

        #region Events
        /// <summary>Notify host of user changes.</summary>
        public event EventHandler<ChannelControlEventArgs>? ChannelControlEvent;
        #endregion

        #region Properties
        /// <summary>Handle.</summary>
        public ChannelHandle ChHandle { get; init; }

        /// <summary>For display.</summary>
        public List<string> Info { get; set; } = ["???"];

        /// <summary>For muting/soloing.</summary>
        public PlayState State
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
        public ChannelControl(ChannelHandle ch) : this()
        {
            ChHandle = ch;
            // Colors.
            _selColor = UserSettings.Current.SelectedColor;
            _unselColor = UserSettings.Current.BackColor;
            lblChannelInfo.BackColor = _unselColor;
            lblSolo.BackColor = _unselColor;
            lblMute.BackColor = _unselColor;
            sldVolume.BackColor = _unselColor;
            sldVolume.ForeColor = UserSettings.Current.ActiveColor;

            toolTip.SetToolTip(this, string.Join(Environment.NewLine, Info));
        }

        /// <summary>
        /// Designer constructor.
        /// </summary>
        public ChannelControl()
        {
            // Dummy to keep the designer happy.
            ChHandle = new(-1, -1, Direction.None);

            lblChannelInfo = new()
            {
                Location = new Point(2, 9),
                Size = new Size(40, 20),
                Text = "?"
            };
            Controls.Add(lblChannelInfo);

            lblSolo = new()
            {
                Location = new Point(lblChannelInfo.Right + 4, 9),
                Size = new Size(20, 20),
                Text = "S"
            };
            Controls.Add(lblSolo);

            lblMute = new()
            {
                Location = new Point(lblSolo.Right + 4, 9),
                Size = new Size(20, 20),
                Text = "M"
            };
            Controls.Add(lblMute);

            sldVolume = new()
            {
                Location = new Point(lblMute.Right + 4, 4),
                Size = new Size(40, 30),
                Orientation = Orientation.Horizontal,
                BorderStyle = BorderStyle.FixedSingle,
                Maximum = Defs.MAX_VOLUME,
                Minimum = 0.0,
                Value = Defs.DEFAULT_VOLUME,
                Resolution = 0.05
            };
            Controls.Add(sldVolume);

            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Size = new Size(sldVolume.Right + 5, 38);
            BorderStyle = BorderStyle.FixedSingle;

            toolTip = new(components);
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

            lblChannelInfo.Text = $"{ChHandle.DeviceId}:{ChHandle.ChannelNumber}";
            lblChannelInfo.BackColor = _unselColor;

            toolTip.SetToolTip(lblChannelInfo, string.Join(Environment.NewLine, Info));

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
            if (disposing)
            {
                components?.Dispose();
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
                State = lblSolo.BackColor == _selColor ? PlayState.Normal : PlayState.Solo;
            }
            else if (sender == lblMute)
            {
                State = lblMute.BackColor == _selColor ? PlayState.Normal : PlayState.Mute;
            }
            //else ??

            ChannelControlEvent?.Invoke(this, new ChannelControlEventArgs());
        }

        /// <summary>Draw mode checkboxes etc.</summary>
        void UpdateUi()
        {
            lblSolo.BackColor = _state == PlayState.Solo ? _selColor :  _unselColor;
            lblMute.BackColor = _state == PlayState.Mute ? _selColor :  _unselColor;
        }
    }
}
