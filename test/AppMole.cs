using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace Nebulua
{
    // Insert some hooks to support testing.
    public partial class App
    {
        // Fake cli output.
        CliTextWriter _myCliOut = new();

        // Fake cli input.
        CliTextReader _myCliIn = new();

        public List<string> CaptureLines
        {
            get { return StringUtils.SplitByTokens(_myCliOut.Capture.ToString(), "\r\n"); }
        }

        public string NextLine
        {
            get { return _myCliIn.NextLine; }
            set { _myCliIn.NextLine = value; }
        }

        public string Prompt
        {
            get { return _prompt; }
        }

        public void Clear()
        {
            _myCliOut.Capture.Clear();
            _myCliIn.NextLine = "";
        }

        public string GetPrompt()
        {
            return _prompt;
        }

        public void HookCli()
        {
            _cliOut = _myCliOut;
            _cliIn = _myCliIn;
        }
    }


    public class CliTextWriter: TextWriter
    {
        public StringBuilder Capture { get; } = new();

        public override void Write(char value)
        {
            Capture.Append(value);
        }
        public override Encoding Encoding
        {
            get { return Encoding.Default; }
        }

        public void Clear()
        {
        Capture.Clear();
        }
    }


    public class CliTextReader: TextReader
    {
        public string NextLine { get; set; } = "";

        public override int Read()
        {
            // return the next char or -1 if done.
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
            // return the next char or -1 if done. Doesn't remove.
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