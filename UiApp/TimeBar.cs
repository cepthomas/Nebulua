using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;
using Nebulua.Common;


namespace Nebulua.UiApp
{
    /// <summary>User selection options.</summary>
    public enum SnapType { Bar, Beat, Sub }

    /// <summary>The control.</summary>
    public class TimeBar : UserControl
    {
        #region Fields
        /// <summary>For tracking mouse moves.</summary>
        int _lastXPos = 0;

        /// <summary>Tooltip for mousing.</summary>
        readonly ToolTip _toolTip = new();

        /// <summary>For drawing text.</summary>
        readonly StringFormat _format = new() { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };
        #endregion

        #region Backing fields
        readonly SolidBrush _brush = new(Color.White);
        readonly Pen _penMarker = new(Color.Black, 1);
        #endregion

        #region Properties
        /// <summary>For styling.</summary>
        public Color ProgressColor { get { return _brush.Color; } set { _brush.Color = value; } }

        /// <summary>For styling.</summary>
        public Color MarkerColor { get { return _penMarker.Color; } set { _penMarker.Color = value; } }

        /// <summary>Big font.</summary>
        public Font FontLarge { get; set; } = new("Microsoft Sans Serif", 20, FontStyle.Regular, GraphicsUnit.Point, 0);

        /// <summary>Baby font.</summary>
        public Font FontSmall { get; set; } = new("Microsoft Sans Serif", 10, FontStyle.Regular, GraphicsUnit.Point, 0);

        /// <summary>How to select times.</summary>
        public SnapType Snap { get; set; }
        #endregion

        //#region Events
        ///// <summary>Value changed by user.</summary>
        //public event EventHandler? CurrentTimeChanged;
        //#endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public TimeBar()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
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
                _brush.Dispose();
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

            // Draw the bar.
            int dcurrent = GetClientFromTick(State.Instance.CurrentTick);
            pe.Graphics.FillRectangle(_brush, dcurrent - 1, 0, 2, Height);

            //int dstart = GetClientFromTick(State.Instance.LoopStart);
            //int dend = State.Instance.CurrentTick > State.Instance.LoopEnd ?
            //    GetClientFromTick(State.Instance.LoopEnd) :
            //    GetClientFromTick(State.Instance.CurrentTick);
            //pe.Graphics.FillRectangle(_brush, dstart, 0, dend - dstart, Height);

            // Draw start/end markers.
            int mstart = GetClientFromTick(State.Instance.LoopStart);
            int mend = GetClientFromTick(State.Instance.LoopEnd);
            pe.Graphics.DrawLine(_penMarker, mstart, 0, mstart, Height);
            pe.Graphics.DrawLine(_penMarker, mend, 0, mend, Height);

            // Text.
            _format.Alignment = StringAlignment.Center;
            pe.Graphics.DrawString(MusicTime.Format(State.Instance.CurrentTick), FontLarge, Brushes.Black, ClientRectangle, _format);
            _format.Alignment = StringAlignment.Near;
            pe.Graphics.DrawString(MusicTime.Format(State.Instance.LoopStart), FontSmall, Brushes.Black, ClientRectangle, _format);
            _format.Alignment = StringAlignment.Far;
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
            if(e.KeyData == Keys.Escape)
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
            var sub = GetTickFromClient(e.X);
            var bs = GetRounded(sub, Snap);
            var sdef = GetTimeDefString(bs);

            _toolTip.SetToolTip(this, $"{MusicTime.Format(bs)} {sdef}");
            //_toolTip.SetToolTip(this, $"{State.Instance.CurrentTick}: 0 {State.Instance.LoopStart} {State.Instance.LoopEnd} {State.Instance.Length} ");

            _lastXPos = e.X;

            Invalidate();
            base.OnMouseMove(e);
        }

        /// <summary>
        /// Selection of time and loop points.
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
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

            foreach (var (tick, name) in State.Instance.SectionInfo)
            {
                if (tick > val)
                {
                    break;
                }
                else
                {
                    s = name;
                }
            }

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
        /// <param name="val"></param>
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
                // res:32 in:27 floor=(in%aim)*aim  ceiling=floor+aim
                int res = snapType == SnapType.Bar ? Defs.SUBS_PER_BAR : Defs.SUBS_PER_BEAT;

                double dtick = Math.Floor((double)tick);
                int floor = (int)(dtick / res) * res;
                int ceiling = floor + res;

                if (up || (ceiling - tick) < res / 2)
                {
                    tick = ceiling;
                }
                else
                {
                    tick = floor;
                }
            }

            return tick;
        }
        #endregion
    }
}
