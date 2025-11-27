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
            traffic = new Ephemera.NBagOfUis.TextViewer();
            btnAbout = new Button();
            btnKill = new Button();
            chkMonRcv = new CheckBox();
            chkMonSnd = new CheckBox();
            chkLoop = new CheckBox();
            btnSettings = new Button();
            timeBar = new TimeBar();
            ddbtnFile = new Ephemera.NBagOfUis.DropDownButton();
            toolTip = new ToolTip(components);
            SuspendLayout();
            // 
            // chkPlay
            // 
            chkPlay.Appearance = Appearance.Button;
            chkPlay.FlatAppearance.CheckedBackColor = Color.PapayaWhip;
            chkPlay.FlatStyle = FlatStyle.Flat;
            chkPlay.Image = Properties.Resources.glyphicons_174_play;
            chkPlay.Location = new Point(64, 9);
            chkPlay.Name = "chkPlay";
            chkPlay.Size = new Size(40, 38);
            chkPlay.TabIndex = 0;
            toolTip.SetToolTip(chkPlay, "Play toggle");
            chkPlay.UseVisualStyleBackColor = false;
            // 
            // btnRewind
            // 
            btnRewind.FlatStyle = FlatStyle.Flat;
            btnRewind.Image = Properties.Resources.glyphicons_173_rewind;
            btnRewind.Location = new Point(13, 8);
            btnRewind.Name = "btnRewind";
            btnRewind.Size = new Size(40, 38);
            btnRewind.TabIndex = 1;
            toolTip.SetToolTip(btnRewind, "Rewind");
            btnRewind.UseVisualStyleBackColor = false;
            // 
            // sldVolume
            // 
            sldVolume.BorderStyle = BorderStyle.FixedSingle;
            sldVolume.DrawColor = Color.Red;
            sldVolume.Label = "vol";
            sldVolume.Location = new Point(348, 8);
            sldVolume.Margin = new Padding(4, 5, 4, 5);
            sldVolume.Maximum = 1D;
            sldVolume.Minimum = 0D;
            sldVolume.Name = "sldVolume";
            sldVolume.Orientation = Orientation.Horizontal;
            sldVolume.Resolution = 0.05D;
            sldVolume.Size = new Size(88, 40);
            sldVolume.TabIndex = 36;
            sldVolume.Value = 0.8D;
            // 
            // sldTempo
            // 
            sldTempo.BorderStyle = BorderStyle.FixedSingle;
            sldTempo.DrawColor = Color.IndianRed;
            sldTempo.Label = "bpm";
            sldTempo.Location = new Point(238, 8);
            sldTempo.Margin = new Padding(5, 6, 5, 6);
            sldTempo.Maximum = 240D;
            sldTempo.Minimum = 60D;
            sldTempo.Name = "sldTempo";
            sldTempo.Orientation = Orientation.Horizontal;
            sldTempo.Resolution = 1D;
            sldTempo.Size = new Size(88, 40);
            sldTempo.TabIndex = 33;
            sldTempo.Value = 100D;
            // 
            // traffic
            // 
            traffic.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            traffic.BorderStyle = BorderStyle.FixedSingle;
            traffic.Location = new Point(13, 177);
            traffic.Margin = new Padding(4, 5, 4, 5);
            traffic.MaxText = 5000;
            traffic.Name = "traffic";
            traffic.Prompt = "";
            traffic.Size = new Size(850, 291);
            traffic.TabIndex = 41;
            traffic.WordWrap = true;
            // 
            // btnAbout
            // 
            btnAbout.FlatStyle = FlatStyle.Flat;
            btnAbout.Image = Properties.Resources.glyphicons_195_question_sign;
            btnAbout.Location = new Point(642, 10);
            btnAbout.Name = "btnAbout";
            btnAbout.Size = new Size(40, 38);
            btnAbout.TabIndex = 44;
            toolTip.SetToolTip(btnAbout, "About");
            btnAbout.UseVisualStyleBackColor = false;
            // 
            // btnKill
            // 
            btnKill.FlatStyle = FlatStyle.Flat;
            btnKill.Image = Properties.Resources.glyphicons_206_electricity;
            btnKill.Location = new Point(550, 10);
            btnKill.Name = "btnKill";
            btnKill.Size = new Size(40, 38);
            btnKill.TabIndex = 47;
            toolTip.SetToolTip(btnKill, "Kill all outputs");
            btnKill.UseVisualStyleBackColor = false;
            // 
            // chkMonRcv
            // 
            chkMonRcv.Appearance = Appearance.Button;
            chkMonRcv.FlatAppearance.CheckedBackColor = Color.PapayaWhip;
            chkMonRcv.FlatStyle = FlatStyle.Flat;
            chkMonRcv.Image = Properties.Resources.glyphicons_213_arrow_down;
            chkMonRcv.Location = new Point(458, 10);
            chkMonRcv.Name = "chkMonRcv";
            chkMonRcv.Size = new Size(40, 38);
            chkMonRcv.TabIndex = 48;
            toolTip.SetToolTip(chkMonRcv, "Monitor receive events");
            chkMonRcv.UseVisualStyleBackColor = false;
            // 
            // chkMonSnd
            // 
            chkMonSnd.Appearance = Appearance.Button;
            chkMonSnd.FlatAppearance.CheckedBackColor = Color.PapayaWhip;
            chkMonSnd.FlatStyle = FlatStyle.Flat;
            chkMonSnd.Image = Properties.Resources.glyphicons_214_arrow_up;
            chkMonSnd.Location = new Point(504, 10);
            chkMonSnd.Name = "chkMonSnd";
            chkMonSnd.Size = new Size(40, 38);
            chkMonSnd.TabIndex = 49;
            toolTip.SetToolTip(chkMonSnd, "Monitor send events");
            chkMonSnd.UseVisualStyleBackColor = false;
            // 
            // chkLoop
            // 
            chkLoop.Appearance = Appearance.Button;
            chkLoop.FlatAppearance.CheckedBackColor = Color.PapayaWhip;
            chkLoop.FlatStyle = FlatStyle.Flat;
            chkLoop.Image = Properties.Resources.glyphicons_82_refresh;
            chkLoop.Location = new Point(180, 8);
            chkLoop.Name = "chkLoop";
            chkLoop.Size = new Size(40, 38);
            chkLoop.TabIndex = 50;
            toolTip.SetToolTip(chkLoop, "Enable looping");
            chkLoop.UseVisualStyleBackColor = false;
            // 
            // btnSettings
            // 
            btnSettings.FlatStyle = FlatStyle.Flat;
            btnSettings.Image = Properties.Resources.glyphicons_137_cogwheel;
            btnSettings.Location = new Point(596, 10);
            btnSettings.Name = "btnSettings";
            btnSettings.Size = new Size(40, 38);
            btnSettings.TabIndex = 55;
            toolTip.SetToolTip(btnSettings, "User settings");
            btnSettings.UseVisualStyleBackColor = false;
            // 
            // timeBar
            // 
            timeBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            timeBar.BackColor = Color.LightYellow;
            timeBar.BorderStyle = BorderStyle.FixedSingle;
            timeBar.FontLarge = new Font("Microsoft Sans Serif", 20F, FontStyle.Regular, GraphicsUnit.Point, 0);
            timeBar.FontSmall = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            timeBar.Location = new Point(13, 66);
            timeBar.Name = "timeBar";
            timeBar.Size = new Size(850, 49);
            timeBar.Snap = SnapType.Bar;
            timeBar.TabIndex = 52;
            // 
            // ddbtnFile
            // 
            ddbtnFile.FlatStyle = FlatStyle.Flat;
            ddbtnFile.Image = Properties.Resources.glyphicons_37_file;
            ddbtnFile.Location = new Point(124, 8);
            ddbtnFile.Name = "ddbtnFile";
            ddbtnFile.Size = new Size(40, 38);
            ddbtnFile.TabIndex = 57;
            toolTip.SetToolTip(ddbtnFile, "Open new or recent script");
            ddbtnFile.UseVisualStyleBackColor = false;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(880, 473);
            Controls.Add(ddbtnFile);
            Controls.Add(btnSettings);
            Controls.Add(timeBar);
            Controls.Add(chkLoop);
            Controls.Add(chkMonSnd);
            Controls.Add(chkMonRcv);
            Controls.Add(btnKill);
            Controls.Add(btnAbout);
            Controls.Add(btnRewind);
            Controls.Add(chkPlay);
            Controls.Add(sldVolume);
            Controls.Add(sldTempo);
            Controls.Add(traffic);
            Name = "MainForm";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private CheckBox chkPlay;
        private Button btnRewind;
        private Ephemera.NBagOfUis.Slider sldVolume;
        private Ephemera.NBagOfUis.Slider sldTempo;
        private Ephemera.NBagOfUis.TextViewer traffic;
        private Button btnAbout;
        private Button btnKill;
        private CheckBox chkMonRcv;
        private CheckBox chkMonSnd;
        private CheckBox chkLoop;
        private TimeBar timeBar;
        private Button btnSettings;
        private Ephemera.NBagOfUis.DropDownButton ddbtnFile;
        private ToolTip toolTip;
    }
}
