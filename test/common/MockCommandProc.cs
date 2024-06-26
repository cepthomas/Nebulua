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
    /// <summary>Mock CliApp command processor. See real class for doc. Currently not used.</summary>
    public class CommandProc
    {
        // readonly TextWriter _out;
        // readonly TextReader _in;

        public string Prompt { get; set; } = ">";

        // From out. May be useful
        public List<string> WriteLines { get; set; } = [];

        // For in.
        //public string ReadLine { get; set; } = "";

        // public void Clear()
        // {
        //     _capture.Clear();
        // }

        public CommandProc(TextReader rin, TextWriter rout)
        {
            // _in = rin;
            // _out = rout;
        }

        public void Write(string s)
        {
            WriteLines.Append(s);
        }

        public bool Read()
        {
            return true;
        }
    }
}