using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace Nebulua.UiApp
{
    /// <summary>
    /// Generic input control similar to BingBong from MidiLib. TODO1 move to nbui.
    /// </summary>
    public class ClickClack : UserControl
    {
        #region Fields
        /// <summary>Background image data.</summary>
        PixelBitmap? _bmp;

        /// <summary>Last key down position in client coordinates.</summary>
        int? _lastClickX = null;

        /// <summary>Tool tip.</summary>
        readonly ToolTip _toolTip = new();

        /// <summary>The grid pen.</summary>
        readonly Pen _pen = new(Color.WhiteSmoke, 1);
        #endregion

        #region Properties
        /// <summary>Lowest X value of interest.</summary>
        public int MinX { get; set; } = 0;

        /// <summary>Highest X value of interest.</summary>
        public int MaxX { get; set; } = 100;

        /// <summary>Min Y value.</summary>
        public int MinY { get; set; } = 0;

        /// <summary>Max Y value.</summary>
        public int MaxY { get; set; } = 100;

        /// <summary>Visibility.</summary>
        public List<int> GridX { get; set; } = [];

        /// <summary>Visibility.</summary>
        public List<int> GridY { get; set; } = [];
        #endregion

        #region Events
        /// <summary>Click/move info.</summary>
        public event EventHandler<UserEventArgs>? UserEvent;

        public class UserEventArgs : EventArgs
        {
            /// <summary>The X value in user coordinates. null means invalid.</summary>
            public int? X { get; set; } = null;

            /// <summary>The Y value in user coordinates. -1 means unclicked. null means invalid.</summary>
            public int? Y { get; set; } = null;

            /// <summary>Read me.</summary>
            public override string ToString()
            {
                string sx = X is null ? "null" : X.ToString()!;
                string sy = Y is null ? "null" : Y.ToString()!;
                return $"ClickClack X:{sx} Y:{sy}";
            }
        }

        /// <summary>
        /// Event raiser. Probably not needed.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        void RaiseUserEvent(int? x, int? y)
        {
            UserEvent?.Invoke(this, new() { X = x, Y = y });
        }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public ClickClack()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            Name = nameof(ClickClack);
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
        /// Disable control
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            // Turn off last click.
            if (_lastClickX is not null)
            {
                RaiseUserEvent(_lastClickX, 0);
            }

            // Reset and tell client.
            _lastClickX = null;
            RaiseUserEvent(null, null);

            base.OnMouseLeave(e);
        }

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

            // Draw grid.
            foreach (var gl in GridX)
            {
                if (gl >= MinX && gl <= MaxX) // sanity - throw?
                {
                    int x = MathUtils.Map(gl, MinX, MaxX, 0, Width);
                    pe.Graphics.DrawLine(_pen, x, 0, x, Height);
                }
            }

            foreach (var gl in GridY)
            {
                if (gl >= MinY && gl <= MaxY)
                {
                    int y = MathUtils.Map(gl, MinY, MaxY, Height, 0);
                    pe.Graphics.DrawLine(_pen, 0, y, Width, y);
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
            var (ux, uy) = MouseToUser();
            _toolTip.SetToolTip(this, $"ux:{ux} uy:{uy}");

            if (e.Button == MouseButtons.Left)
            {
                // Dragging. Did it change?
                if (_lastClickX != ux)
                {
                    if (_lastClickX is not null)
                    {
                        // Turn off last click.
                        RaiseUserEvent(_lastClickX, 0);
                    }

                    // Start the new click.
                    _lastClickX = ux;
                    RaiseUserEvent(ux, uy);
                }
            }

            base.OnMouseMove(e);
        }

        /// <summary>
        /// Send info to client.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            var (ux, uy) = MouseToUser();
            _lastClickX = ux;
            RaiseUserEvent(ux, uy);

            base.OnMouseDown(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_lastClickX is not null)
            {
                RaiseUserEvent(_lastClickX, 0);
            }
            _lastClickX = null;

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

            // Draw background.
            _bmp = new(Width, Height);
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    _bmp!.SetPixel(x, y, 255, x * 256 / Width, y * 256 / Height, 150);
                }
            }
        }

        /// <summary>
        /// Get mouse x and y mapped to user coordinates.
        /// </summary>
        /// <returns>Tuple of x and y.</returns>
        (int? ux, int? uy) MouseToUser()
        {
            var mp = PointToClient(MousePosition);

            // Map and check.
            int x = MathUtils.Map(mp.X, 0, Width, MinX, MaxX);
            int? ux = x >= 0 && x < Width ? x : null;
            int y = MathUtils.Map(mp.Y, Height, 0, MinY, MaxY);
            int? uy = y >= 0 && y < Height ? y : null;

            return (ux, uy);
        }
        #endregion
    }
}
