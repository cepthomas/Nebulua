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

    ///// <summary>Notify client of ui changes.</summary>
    //public class TimeChangeEventArgs : EventArgs
    //{
    //    public int Tick { get; set; } = 0;
    //    public TimeType TimeType { get; set; } = TimeType.CurrentTick;
    //}
    //public enum TimeType { CurrentTick, LoopStart, LoopEnd }


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
        //int State.Instance.Length = 0; //TODO1 ?? use State instead
        //int State.Instance.LoopStart = -1;
        //int State.Instance.LoopEnd = -1;
        //int State.Instance.CurrentTick = 0;
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





        ///// <summary>Parts of the composition plus total length.</summary>
        //public List<(int tick, string name)> SectionInfo
        //{
        //    get { return _sectionInfo; }
        //    set { _sectionInfo = value; State.Instance.Length = value.Last().tick; ValidateTimes(); Invalidate(); }
        //}
        //List<(int tick, string name)> _sectionInfo = [];

        ///// <summary>Total length of the composition.</summary>
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        //public int Length { get { return State.Instance.Length; } }

        ///// <summary>Start of marked region.</summary>
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        //public int LoopStart { get { return State.Instance.LoopStart; } set { State.Instance.LoopStart = value; ValidateTimes(); Invalidate(); } }

        ///// <summary>End of marked region.</summary>
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        //public int LoopEnd { get { return State.Instance.LoopEnd; } set { State.Instance.LoopEnd = value; ValidateTimes(); Invalidate(); } }

        ///// <summary>Where we be now.</summary>
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        //public int CurrentTick { get { return State.Instance.CurrentTick; } set { State.Instance.CurrentTick = value; ValidateTimes(); Invalidate(); } }



        // /// <summary>All the important beat points with their names. Used also by tooltip.</summary>
        // [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        // public Dictionary<int, string> TimeDefs { get; set; } = new Dictionary<int, string>();

        // /// <summary>Total length of the bar.</summary>
        // [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        // public int Length { get { return State.Instance.Length; } set { State.Instance.Length = value; ValidateTimes(); Invalidate(); } }

        // /// <summary>Start of marked region.</summary>
        // [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        // public int LoopStart { get { return State.Instance.LoopStart; } set { State.Instance.LoopStart = value; ValidateTimes(); Invalidate(); } }

        // /// <summary>End of marked region.</summary>
        // [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        // public int LoopEnd { get { return State.Instance.LoopEnd; } set { State.Instance.LoopEnd = value; ValidateTimes(); Invalidate(); } }

        // /// <summary>Where we be now.</summary>
        // [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        // public int Current { get { return State.Instance.CurrentTick; } set { State.Instance.CurrentTick = value; ValidateTimes(); Invalidate(); } }


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
            
            //if (State.Instance.CurrentTick < State.Instance.Length)
            {
                int dstart = GetMouseFromTick(State.Instance.LoopStart);
                int dend = State.Instance.CurrentTick > State.Instance.LoopEnd ? GetMouseFromTick(State.Instance.LoopEnd) : GetMouseFromTick(State.Instance.CurrentTick);
                pe.Graphics.FillRectangle(_brush, dstart, 0, dend - dstart, Height);
            }

            // Draw start/end markers.
            //if (State.Instance.LoopStart != 0 || State.Instance.LoopEnd != State.Instance.Length)
            {
                int mstart = GetMouseFromTick(State.Instance.LoopStart);
                int mend = GetMouseFromTick(State.Instance.LoopEnd);
                pe.Graphics.DrawLine(_penMarker, mstart, 0, mstart, Height);
                pe.Graphics.DrawLine(_penMarker, mend, 0, mend, Height);
            }

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
            //if (e.Button == MouseButtons.Left)//TODO1 ??
            //{
            //    State.Instance.CurrentTick = GetRounded(GetTickFromMouse(e.X), Snap);
            //    CurrentTimeChanged?.Invoke(this, new EventArgs());
            //}
            //else if (e.X != _lastXPos)
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
        /// Selection of time and loop points.
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            int lstart = State.Instance.LoopStart;
            int lend = State.Instance.LoopEnd;
            int newval = GetRounded(GetTickFromMouse(e.X), Snap);

            if (ModifierKeys.HasFlag(Keys.Control))
            {
                if (newval < lend)
                {
                    State.Instance.LoopStart = newval;
                }
                // else beeeeeep
            }
            else if (ModifierKeys.HasFlag(Keys.Alt))
            {
                if (newval > lstart)
                {
                    State.Instance.LoopEnd = newval;
                }
                // else beeeeeep
            }
            else
            {
                State.Instance.CurrentTick = newval;
            }

            Invalidate();
            base.OnMouseDown(e);
        }
        #endregion

        #region Public functions
        ///// <summary>
        ///// Change current time. 
        ///// </summary>
        ///// <param name="num">Ticks.</param>
        ///// <returns>True if at the end of the sequence.</returns>
        //public bool IncrementCurrent(int num)
        //{
        //    bool done = false;

        //    State.Instance.CurrentTick += num;

        //    if (State.Instance.CurrentTick < 0)
        //    {
        //        State.Instance.CurrentTick = 0;
        //    }
        //    else if (State.Instance.CurrentTick < State.Instance.LoopStart)
        //    {
        //        State.Instance.CurrentTick = GetRounded(State.Instance.LoopStart, SnapType.Sub);
        //        done = true;
        //    }
        //    else if (State.Instance.CurrentTick > State.Instance.LoopEnd)
        //    {
        //        State.Instance.CurrentTick = GetRounded(State.Instance.LoopEnd, SnapType.Sub);
        //        done = true;
        //    }

        //    Invalidate();

        //    return done;
        //}

        // /// <summary>
        // /// Clear everything.
        // /// </summary>
        // public void Reset()
        // {
        //     _lastXPos = 0;
        //     State.Instance.Length = 0;
        //     State.Instance.CurrentTick = 0;
        //     State.Instance.LoopStart = 0;
        //     State.Instance.LoopEnd = 0;

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

            foreach (var si in State.Instance.SectionInfo)
            {
                if (si.tick > val)
                {
                    break;
                }
                else
                {
                    s = si.name;
                }
            }

            return s;
        }
        #endregion

        #region Private functions
        ///// <summary>
        ///// Validate and correct all times.
        ///// </summary>
        //void ValidateTimes()
        //{
        //    // 0 -> start -> end -> length

        //    // Must have this.
        //    if (State.Instance.Length <= 0)
        //    {
        //        throw new ScriptSyntaxException("Length not set");
        //    }

        //    // Fix end points.
        //    if (State.Instance.LoopStart < 0) State.Instance.LoopStart = 0;
        //    if (State.Instance.LoopEnd < 0) State.Instance.LoopEnd = State.Instance.Length;
        //    // and loop points...
        //    State.Instance.LoopEnd = Math.Min(State.Instance.LoopEnd, State.Instance.Length);
        //    State.Instance.LoopStart = Math.Min(State.Instance.LoopStart, State.Instance.LoopEnd);
        //    State.Instance.CurrentTick = MathUtils.Constrain(State.Instance.CurrentTick, State.Instance.LoopStart, State.Instance.LoopEnd);
        //}

        /// <summary>
        /// Convert x pos to tick.
        /// </summary>
        /// <param name="x"></param>
        int GetTickFromMouse(int x)
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
        int GetMouseFromTick(int tick)
        {
            return State.Instance.Length > 0 ? tick * Width / State.Instance.Length : 0;
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
