using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;
using Ephemera.NBagOfTricks;


namespace Nebulua.UiApp
{
    public class CliInput : UserControl
    {
        #region Properties
        /// <summary>Cosmetics.</summary>
        public override Color BackColor { get { return _rtb.BackColor; } set { _rtb.BackColor = value; } }

        /// <summary>Cosmetics.</summary>
        public override Font Font { get { return _rtb.Font; } set { _rtb.Font = value; } }

        /// <summary>Optional prompt.</summary>
        public string Prompt { get; set; } = "???";

        /// <summary>Support for TextReader.</summary>
        public TextReader In { get { return _in; } }
        #endregion

        #region Internal class
        /// <summary>
        /// Give this component TextReader interface. Wouldn't have to do this if C# supported multiple inheritance.
        /// </summary>
        class InReader : TextReader
        {
            public string NextLine { get; set; } = "";

            // ReadLine() calls Read() repeatedly.
            public override int Read()
            {
                // Return the next char or -1 if none.
                if (NextLine.Length > 0)
                {
                    int c = NextLine[0];
                    NextLine = NextLine.Remove(0, 1);
                    return c;
                }
                else
                {
                    return -1;
                }
            }
        }
        #endregion

        #region Fields
        /// <summary>Contained control.</summary>
        readonly RichTextBox _rtb;

        /// <summary>Support for TextReader.</summary>
        readonly InReader _in = new();

        /// <summary>Most recent at beginning.</summary>
        List<string> _history = [];

        /// <summary>Current location in list.</summary>
        int _historyIndex = 0;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor sets some defaults.
        /// </summary>
        public CliInput()
        {
            _rtb = new()
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                BorderStyle = BorderStyle.None,
                ForeColor = Color.Black,
                Multiline = false,
                ReadOnly = false,
                ScrollBars = RichTextBoxScrollBars.Horizontal,
                AcceptsTab = true,
                TabIndex = 0,
                Text = ""
            };

            _rtb.KeyDown += Rtb_KeyDown;
            Controls.Add(_rtb);
        }
        #endregion

        #region Misc functions
        /// <summary>
        /// Update the history with the new entry.
        /// </summary>
        /// <param name="s"></param>
        void AddToHistory(string s)
        {
            if (s.Length > 0)
            {
                var newlist = new List<string> { s };
                // Check for dupes and max size.
                _history.ForEach(v => { if (!newlist.Contains(v) && newlist.Count <= 20) newlist.Add(v); });
                _history = newlist;
                _historyIndex = 0;
            }
        }
        #endregion

        #region Handle input
        /// <summary>
        /// Catch a few keys.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Rtb_KeyDown(object? sender, KeyEventArgs e)
        {
            // esc clears entry

            switch (e.KeyCode)
            {
                case Keys.Enter:
                    // Add to history and seed for client.
                    var t = _rtb.Text.Remove(0, Prompt.Length);
                    AddToHistory(t);
                    _in.NextLine = t;
                    break;

                case Keys.Escape:
                    // Throw away current.
                    _rtb.Text = $"{Prompt}";
                    break;

                case Keys.Up:
                    // Go through history older.
                    if (_historyIndex < _history.Count - 1)
                    {
                        _historyIndex++;
                        _rtb.Text = $"{Prompt}{_history[_historyIndex]}";
                    }
                    break;

                case Keys.Down:
                    // Go through history newer.
                    if (_historyIndex > 0)
                    {
                        _historyIndex--;
                        _rtb.Text = $"{Prompt}{_history[_historyIndex]}";
                    }
                    break;

                //case Keys.Space:
                //    // TODO1? Use for start stop.
                //    break;

                default:
                    //e.SuppressKeyPress = true;
                    //e.Handled = true;
                    break;
            }
        }
        #endregion
    }
}
