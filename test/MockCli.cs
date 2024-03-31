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

        string _prompt;

        public Cli(TextReader cliIn, TextWriter cliOut, string prompt)
        {
            //_cliIn = new CliTextReader();
            //_cliOut = new CliTextWriter();
            _prompt = prompt;
        }

        public void Write(string s)
        {
            //_ = CaptureLines.Append(s);
        }

        public NebStatus Read()
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

            return NebStatus.Ok;
        }
    }
}