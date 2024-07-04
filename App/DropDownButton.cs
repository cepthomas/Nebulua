using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;


namespace Nebulua
{
    // Based on https://stackoverflow.com/a/27173509

    public class DropDownButton : Button
    {
        int _splitWidth = 20;

        #region Events
        /// <summary>Drop down selection event.</summary>
        public event EventHandler<string>? Selected;
        #endregion

        /// <summary>Drop down options. Empty line inserts a separator.</summary>
        public List<string> Options { get; set; } = [];

        public bool DropDownEnabled { get; set; } = true;

        public DropDownButton()
        {
            ContextMenuStrip = new();
        }

        /// <summary>
        /// Handle drop down selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Menu_Click(object? sender, EventArgs e)
        {
            // int index = Options.IndexOf(sender!.ToString()!);
            // Selected?.Invoke(this, index);
            Selected?.Invoke(this, sender!.ToString()!);
        }

        /// <summary>
        /// Handle mouse down.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            ContextMenuStrip.Items.Clear();

            // Populate menu with options.
            foreach(var o in Options)
            {
                ContextMenuStrip.Items.Add(o == "" ? new ToolStripSeparator() : new ToolStripMenuItem(o, null, Menu_Click));
            }
            
            // Show it.
            var splitRect = new Rectangle(Width - _splitWidth, 0, _splitWidth, Height);

            // Figure out if the button click was on the button itself or the menu split.
            if (e.Button == MouseButtons.Left && splitRect.Contains(e.Location))
            {
                ContextMenuStrip.Show(this, 0, Height);
            }
            else // default click
            {
                base.OnMouseDown(e);
            }
        }

        /// <summary>
        /// Draw everything.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Draw the arrow glyph on the right side of the button
            int x = ClientRectangle.Width - 14;
            int y = ClientRectangle.Height / 2 - 1;

            var arrowBrush = Enabled && DropDownEnabled ? SystemBrushes.ControlText : SystemBrushes.ButtonShadow;
            Point[] arrows = [new Point(x, y), new Point(x + 7, y), new Point(x + 3, y + 4)];
            e.Graphics.FillPolygon(arrowBrush, arrows);

            // Draw a dashed separator on the left of the arrow
            int lineX = ClientRectangle.Width - _splitWidth;
            int lineYFrom = y - 4;
            int lineYTo = y + 8;
            using (var separatorPen = new Pen(Brushes.DarkGray) { DashStyle = DashStyle.Dot })
            {
                e.Graphics.DrawLine(separatorPen, lineX, lineYFrom, lineX, lineYTo);
            }
        }
    }
}