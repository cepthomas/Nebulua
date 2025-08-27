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
    /// <summary>Channel state.</summary>
    public enum ChannelState { Normal = 0, Solo = 1, Mute = 2 }

    /// <summary>Notify host of asynchronous changes from user.</summary>
    public class ChannelChangeEventArgs : EventArgs
    {
        public bool StateChange { get; set; } = false;
        public int ChannelNumber { get; set; } = 0;
    }

    /// <summary>Channel events and other properties.</summary>
    public class ChannelControl : UserControl
    {
        #region Fields
        /// <summary>Required designer variable.</summary>
        IContainer components = new Container();
        #endregion

        #region Events
        /// <summary>Notify host of asynchronous changes from user.</summary>
        public event EventHandler<ChannelChangeEventArgs>? ChannelChange;
        #endregion

        #region Properties
        /// <summary>Actual 1-based midi channel number for UI.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public int ChannelNumber { get; set; }

        /// <summary>For muting/soloing.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public ChannelState State { get; set; } // UpdateUi();

        /// <summary>Current volume. Channel.Volume performs the constraints.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public double Volume { get; set; } // limit

        /// <summary>User has selected this channel.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public bool Selected { get; set; } // UpdateUi();

        /// <summary>Indicate user selected.</summary>
        public Color SelectedColor { get; set; } = Color.Aquamarine;

        /// <summary>Indicate user not selected.</summary>
        public Color UnselectedColor { get; set; } = DefaultBackColor;
        #endregion

        #region Lifecycle
        /// <summary>Normal constructor.</summary>
        public ChannelControl()
        {
            InitializeComponent();
            sldVolume.ValueChanged += Volume_ValueChanged;
            lblSolo.Click += SoloMute_Click;
            lblMute.Click += SoloMute_Click;
        }

        /// <summary> </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            sldVolume.Value = Defs.VOLUME_DEFAULT;
            sldVolume.DrawColor = SelectedColor;
            sldVolume.Minimum = Defs.VOLUME_MIN;
            sldVolume.Maximum = Defs.MAX_GAIN;
            UpdateUi();
            base.OnLoad(e);
        }
        #endregion

        #region Handlers for user selections
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Volume_ValueChanged(object? sender, EventArgs e)  // notify
        {
            Volume = (sender as Slider)!.Value;

            //if (sender is not null)
            //{
            //    Volume = (sender as Slider)!.Value;
            //}
        }

        /// <summary>Handles solo and mute.</summary>
        void SoloMute_Click(object? sender, EventArgs e)  // notify
        {
            var lbl = sender as Label;

            // Figure out state.
            ChannelState newState = ChannelState.Normal; // default

            // Toggle control. Get current.
            bool soloSel = lblSolo.BackColor == SelectedColor;
            bool muteSel = lblMute.BackColor == SelectedColor;

            if (lbl == lblSolo)
            {
                if (soloSel) // unselect
                {
                    if (muteSel)
                    {
                        newState = ChannelState.Mute;
                    }
                }
                else // select
                {
                    newState = ChannelState.Solo;
                }
            }
            else // lblMute
            {
                if (muteSel) // unselect
                {
                    if (soloSel)
                    {
                        newState = ChannelState.Solo;
                    }
                }
                else // select
                {
                    newState = ChannelState.Mute;
                }
            }

            if (newState != State)
            {
                State = newState;
                UpdateUi();
                ChannelChange?.Invoke(this, new() { StateChange = true });
            }
        }

        /// <summary>Draw mode checkboxes etc.</summary>
        void UpdateUi()
        {
            // Solo/mute state.
            switch (State)
            {
                case ChannelState.Normal:
                    lblSolo.BackColor = UnselectedColor;
                    lblMute.BackColor = UnselectedColor;
                    break;
                case ChannelState.Solo:
                    lblSolo.BackColor = SelectedColor;
                    lblMute.BackColor = UnselectedColor;
                    break;
                case ChannelState.Mute:
                    lblSolo.BackColor = UnselectedColor;
                    lblMute.BackColor = SelectedColor;
                    break;
            }

            // General.
            lblChannelNumber.Text = $"Ch{ChannelNumber}";
            lblChannelNumber.BackColor = Selected ? SelectedColor : UnselectedColor;
        }

        /// <summary>Readable.</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"ChannelControl: ChannelNumber:{ChannelNumber} State:{State}";
        }
        #endregion


        ///////////////////////////////////////////////////////////////////////////////////
        Label lblChannelNumber = new();
        Label lblSolo = new();
        Label lblMute = new();
        Slider sldVolume = new();

        /// <summary>Designer support.</summary>
        void InitializeComponent()
        {
            SuspendLayout();

            // 
            // lblChannelNumber
            // 
            lblChannelNumber.AutoSize = true;
            lblChannelNumber.Location = new Point(2, 8);
            lblChannelNumber.Name = "lblChannelNumber";
            lblChannelNumber.Size = new Size(18, 20);
            lblChannelNumber.TabIndex = 3;
            lblChannelNumber.Text = "#";

            // 
            // lblSolo
            // 
            lblSolo.Location = new Point(290, 7);
            lblSolo.Name = "lblSolo";
            lblSolo.Size = new Size(20, 20);
            lblSolo.TabIndex = 45;
            lblSolo.Text = "S";

            // 
            // lblMute
            // 
            lblMute.Location = new Point(315, 7);
            lblMute.Name = "lblMute";
            lblMute.Size = new Size(20, 20);
            lblMute.TabIndex = 46;
            lblMute.Text = "M";

            // 
            // sldVolume
            // 
            sldVolume.BorderStyle = BorderStyle.FixedSingle;
            sldVolume.DrawColor = Color.White;
            sldVolume.Label = "";
            sldVolume.Location = new Point(194, 3);
            sldVolume.Maximum = 10D;
            sldVolume.Minimum = 0D;
            sldVolume.Name = "sldVolume";
            sldVolume.Orientation = Orientation.Horizontal;
            sldVolume.Resolution = 0.1D;
            sldVolume.Size = new Size(83, 30);
            sldVolume.TabIndex = 47;
            sldVolume.Value = 5D;

            // 
            // ChannelControl
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(sldVolume);
            Controls.Add(lblMute);
            Controls.Add(lblSolo);
            //Controls.Add(lblPatch);
            Controls.Add(lblChannelNumber);
            Name = "ChannelControl";
            Size = new Size(345, 38);
            ResumeLayout(false);
            PerformLayout();
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


    }
}
