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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
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
            btnReload = new Button();
            btnSettings = new Button();
            timeBar = new TimeBar();
            lblState = new Label();
            lblInfo = new Label();
            ccMidiGen = new Ephemera.NBagOfUis.ClickClack();
            SuspendLayout();
            // 
            // chkPlay
            // 
            chkPlay.Appearance = Appearance.Button;
            chkPlay.FlatStyle = FlatStyle.Flat;
            chkPlay.Image = (Image)resources.GetObject("chkPlay.Image");
            chkPlay.Location = new Point(64, 9);
            chkPlay.Name = "chkPlay";
            chkPlay.Size = new Size(40, 40);
            chkPlay.TabIndex = 0;
            chkPlay.UseVisualStyleBackColor = false;
            // 
            // btnRewind
            // 
            btnRewind.FlatStyle = FlatStyle.Flat;
            btnRewind.Image = (Image)resources.GetObject("btnRewind.Image");
            btnRewind.Location = new Point(13, 8);
            btnRewind.Name = "btnRewind";
            btnRewind.Size = new Size(40, 40);
            btnRewind.TabIndex = 1;
            btnRewind.UseVisualStyleBackColor = false;
            // 
            // sldVolume
            // 
            sldVolume.BorderStyle = BorderStyle.FixedSingle;
            sldVolume.DrawColor = Color.Orange;
            sldVolume.Label = "vol";
            sldVolume.Location = new Point(348, 8);
            sldVolume.Margin = new Padding(4, 5, 4, 5);
            sldVolume.Maximum = 1D;
            sldVolume.Minimum = 0D;
            sldVolume.Name = "sldVolume";
            sldVolume.Orientation = Orientation.Horizontal;
            sldVolume.Resolution = 0.05D;
            sldVolume.Size = new Size(88, 52);
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
            sldTempo.Size = new Size(88, 52);
            sldTempo.TabIndex = 33;
            sldTempo.Value = 100D;
            // 
            // traffic
            // 
            traffic.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            traffic.BorderStyle = BorderStyle.FixedSingle;
            traffic.Location = new Point(13, 137);
            traffic.Margin = new Padding(4, 5, 4, 5);
            traffic.MaxText = 5000;
            traffic.Name = "traffic";
            traffic.Prompt = "";
            traffic.Size = new Size(835, 188);
            traffic.TabIndex = 41;
            traffic.WordWrap = true;
            // 
            // btnAbout
            // 
            btnAbout.FlatStyle = FlatStyle.Flat;
            btnAbout.Image = (Image)resources.GetObject("btnAbout.Image");
            btnAbout.Location = new Point(642, 10);
            btnAbout.Name = "btnAbout";
            btnAbout.Size = new Size(40, 40);
            btnAbout.TabIndex = 44;
            btnAbout.UseVisualStyleBackColor = false;
            // 
            // btnKill
            // 
            btnKill.FlatStyle = FlatStyle.Flat;
            btnKill.Image = (Image)resources.GetObject("btnKill.Image");
            btnKill.Location = new Point(550, 10);
            btnKill.Name = "btnKill";
            btnKill.Size = new Size(40, 40);
            btnKill.TabIndex = 47;
            btnKill.UseVisualStyleBackColor = false;
            // 
            // chkMonRcv
            // 
            chkMonRcv.Appearance = Appearance.Button;
            chkMonRcv.FlatStyle = FlatStyle.Flat;
            chkMonRcv.Image = (Image)resources.GetObject("chkMonRcv.Image");
            chkMonRcv.Location = new Point(458, 10);
            chkMonRcv.Name = "chkMonRcv";
            chkMonRcv.Size = new Size(40, 40);
            chkMonRcv.TabIndex = 48;
            chkMonRcv.UseVisualStyleBackColor = false;
            // 
            // chkMonSnd
            // 
            chkMonSnd.Appearance = Appearance.Button;
            chkMonSnd.FlatStyle = FlatStyle.Flat;
            chkMonSnd.Image = (Image)resources.GetObject("chkMonSnd.Image");
            chkMonSnd.Location = new Point(504, 10);
            chkMonSnd.Name = "chkMonSnd";
            chkMonSnd.Size = new Size(40, 40);
            chkMonSnd.TabIndex = 49;
            chkMonSnd.UseVisualStyleBackColor = false;
            // 
            // chkLoop
            // 
            chkLoop.Appearance = Appearance.Button;
            chkLoop.FlatStyle = FlatStyle.Flat;
            chkLoop.Image = (Image)resources.GetObject("chkLoop.Image");
            chkLoop.Location = new Point(180, 8);
            chkLoop.Name = "chkLoop";
            chkLoop.Size = new Size(40, 40);
            chkLoop.TabIndex = 50;
            chkLoop.UseVisualStyleBackColor = false;
            // 
            // btnReload
            // 
            btnReload.FlatStyle = FlatStyle.Flat;
            btnReload.Image = (Image)resources.GetObject("btnReload.Image");
            btnReload.Location = new Point(124, 8);
            btnReload.Name = "btnReload";
            btnReload.Size = new Size(40, 40);
            btnReload.TabIndex = 51;
            btnReload.UseVisualStyleBackColor = false;
            // 
            // btnSettings
            // 
            btnSettings.FlatStyle = FlatStyle.Flat;
            btnSettings.Image = (Image)resources.GetObject("btnSettings.Image");
            btnSettings.Location = new Point(596, 10);
            btnSettings.Name = "btnSettings";
            btnSettings.Size = new Size(40, 40);
            btnSettings.TabIndex = 55;
            btnSettings.UseVisualStyleBackColor = false;
            // 
            // timeBar
            // 
            timeBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            timeBar.BorderStyle = BorderStyle.FixedSingle;
            timeBar.FontLarge = new Font("Microsoft Sans Serif", 20F, FontStyle.Regular, GraphicsUnit.Point, 0);
            timeBar.FontSmall = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            timeBar.Location = new Point(13, 69);
            timeBar.MarkerColor = Color.Black;
            timeBar.Name = "timeBar";
            timeBar.ProgressColor = Color.White;
            timeBar.Size = new Size(835, 51);
            timeBar.Snap = SnapType.Bar;
            timeBar.TabIndex = 52;
            // 
            // lblState
            // 
            lblState.AutoSize = true;
            lblState.BorderStyle = BorderStyle.FixedSingle;
            lblState.Location = new Point(717, 10);
            lblState.MinimumSize = new Size(120, 22);
            lblState.Name = "lblState";
            lblState.Size = new Size(120, 22);
            lblState.TabIndex = 53;
            // 
            // lblInfo
            // 
            lblInfo.AutoSize = true;
            lblInfo.BorderStyle = BorderStyle.FixedSingle;
            lblInfo.Location = new Point(717, 38);
            lblInfo.MinimumSize = new Size(120, 22);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new Size(120, 22);
            lblInfo.TabIndex = 54;
            // 
            // ccMidiGen
            // 
            ccMidiGen.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            ccMidiGen.Location = new Point(52, 349);
            ccMidiGen.Size = new Size(375, 176);
            ccMidiGen.TabIndex = 56;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(865, 547);
            Controls.Add(ccMidiGen);
            Controls.Add(btnSettings);
            Controls.Add(lblInfo);
            Controls.Add(lblState);
            Controls.Add(timeBar);
            Controls.Add(btnReload);
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
            PerformLayout();
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
        private Button btnReload;
        private TimeBar timeBar;
        private Label lblState;
        private Label lblInfo;
        private Button btnSettings;
        private Ephemera.NBagOfUis.ClickClack ccMidiGen;
    }
}
