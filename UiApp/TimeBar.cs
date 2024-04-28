using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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
        public SnapType Snap { get; set; }

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
        int _length = 0; >>> use State instead
        int _loopStart = -1;
        int _loopEnd = -1;
        int _current = 0;
        #endregion

        #region Properties
        /// <summary>Total length of the bar.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public int Length { get { return _length; } set { _length = value; ValidateTimes(); Invalidate(); } }

        /// <summary>Start of marked region.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public int LoopStart { get { return _loopStart; } set { _loopStart = value; ValidateTimes(); Invalidate(); } }

        /// <summary>End of marked region.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public int LoopEnd { get { return _loopEnd; } set { _loopEnd = value; ValidateTimes(); Invalidate(); } }

        /// <summary>Where we be now.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public int Current { get { return _current; } set { _current = value; ValidateTimes(); Invalidate(); } }

        /// <summary>For styling.</summary>
        public Color ProgressColor { get { return _brush.Color; } set { _brush.Color = value; } }

        /// <summary>For styling.</summary>
        public Color MarkerColor { get { return _penMarker.Color; } set { _penMarker.Color = value; } }

        /// <summary>Big font.</summary>
        public Font FontLarge { get; set; } = new("Microsoft Sans Serif", 20, FontStyle.Regular, GraphicsUnit.Point, 0);

        /// <summary>Baby font.</summary>
        public Font FontSmall { get; set; } = new("Microsoft Sans Serif", 10, FontStyle.Regular, GraphicsUnit.Point, 0);

        /// <summary>All the important beat points with their names. Used also by tooltip.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public Dictionary<int, string> TimeDefs { get; set; } = new Dictionary<int, string>();
        #endregion

        #region Events
        /// <summary>Value changed by user.</summary>
        public event EventHandler? CurrentTimeChanged;
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
            if (_current < _length)
            {
                int dstart = GetMouseFromTick(_loopStart);
                int dend = _current > _loopEnd ? GetMouseFromTick(_loopEnd) : GetMouseFromTick(_current);
                pe.Graphics.FillRectangle(_brush, dstart, 0, dend - dstart, Height);
            }

            // Draw start/end markers.
            if (_loopStart != 0 || _loopEnd != _length)
            {
                int mstart = GetMouseFromTick(_loopStart);
                int mend = GetMouseFromTick(_loopEnd);
                pe.Graphics.DrawLine(_penMarker, mstart, 0, mstart, Height);
                pe.Graphics.DrawLine(_penMarker, mend, 0, mend, Height);
            }

            // Text.
            _format.Alignment = StringAlignment.Center;
            pe.Graphics.DrawString(MusicTime.Format(_current), FontLarge, Brushes.Black, ClientRectangle, _format);
            _format.Alignment = StringAlignment.Near;
            pe.Graphics.DrawString(MusicTime.Format(_loopStart), FontSmall, Brushes.Black, ClientRectangle, _format);
            _format.Alignment = StringAlignment.Far;
            pe.Graphics.DrawString(MusicTime.Format(_loopEnd), FontSmall, Brushes.Black, ClientRectangle, _format);
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
                _loopStart = 0;
                _loopEnd = 0;
                Invalidate();
            }
            base.OnKeyDown(e);
        }

        /// <summary>
        /// Handle mouse position changes.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _current = GetRounded(GetTickFromMouse(e.X), Snap);
                CurrentTimeChanged?.Invoke(this, new EventArgs());
            }
            else if (e.X != _lastXPos)
            {
                var sub = GetTickFromMouse(e.X);
                var bs = GetRounded(sub, Snap);
                var sdef = GetTimeDefString(bs);
                _toolTip.SetToolTip(this, $"{MusicTime.Format(bs)} {sdef}");
                _lastXPos = e.X;
            }

            Invalidate();
            base.OnMouseMove(e);
        }

        /// <summary>
        /// Handle dragging.
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                _loopStart = GetRounded(GetTickFromMouse(e.X), Snap);
            }
            else if (ModifierKeys.HasFlag(Keys.Alt))
            {
                _loopEnd = GetRounded(GetTickFromMouse(e.X), Snap);
            }
            else
            {
                _current = GetRounded(GetTickFromMouse(e.X), Snap);
            }

            CurrentTimeChanged?.Invoke(this, new EventArgs());
            Invalidate();
            base.OnMouseDown(e);
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Change current time. 
        /// </summary>
        /// <param name="num">Ticks.</param>
        /// <returns>True if at the end of the sequence.</returns>
        public bool IncrementCurrent(int num)
        {
            bool done = false;

            _current += num;

            if (_current < 0)
            {
                _current = 0;
            }
            else if (_current < _loopStart)
            {
                _current = GetRounded(_loopStart, SnapType.Sub);
                done = true;
            }
            else if (_current > _loopEnd)
            {
                _current = GetRounded(_loopEnd, SnapType.Sub);
                done = true;
            }

            Invalidate();

            return done;
        }

        // /// <summary>
        // /// Clear everything.
        // /// </summary>
        // public void Reset()
        // {
        //     _lastXPos = 0;
        //     _length = 0;
        //     _current = 0;
        //     _loopStart = 0;
        //     _loopEnd = 0;

        //     Invalidate();
        // }

        /// <summary>
        /// Gets the time def string associated with val.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private string GetTimeDefString(int val)
        {
            string s = "";

            foreach (KeyValuePair<int, string> kv in TimeDefs)
            {
                if (kv.Key > val)
                {
                    break;
                }
                else
                {
                    s = kv.Value;
                }
            }

            return s;
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Validate and correct all times.
        /// </summary>
        void ValidateTimes()
        {
            // 0 -> start -> end -> length

            // Must have this.
            if (_length <= 0)
            {
                throw new ScriptSyntaxException("Length not set");
            }

            // Fix end points.
            if (_loopStart < 0) _loopStart = 0;
            if (_loopEnd < 0) _loopEnd = _length;
            // and loop points...
            _loopEnd = Math.Min(_loopEnd, _length);
            _loopStart = Math.Min(_loopStart, _loopEnd);
            _current = MathUtils.Constrain(_current, _loopStart, _loopEnd);
        }

        /// <summary>
        /// Convert x pos to tick.
        /// </summary>
        /// <param name="x"></param>
        int GetTickFromMouse(int x)
        {
            int tick = 0;

            if (_current < _length)
            {
                tick = x * _length / Width;
                tick = MathUtils.Constrain(tick, 0, _length);
            }

            return tick;
        }

        /// <summary>
        /// Map from time to UI pixels.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        int GetMouseFromTick(int tick)
        {
            return _length > 0 ? tick * Width / _length : 0;
        }

        /// <summary>
        /// Set to sub using specified rounding.
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="snapType"></param>
        /// <param name="up">To ceiling otherwise closest.</param>
        int GetRounded(int tick, SnapType snapType, bool up = false)
        {
            if (tick > 0 && snapType != SnapType.Sub)
            {
                // res:32 in:27 floor=(in%aim)*aim  ceiling=floor+aim
                int res = snapType == SnapType.Bar ? Defs.SUBS_PER_BAR : Defs.SUBS_PER_BEAT;
                int floor = (tick / res) * res;
                int ceiling = floor + res;

                if (up || (ceiling - tick) >= res / 2)
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
