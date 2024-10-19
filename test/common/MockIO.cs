using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace Nebulua.Test
{
    public class MockConsole : IConsole
    {
        #region Fields
        readonly StringBuilder _capture = new();
        int _left = 0;
        int _top = 0;
        #endregion

        #region Internals
        public List<string> Capture { get { return StringUtils.SplitByTokens(_capture.ToString(), Environment.NewLine); } }
        public string NextReadLine { get; set; } = "";
        public void Reset()
        {
            _capture.Clear();
        }
        #endregion

        #region IConsole implementation
        public bool KeyAvailable { get => NextReadLine.Length > 0; }
        public bool CursorVisible { get; set; } = true;
        public string Title { get; set; } = "";
        public int BufferWidth { get; set; }

        public string? ReadLine()
        {
            if (NextReadLine == "")
            {
                return null;
            }
            else
            {
                var ret = NextReadLine;
                NextReadLine = "";
                return ret;
            }
        }
        public ConsoleKeyInfo ReadKey(bool intercept)
        {
            if (KeyAvailable)
            {
                var key = NextReadLine[0];
                NextReadLine = NextReadLine.Substring(1);
                return new ConsoleKeyInfo(key, (ConsoleKey)key, false, false, false);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public void Write(string text)
        {
            _capture.Append(text);
        }

        public void WriteLine(string text)
        {
            _capture.Append(text + Environment.NewLine);
        }

        public void SetCursorPosition(int left, int top)
        {
            _left = left;
            _top = top;
        }

        public (int left, int top) GetCursorPosition()
        {
            return (_left, _top);
        }
        #endregion
    }
}