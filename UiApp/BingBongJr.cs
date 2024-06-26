using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using Nebulua.Common;


namespace Nebulua.UiApp
{
    /// <summary>
    /// TODO Simplified version copied from MidiLib because I didn't want to bring in that whole lib.
    /// </summary>
    public class BingBongJr : UserControl
    {
        #region Fields
        /// <summary>Background image data.</summary>
        PixelBitmap? _bmp;

        /// <summary>Tool tip.</summary>
        readonly ToolTip _toolTip = new();

        /// <summary>Last key down.</summary>
        int _lastNote = -1;

        /// <summary>The pen.</summary>
        readonly Pen _pen = new(Color.WhiteSmoke, 1);
        #endregion

        #region Properties
        /// <summary>Lowest midi note of interest. Adjust to taste.</summary>
        public int MinNote { get; set; } = 24;

        /// <summary>Highest midi note of interest. Adjust to taste.</summary>
        public int MaxNote { get; set; } = 95;

        /// <summary>Min control value. For velocity = off.</summary>
        public int MinControl { get; set; } = MidiDefs.MIDI_VAL_MIN;

        /// <summary>Max control value. For velocity = loudest.</summary>
        public int MaxControl { get; set; } = MidiDefs.MIDI_VAL_MAX;

        /// <summary>Visibility.</summary>
        public bool DrawNoteGrid { get; set; } = true;
        #endregion

        #region Events
        /// <summary>Click press info.</summary>
        public event EventHandler<MidiEventArgs>? MidiEvent;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public BingBongJr()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            Name = "BingBong";
            ClientSize = new Size(300, 300);
        }

        /// <summary>
        /// Init after properties set.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            DrawBitmap();
            base.OnLoad(e);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            _bmp?.Dispose();
            _pen.Dispose();
            base.Dispose(disposing);
        }
        #endregion

        #region Event handlers
        /// <summary>
        /// Paint the surface.
        /// </summary>
        /// <param name="pe"></param>
        protected override void OnPaint(PaintEventArgs pe)
        {
            // Background?
            if (_bmp is not null)
            {
                pe.Graphics.DrawImage(_bmp.Bitmap, 0, 0, _bmp.Bitmap.Width, _bmp.Bitmap.Height);
            }

            // Draw grid?
            if (DrawNoteGrid)
            {
                int numNotes = MaxNote - MinNote;
                int pixelsPerNote = Width / numNotes;
                int numGridLines = 10;
                int pixelsPerGridLine = Width / numGridLines;

                for (int gl = 0; gl < numGridLines; gl++)
                {
                    int x = gl * pixelsPerGridLine;
                    pe.Graphics.DrawLine(_pen, x, 0, x, Height);
                }
            }

            base.OnPaint(pe);
        }

        /// <summary>
        /// Show the pixel info.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            var mp = PointToClient(MousePosition);
            var (note, value) = XyToMidi(mp.X, mp.Y);

            if (e.Button == MouseButtons.Left)
            {
                // Dragging. Did it change?
                if (_lastNote != note)
                {
                    if (_lastNote != -1)
                    {
                        // Turn off last note.
                        MidiEvent?.Invoke(this, new() { Note = _lastNote, Velocity = 0 });
                    }

                    // Start the new note.
                    _lastNote = note;
                    MidiEvent?.Invoke(this, new() { Note = note, Velocity = value });
                }
            }

            //toolTip1.SetToolTip(this, $"X:{mp.X} Y:{mp.Y}");

            //Color clr = _result.GetPixel(mp.X, mp.Y);
            //toolTip1.SetToolTip(this, $"X:{mp.X} Y:{mp.Y} C:{clr}");

            //var note = MidiDefs.NoteNumberToName(mp.X);
            //toolTip1.SetToolTip(this, $"{note}({mp.Y})");

            var snote = MusicDefinitions.NoteNumberToName(note);
            _toolTip.SetToolTip(this, $"{snote} {note} {value}");

            base.OnMouseMove(e);
        }

        /// <summary>
        /// Send info to client.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            var mp = PointToClient(MousePosition);
            var (note, value) = XyToMidi(mp.X, mp.Y);
            _lastNote = note;

            MidiEvent?.Invoke(this, new() { Note = note, Velocity = value });

            base.OnMouseDown(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_lastNote != -1)
            {
                MidiEvent?.Invoke(this, new() { Note = _lastNote, Velocity = 0 });
            }
            _lastNote = -1;

            base.OnMouseUp(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnResize(EventArgs e)
        {
            DrawBitmap();
            Invalidate();
            base.OnResize(e);
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Render.
        /// </summary>
        void DrawBitmap()
        {
            // Clean up old.
            _bmp?.Dispose();

            _bmp = new(Width, Height);

            foreach (var y in Enumerable.Range(0, Height))
            {
                foreach (var x in Enumerable.Range(0, Width))
                {
                    _bmp!.SetPixel(x, y, 255, x * 256 / Width, y * 256 / Height, 150);
                }
            }
        }

        /// <summary>
        /// Map function.
        /// </summary>
        /// <param name="x">UI location.</param>
        /// <param name="y">UI location.</param>
        /// <returns>Tuple of note num and vertical value.</returns>
        (int note, int control) XyToMidi(int x, int y)
        {
            int note = MathUtils.Map(x, 0, Width, MinNote, MaxNote);
            int value = MathUtils.Map(y, Height, 0, MinControl, MaxControl);

            return (note, value);
        }
        #endregion
    }

    /// <summary>
    /// Midi (real or sim) has received something. It's up to the client to make sense of it.
    /// Property value of -1 indicates invalid or not pertinent e.g a controller event doesn't have velocity.
    /// </summary>
    public class MidiEventArgs : EventArgs
    {
        /// <summary>The note number to play.</summary>
        public int Note { get; set; } = -1;

        /// <summary>For Note = velocity.</summary>
        public int Velocity { get; set; } = 0;

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"Note:{Note} Velocity:{Velocity}";
        }
    }
}
