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
        StringBuilder _capture = new();
        int _left = 0;
        int _top = 0;

        public List<string> Capture { get { return StringUtils.SplitByTokens(_capture.ToString(), "\r\n"); } }
        public bool KeyAvailable { get => NextLine.Length > 0; }
        public bool CursorVisible { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Title { get; set; } = "";

        public string NextLine { get; set; } = "";

        public string? ReadLine()
        {
            if (NextLine == "")
            {
                return null;
            }
            else
            {
                var ret = NextLine;// + Environment.NewLine;
                NextLine = "";
                return ret;
            }
        }

        public ConsoleKeyInfo ReadKey(bool intercept)
        {
            if (KeyAvailable)
            {
                var key = NextLine[0];
                NextLine = NextLine.Substring(1);
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

        public void Clear()
        {
            _capture.Clear();
        }
    }
}