using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;


// ? zoom, drag, shift

namespace Nebulua
{
    /// <summary>User selection options.</summary>
    public enum SnapType { Bar, Beat, Sub }

    /// <summary>The control.</summary>
    public class TimeBar : UserControl //TODO make generic? Also MusicTime.cs.
    {
        #region Fields
        /// <summary>For tracking mouse moves.</summary>
        int _lastXPos = 0;

        /// <summary>Tooltip for mousing.</summary>
        readonly ToolTip _toolTip = new();

        /// <summary>For drawing text.</summary>
        readonly StringFormat _format = new() { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };

        /// <summary>For drawing lines etc.</summary>
        readonly Pen _penMarker = new(Color.Red, 1);
        #endregion

        #region Properties
        /// <summary>Big font.</summary>
        public Font FontLarge { get; set; } = new("Microsoft Sans Serif", 20, FontStyle.Regular, GraphicsUnit.Point, 0);

        /// <summary>Baby font.</summary>
        public Font FontSmall { get; set; } = new("Microsoft Sans Serif", 10, FontStyle.Regular, GraphicsUnit.Point, 0);

        /// <summary>For drawing.</summary>
        public Color ControlColor { get; set; } = Color.Red;

        /// <summary>How to select times.</summary>
        public SnapType Snap { get; set; }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public TimeBar()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }

        /// <summary>
        /// Later init.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            _penMarker.Color = ControlColor;
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
                _toolTip.Dispose();
                //_brush.Dispose();
                _penMarker.Dispose();
                _format.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Drawing
        /// <summary>
        /// Draw the control.
        /// </summary>
        protected override void OnPaint(PaintEventArgs pe)
        {
            // Setup.
            pe.Graphics.Clear(BackColor);

            if (State.Instance.IsComposition)
            {
                var vpos = Height / 2;

                // Loop area.
                int lstart = GetClientFromTick(State.Instance.LoopStart);
                int lend = GetClientFromTick(State.Instance.LoopEnd);
                pe.Graphics.DrawLine(_penMarker, lstart, 0, lstart, Height);
                pe.Graphics.DrawLine(_penMarker, lend, 0, lend, Height);
                pe.Graphics.FillPolygon(_penMarker.Brush, new PointF[] { new(lstart, vpos - 5), new(lstart, vpos + 5), new(lstart + 10, vpos) });
                pe.Graphics.FillPolygon(_penMarker.Brush, new PointF[] { new(lend, vpos - 5), new(lend, vpos + 5), new(lend - 10, vpos) });

                // Bars? vert line per bar or ?

                // Sections.
                var fsize = pe.Graphics.MeasureString("X", FontSmall).Height;

                foreach (var (tick, name) in State.Instance.SectionInfo)
                {
                    int sect = GetClientFromTick(tick);
                    pe.Graphics.DrawLine(_penMarker, sect, 0, sect, Height);
                    _format.Alignment = StringAlignment.Center;
                    _format.LineAlignment = StringAlignment.Center;
                    pe.Graphics.DrawString(name, FontSmall, Brushes.Black, sect + 2, Height - fsize - 2);
                }

                // Current pos.
                int cpos = GetClientFromTick(State.Instance.CurrentTick);
                pe.Graphics.DrawLine(_penMarker, cpos, 0, cpos, Height);
                pe.Graphics.FillPolygon(_penMarker.Brush, new PointF[] { new(cpos - 5, 0), new(cpos + 5, 0), new(cpos, 10) });
            }
            // else free-running

            // Time text.
            _format.Alignment = StringAlignment.Center;
            _format.LineAlignment = StringAlignment.Center;
            pe.Graphics.DrawString(MusicTime.Format(State.Instance.CurrentTick), FontLarge, Brushes.Black, ClientRectangle, _format);
            
            _format.Alignment = StringAlignment.Near;
            _format.LineAlignment = StringAlignment.Near;
            pe.Graphics.DrawString(MusicTime.Format(State.Instance.LoopStart), FontSmall, Brushes.Black, ClientRectangle, _format);
            
            _format.Alignment = StringAlignment.Far;
            _format.LineAlignment = StringAlignment.Near;
            pe.Graphics.DrawString(MusicTime.Format(State.Instance.LoopEnd), FontSmall, Brushes.Black, ClientRectangle, _format);
        }
        #endregion

        #region UI handlers
        /// <summary>
        /// Handle selection operations.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (State.Instance.IsComposition && e.KeyData == Keys.Escape)
            {
                // Reset.
                State.Instance.LoopStart = -1;
                State.Instance.LoopEnd = -1;
                Invalidate();
            }
            base.OnKeyDown(e);
        }

        /// <summary>
        /// Handle mouse position changes.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (State.Instance.IsComposition)
            {
                var sub = GetTickFromClient(e.X);
                var bs = GetRounded(sub, Snap);
                var sdef = GetTimeDefString(bs);

                _toolTip.SetToolTip(this, $"{MusicTime.Format(bs)} {sdef}");

                _lastXPos = e.X;

                Invalidate();
            }
            base.OnMouseMove(e);
        }

        /// <summary>
        /// Selection of time and loop points.
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (State.Instance.IsComposition)
            {
                int lstart = State.Instance.LoopStart;
                int lend = State.Instance.LoopEnd;
                int newval = GetRounded(GetTickFromClient(e.X), Snap);

                if (ModifierKeys.HasFlag(Keys.Control))
                {
                    if (newval < lend)
                    {
                        State.Instance.LoopStart = newval;
                    }
                    // else beeeeeep?
                }
                else if (ModifierKeys.HasFlag(Keys.Alt))
                {
                    if (newval > lstart)
                    {
                        State.Instance.LoopEnd = newval;
                    }
                    // else beeeeeep?
                }
                else
                {
                    State.Instance.CurrentTick = newval;
                }

                Invalidate();
            }
            base.OnMouseDown(e);
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Gets the time def string associated with val.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        static string GetTimeDefString(int val)
        {
            string s = "";

            State.Instance.SectionInfo.TakeWhile(si => si.tick <= val).ForEach(si => s = si.name);

            return s;
        }

        /// <summary>
        /// Convert x pos to tick.
        /// </summary>
        /// <param name="x"></param>
        int GetTickFromClient(int x)
        {
            int tick = 0;

            if (State.Instance.CurrentTick < State.Instance.Length)
            {
                tick = x * State.Instance.Length / Width;
                tick = MathUtils.Constrain(tick, 0, State.Instance.Length);
            }

            return tick;
        }

        /// <summary>
        /// Map from time to UI pixels.
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        int GetClientFromTick(int tick)
        {
            return State.Instance.Length > 0 ? tick * Width / State.Instance.Length : 0;
        }

        /// <summary>
        /// Set to sub using specified rounding.
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="snapType"></param>
        /// <param name="up">To ceiling otherwise closest.</param>
        static int GetRounded(int tick, SnapType snapType, bool up = false)
        {
            if (tick > 0 && snapType != SnapType.Sub)
            {
                int res = snapType == SnapType.Bar ? MusicTime.SUBS_PER_BAR : MusicTime.SUBS_PER_BEAT;

                double dtick = Math.Floor((double)tick);
                int floor = (int)(dtick / res) * res;
                int ceiling = floor + res;

                tick = (up || (ceiling - tick) < res / 2) ? ceiling : floor;
            }

            return tick;
        }
        #endregion
    }
}
