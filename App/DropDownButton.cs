using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;


namespace Nebulua
{
    // Based on https://stackoverflow.com/a/27173509
    // Expanding @Jaex answer a little bit to allow for a separator line, conditional drawing of the arrow if nothing
    // is configured and a separate click event for the main button body and the menu arrow.
    // It should be noted that for better alignment you can set the button.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

    public class DropDownButton : Button
    {
        //[DefaultValue(null)]
        //[Browsable(true)]
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        //public ContextMenuStrip Menu { get; set; }

        // [DefaultValue(20)]
        // [Browsable(true)]
        // [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        // public int SplitWidth { get; set; }

        int _splitWidth = 20;

        #region Events
        public event EventHandler<int>? Selected;
        #endregion

        /// <summary>Drop down options. Empty line inserts a separator.</summary>
        public List<string> Options { get; set; } = [];

        public DropDownButton()
        {
           // SplitWidth = 20;
            //MenuButton = ContextMenuStrip;
            ContextMenuStrip = new();
        }

        /// <summary>
        /// Handle drop down selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Menu_Click(object? sender, EventArgs e)
        {
            int index = Options.IndexOf(sender!.ToString()!);
            Selected?.Invoke(this, index);
        }

        /// <summary>
        /// Handle mouse down.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            ContextMenuStrip!.Items.Clear();

            // Populate withh options.
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
            else
            {
                base.OnMouseDown(e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Draw the arrow glyph on the right side of the button
            int arrowX = ClientRectangle.Width - 14;
            int arrowY = ClientRectangle.Height / 2 - 1;

            var arrowBrush = Enabled ? SystemBrushes.ControlText : SystemBrushes.ButtonShadow;
            Point[] arrows = [new Point(arrowX, arrowY), new Point(arrowX + 7, arrowY), new Point(arrowX + 3, arrowY + 4)];
            e.Graphics.FillPolygon(arrowBrush, arrows);

            // Draw a dashed separator on the left of the arrow
            int lineX = ClientRectangle.Width - _splitWidth;
            int lineYFrom = arrowY - 4;
            int lineYTo = arrowY + 8;
            using (var separatorPen = new Pen(Brushes.DarkGray) { DashStyle = DashStyle.Dot })
            {
                e.Graphics.DrawLine(separatorPen, lineX, lineYFrom, lineX, lineYTo);
            }
        }
    }
}