using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace Ephemera.Nebulua.Test
{
    /// <summary>Mock IO for testing cli. Captures output lines.</summary>
    public class MockCliOut: TextWriter
    {
        public List<string> Capture
        {
            get { return StringUtils.SplitByTokens(_capture.ToString(), "\r\n"); }
        }

        StringBuilder _capture = new();

        public override void Write(char value)
        {
            _capture.Append(value);
        }
        public override Encoding Encoding
        {
            get { return Encoding.Default; }
        }

        public void Clear()
        {
            _capture.Clear();
        }
    }


    /// <summary>Mock for testing cli. Force feed input line.</summary>
    public class MockCliIn: TextReader
    {
        public string NextLine { get; set; } = "";

        // ReadLine() calls Read() repeatedly.
        public override int Read()
        {
            // Return the next char or -1 if done.
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

        public override int Peek()
        {
            // throw new NotSupportedException("peek() doesn't work!");
            // Return the next char or -1 if done. Doesn't remove.
            if (NextLine.Length > 0)
            {
                return NextLine[0];
            }
            else
            {
                return -1;
            }
        }
    }
}