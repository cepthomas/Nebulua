using System.Drawing;
using System.Windows.Forms;

namespace Ephemera.Nebulua.UiApp
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            chkPlay = new CheckBox();
            btnRewind = new Button();
            sldVolume = new NBagOfUis.Slider();
            sldTempo = new NBagOfUis.Slider();
            traffic = new NBagOfUis.TextViewer();
            toolTip = new ToolTip(components);
            btnAbout = new Button();
            btnMonRcv = new Button();
            btnMonSnd = new Button();
            btnKill = new Button();
            timeBar = new TimeBar();
            cliIn = new CliInput();
            SuspendLayout();
            // 
            // chkPlay
            // 
            chkPlay.Appearance = Appearance.Button;
            chkPlay.Image = (Image)resources.GetObject("chkPlay.Image");
            chkPlay.Location = new Point(187, 12);
            chkPlay.Name = "chkPlay";
            chkPlay.Size = new Size(43, 49);
            chkPlay.TabIndex = 0;
            toolTip.SetToolTip(chkPlay, "Play project");
            chkPlay.UseVisualStyleBackColor = false;
            // 
            // btnRewind
            // 
            btnRewind.Image = (Image)resources.GetObject("btnRewind.Image");
            btnRewind.Location = new Point(50, 10);
            btnRewind.Name = "btnRewind";
            btnRewind.Size = new Size(45, 49);
            btnRewind.TabIndex = 1;
            toolTip.SetToolTip(btnRewind, "Reset to start");
            btnRewind.UseVisualStyleBackColor = false;
            // 
            // sldVolume
            // 
            sldVolume.BorderStyle = BorderStyle.FixedSingle;
            sldVolume.DrawColor = Color.Orange;
            sldVolume.Label = "vol";
            sldVolume.Location = new Point(444, 12);
            sldVolume.Margin = new Padding(4, 5, 4, 5);
            sldVolume.Maximum = 1D;
            sldVolume.Minimum = 0D;
            sldVolume.Name = "sldVolume";
            sldVolume.Orientation = Orientation.Horizontal;
            sldVolume.Resolution = 0.05D;
            sldVolume.Size = new Size(88, 52);
            sldVolume.TabIndex = 36;
            toolTip.SetToolTip(sldVolume, "Master volume");
            sldVolume.Value = 1D;
            // 
            // sldTempo
            // 
            sldTempo.BorderStyle = BorderStyle.FixedSingle;
            sldTempo.DrawColor = Color.IndianRed;
            sldTempo.Label = "bpm";
            sldTempo.Location = new Point(302, 12);
            sldTempo.Margin = new Padding(5, 6, 5, 6);
            sldTempo.Maximum = 240D;
            sldTempo.Minimum = 60D;
            sldTempo.Name = "sldTempo";
            sldTempo.Orientation = Orientation.Horizontal;
            sldTempo.Resolution = 5D;
            sldTempo.Size = new Size(88, 52);
            sldTempo.TabIndex = 33;
            toolTip.SetToolTip(sldTempo, "Speed in BPM");
            sldTempo.Value = 100D;
            // 
            // traffic
            // 
            traffic.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            traffic.BorderStyle = BorderStyle.FixedSingle;
            traffic.Location = new Point(36, 169);
            traffic.Margin = new Padding(4, 5, 4, 5);
            traffic.MaxText = 5000;
            traffic.Name = "traffic";
            traffic.Prompt = "";
            traffic.Size = new Size(789, 217);
            traffic.TabIndex = 41;
            traffic.WordWrap = true;
            // 
            // btnAbout
            // 
            btnAbout.Image = Properties.Resources.glyphicons_195_question_sign;
            btnAbout.Location = new Point(844, 18);
            btnAbout.Name = "btnAbout";
            btnAbout.Size = new Size(58, 46);
            btnAbout.TabIndex = 44;
            toolTip.SetToolTip(btnAbout, "Tell me about yourself");
            btnAbout.UseVisualStyleBackColor = false;
            // 
            // btnMonRcv
            // 
            btnMonRcv.Image = Properties.Resources.glyphicons_213_arrow_down;
            btnMonRcv.Location = new Point(632, 19);
            btnMonRcv.Name = "btnMonRcv";
            btnMonRcv.Size = new Size(59, 29);
            btnMonRcv.TabIndex = 45;
            toolTip.SetToolTip(btnMonRcv, "Monitor midi rcv");
            btnMonRcv.UseVisualStyleBackColor = false;
            // 
            // btnMonSnd
            // 
            btnMonSnd.Image = Properties.Resources.glyphicons_214_arrow_up;
            btnMonSnd.Location = new Point(722, 27);
            btnMonSnd.Name = "btnMonSnd";
            btnMonSnd.Size = new Size(59, 29);
            btnMonSnd.TabIndex = 46;
            toolTip.SetToolTip(btnMonSnd, "Monitor midi send");
            btnMonSnd.UseVisualStyleBackColor = false;
            // 
            // btnKill
            // 
            btnKill.Image = Properties.Resources.glyphicons_206_electricity;
            btnKill.Location = new Point(795, 19);
            btnKill.Name = "btnKill";
            btnKill.Size = new Size(43, 43);
            btnKill.TabIndex = 47;
            toolTip.SetToolTip(btnKill, "Kill all midi out");
            btnKill.UseVisualStyleBackColor = false;
            // 
            // timeBar
            // 
            timeBar.BorderStyle = BorderStyle.FixedSingle;
            timeBar.Font = new Font("Cascadia Mono", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            timeBar.FontLarge = new Font("Microsoft Sans Serif", 20F, FontStyle.Regular, GraphicsUnit.Point, 0);
            timeBar.FontSmall = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            timeBar.Location = new Point(50, 82);
            timeBar.MarkerColor = Color.Black;
            timeBar.Name = "timeBar";
            timeBar.ProgressColor = Color.White;
            timeBar.Size = new Size(731, 64);
            timeBar.Snap = SnapType.Bar;
            timeBar.TabIndex = 42;
            // 
            // cliIn
            // 
            cliIn.Location = new Point(36, 418);
            cliIn.Name = "cliIn";
            cliIn.Prompt = "???";
            cliIn.Size = new Size(773, 125);
            cliIn.TabIndex = 43;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1041, 606);
            Controls.Add(btnKill);
            Controls.Add(btnMonSnd);
            Controls.Add(btnMonRcv);
            Controls.Add(btnAbout);
            Controls.Add(cliIn);
            Controls.Add(timeBar);
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
        private ToolTip toolTip;
        private TimeBar timeBar;
        private CliInput cliIn;
        private Button btnAbout;
        private Button btnMonRcv;
        private Button btnMonSnd;
        private Button btnKill;
    }
}
