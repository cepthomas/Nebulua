using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;


namespace Nebulua
{
    // Based on https://stackoverflow.com/a/27173509

    public class DropDownButton : Button // TODO1 put in nbui
    {
        #region Events
        /// <summary>Drop down selection event.</summary>
        public event EventHandler<string>? Selected;
        #endregion

        #region Properties
        /// <summary>Drop down options. Empty line inserts a separator.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<string> Options { get; set; } = [];
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
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
            ContextMenuStrip!.Items.Clear();

            // Populate menu with options.
            foreach(var o in Options)
            {
                ContextMenuStrip.Items.Add(o == "" ? new ToolStripSeparator() : new ToolStripMenuItem(o, null, Menu_Click));
            }
            ContextMenuStrip.Show(this, 0, Height);
        }

        /// <summary>
        /// Draw everything.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Draw the arrow.
            int x = ClientRectangle.Width - 12;
            int y = ClientRectangle.Height - 12;

            using var br = new SolidBrush(Color.Gray);
            Point[] arrows = [new Point(x, y), new Point(x + 10, y), new Point(x + 5, y + 10)];
            e.Graphics.FillPolygon(br, arrows);
        }
    }
}