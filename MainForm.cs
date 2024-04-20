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
            ////Size = new Size(_settings.FormGeometry.Width, _settings.FormGeometry.Height);
            ///

            barBar.BackColor = Color.AliceBlue;
            barBar.ProgressColor = Color.Pink;
            barBar.MarkerColor = Color.Black;

            Console.WriteLine("");


            /////// Text control.
            //txtInfo.MatchColors.Add("50", Color.Purple);
            //txtInfo.MatchColors.Add("55", Color.Green);
            //txtInfo.BackColor = Color.Cornsilk;
            //txtInfo.Prompt = ">>> ";


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
            ////

        }

        protected override void OnLoad(EventArgs e)
        {
            //propGrid.ResizeDescriptionArea(6); // This doesn't work in constructor.

            base.OnLoad(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            //SaveSettings();
            base.OnFormClosing(e);
        }

        void Tell(string msg)
        {
            //txtInfo.AppendLine(msg);
        }

        public void MakeIcon(string fn)
        {
            string outfn = fn + ".ico";
            // Read bmp and convert to icon.
            var bmp = (Bitmap)Image.FromFile(fn);
            // Save icon.
            var ico = GraphicsUtils.CreateIcon(bmp);//, 32);
            GraphicsUtils.SaveIcon(ico, outfn);
        }
        
    }
}
