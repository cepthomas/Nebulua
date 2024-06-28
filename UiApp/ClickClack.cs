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
    /// Generic input control similar to BingBong from MidiLib.
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
        public event EventHandler<ClickClackEventArgs>? ClickClackEvent;

        public class ClickClackEventArgs : EventArgs
        {
            /// <summary>The X value in user coordinates. null means invalid.</summary>
            public int? X { get; set; } = null;

            /// <summary>The Y value in user coordinates. -1 means unclicked. null means invalid.</summary>
            public int? Y { get; set; } = null;

            /// <summary>Read me.</summary>
            public override string ToString()
            {
                return $"ClickClack X:{(X is null ? "null" : X)} Y:{(Y is null ? "null" : Y)}";
            }
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
            var mp = PointToClient(MousePosition);
            var (ux, uy) = XyToUser(mp.X, mp.Y);

            // Lingerer?
            if (_lastClickX is not null)
            {
                // Turn off last click.
                ClickClackEvent?.Invoke(this, new() { X = ux, Y = 0 });
            }

            // Reset.
            _lastClickX = null;
            ClickClackEvent?.Invoke(this, new() { X = null, Y = null });

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
            var mp = PointToClient(MousePosition);
            var (ux, uy) = XyToUser(mp.X, mp.Y);

            if (e.Button == MouseButtons.Left)
            {
                // Dragging. Did it change?
                if (_lastClickX != ux)
                {
                    if (_lastClickX is not null)
                    {
                        // Turn off last click.
                        ClickClackEvent?.Invoke(this, new() { X = _lastClickX, Y = 0 });
                    }

                    // Start the new click.
                    _lastClickX = ux;
                    ClickClackEvent?.Invoke(this, new() { X = ux, Y = uy });
                }
            }

            _toolTip.SetToolTip(this, $"ux:{ux} uy:{uy}");

            base.OnMouseMove(e);
        }

        /// <summary>
        /// Send info to client.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            var mp = PointToClient(MousePosition);
            var (ux, uy) = XyToUser(mp.X, mp.Y);
            _lastClickX = ux;

            ClickClackEvent?.Invoke(this, new() { X = ux, Y = uy });

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
                ClickClackEvent?.Invoke(this, new() { X = _lastClickX, Y = 0 });
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
        /// Map function.
        /// </summary>
        /// <param name="x">UI location.</param>
        /// <param name="y">UI location.</param>
        /// <returns>Tuple of x and y mapped to user coordinates.</returns>
        (int ux, int uy) XyToUser(int x, int y)
        {
            int ux = MathUtils.Map(x, 0, Width, MinX, MaxX);
            int uy = MathUtils.Map(y, Height, 0, MinY, MaxY);

            return (ux, uy);
        }
        #endregion
    }
}
