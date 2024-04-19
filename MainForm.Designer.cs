using System.Drawing;
using System.Windows.Forms;

namespace Nebulua
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            chkPlay = new CheckBox();
            btnRewind = new Button();
            sldVolume = new Ephemera.NBagOfUis.Slider();
            sldTempo = new Ephemera.NBagOfUis.Slider();
            terminal = new Ephemera.NBagOfUis.TextViewer();
            toolTip = new ToolTip(components);
            SuspendLayout();
            // 
            // chkPlay
            // 
            chkPlay.Appearance = Appearance.Button;
            chkPlay.Image = Properties.Resources.glyphicons_174_play;
            chkPlay.Location = new Point(189, 46);
            chkPlay.Name = "chkPlay";
            chkPlay.Size = new Size(43, 49);
            chkPlay.TabIndex = 0;
            chkPlay.UseVisualStyleBackColor = false;
            this.toolTip.SetToolTip(this.chkPlay, "Play project");
            // 
            // btnRewind
            // 
            btnRewind.Image = Properties.Resources.glyphicons_173_rewind;
            btnRewind.Location = new Point(52, 44);
            btnRewind.Name = "btnRewind";
            btnRewind.Size = new Size(45, 49);
            btnRewind.TabIndex = 1;
            btnRewind.UseVisualStyleBackColor = false;
            this.toolTip.SetToolTip(this.btnRewind, "Reset to start");
            // 
            // sldVolume
            // 
            sldVolume.BorderStyle = BorderStyle.FixedSingle;
            sldVolume.DrawColor = Color.Orange;
            sldVolume.Label = "vol";
            sldVolume.Location = new Point(630, 49);
            sldVolume.Margin = new Padding(4, 5, 4, 5);
            sldVolume.Maximum = 1D;
            sldVolume.Minimum = 0D;
            sldVolume.Name = "sldVolume";
            sldVolume.Orientation = Orientation.Horizontal;
            sldVolume.Resolution = 0.05D;
            sldVolume.Size = new Size(88, 52);
            sldVolume.TabIndex = 36;
            sldVolume.Value = 1D;
            this.toolTip.SetToolTip(this.sldVolume, "Master volume");
            // 
            // sldTempo
            // 
            sldTempo.BorderStyle = BorderStyle.FixedSingle;
            sldTempo.DrawColor = Color.IndianRed;
            sldTempo.Label = "bpm";
            sldTempo.Location = new Point(428, 43);
            sldTempo.Margin = new Padding(5, 6, 5, 6);
            sldTempo.Maximum = 240D;
            sldTempo.Minimum = 60D;
            sldTempo.Name = "sldTempo";
            sldTempo.Orientation = Orientation.Horizontal;
            sldTempo.Resolution = 5D;
            sldTempo.Size = new Size(88, 52);
            sldTempo.TabIndex = 33;
            sldTempo.Value = 100D;
            this.toolTip.SetToolTip(this.sldTempo, "Speed in BPM");
            // 
            // terminal
            // 
            terminal.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            terminal.BorderStyle = BorderStyle.FixedSingle;
            terminal.Location = new Point(69, 277);
            terminal.Margin = new Padding(4, 5, 4, 5);
            terminal.MaxText = 5000;
            terminal.Name = "terminal";
            terminal.Prompt = "";
            terminal.Size = new Size(789, 217);
            terminal.TabIndex = 41;
            terminal.WordWrap = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1041, 606);
            Controls.Add(btnRewind);
            Controls.Add(chkPlay);
            Controls.Add(sldVolume);
            Controls.Add(sldTempo);
            Controls.Add(terminal);
            Name = "MainForm";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private CheckBox chkPlay;
        private Button btnRewind;
        private Ephemera.NBagOfUis.Slider sldVolume;
        private Ephemera.NBagOfUis.Slider sldTempo;
        private Ephemera.NBagOfUis.TextViewer terminal;
        private ToolTip toolTip;
    }
}
