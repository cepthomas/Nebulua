using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
//using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Interop;


namespace Nebulua
{
    public class Cli
    {
        //    readonly TextWriter _cliOut;
        //    readonly TextReader _cliIn;

        public string Prompt { get; set; } = ">";

        public Cli(TextReader cliIn, TextWriter cliOut)
        {
            //_cliIn = new CliTextReader();
            //_cliOut = new CliTextWriter();
        }

        public void Write(string s)
        {
            //_ = CaptureLines.Append(s);
        }

        public bool Read()
        {
            //// return the next char or -1 if done.
            //if (NextLine.Length > 0)
            //{
            //    int c = NextLine[0];
            //    NextLine = NextLine.Remove(0, 1);
            //    return c;
            //}
            //else
            //{
            //    return -1;
            //}

            return true;
        }
    }
}