using System;
using System.Drawing;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace Ephemera.Nebulua
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            InitializeComponent();


            //Location = new Point(_settings.FormGeometry.X, _settings.FormGeometry.Y);
            //Size = new Size(_settings.FormGeometry.Width, _settings.FormGeometry.Height);

            timeBar.BackColor = Color.AliceBlue;
            timeBar.ProgressColor = Color.Pink;
            timeBar.MarkerColor = Color.Black;

            timeBar.CurrentTimeChanged += TimeBar_CurrentTimeChanged;

            Console.WriteLine("");


            traffic.MatchColors.Add(" SND ", Color.Purple);
            traffic.MatchColors.Add(" RCV ", Color.Green);
            traffic.BackColor = Color.Cornsilk;
            traffic.Prompt = "->";


            /////// Slider.
            //slider1.DrawColor = Color.Orange;
            //slider1.Minimum = 0;
            //slider1.Maximum = 100;
            //slider1.Resolution = 5;
            //slider1.Value = 40;
            //slider1.Label = "|-|-|";
            //slider1.ValueChanged += (_, __) => Tell($"Slider value: {slider1.Value}");


            /////// OptionsEditor and ChoiceSelector
            //optionsEd.AllowEdit = true;
            //optionsEd.BackColor = Color.LightCoral;
            //optionsEd.Options = new() { { "Apple", true }, { "Orange", false }, { "Peach", true }, { "Bird", false }, { "Cow", true } };
            //optionsEd.OptionsChanged += (_, __) => Tell($"Options changed:{optionsEd.Options.Where(o => o.Value == true).Count()}");
            //choicer.Text = "Test choice";
            //choicer.SetOptions(new() { "Apple", "Orange", "Peach", "Bird", "Cow" });
            //choicer.ChoiceChanged += (_, __) => Tell($"Choicer changed:{choicer.SelectedChoice}");
            //btnDump.Click += (_, __) =>
            //{
            //    Tell($"ChoiceSelector: {choicer.SelectedChoice}");
            //    Tell($"OptionsEditor:");
            //    optionsEd.Options.ForEach(v => Tell($"{v.Key} is {v.Value}"));
            //};

            //slider1.ValueChanged += (_, __) => meterLinear.AddValue(slider1.Value);
            //// sl2 2 -> 19
            //slider2.ValueChanged += (_, __) => meterDots.AddValue(slider2.Value);



            // These set vars only
            // this.sldVolume.ValueChanged += new System.EventHandler(this.Volume_ValueChanged);
            // btnMonIn.Click += Monitor_Click;
            // btnMonOut.Click += Monitor_Click;
            // 
            // this.sldTempo.ValueChanged += new System.EventHandler(this.Speed_ValueChanged);
            // > SetFastTimerPeriod();
            // 
            // ProcessPlay(PlayCommand cmd):
            // this.btnRewind.Click += new System.EventHandler(this.Rewind_Click);  
            // this.chkPlay.Click += new System.EventHandler(this.Play_Click);  ProcessPlay(PlayCommand cmd)
            // void BarBar_CurrentTimeChanged(object? sender, EventArgs e)
            // 
            // 
            // 
            // btnAbout.Click += About_Click;
            // > MiscUtils.ShowReadme("Nebulator");
            // this.showDefinitionsToolStripMenuItem.Click += new System.EventHandler(this.ShowDefinitions_Click);
            // > var docs = MidiDefs.FormatDoc();
            // > docs.AddRange(MusicDefinitions.FormatDoc());
            // > Tools.MarkdownToHtml(docs, Color.LightYellow, new Font("arial", 16), true);
            // 
            // btnKillComm.Click += (object? _, EventArgs __) => { KillAll(); };


        }

        private void TimeBar_CurrentTimeChanged(object? sender, EventArgs e)
        {
//            throw new NotImplementedException();
        }

        //protected override void OnLoad(EventArgs e)
        //{


        //    base.OnLoad(e);
        //}

        //protected override void OnFormClosing(FormClosingEventArgs e)
        //{
        //    //SaveSettings();
        //    base.OnFormClosing(e);
        //}

        //void Tell(string msg)
        //{
        //    //txtInfo.AppendLine(msg);
        //}

        //public void MakeIcon(string fn)
        //{
        //    string outfn = fn + ".ico";
        //    // Read bmp and convert to icon.
        //    var bmp = (Bitmap)Image.FromFile(fn);
        //    // Save icon.
        //    var ico = GraphicsUtils.CreateIcon(bmp);//, 32);
        //    GraphicsUtils.SaveIcon(ico, outfn);
        //}
    }
}
