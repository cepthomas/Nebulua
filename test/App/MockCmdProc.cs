using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Interop;


namespace Ephemera.Nebulua
{
    public class CmdProc
    {
        // readonly TextWriter _cliOut;
        // readonly TextReader _cliIn;

        public string Prompt { get; set; } = ">";

        // From cliOut. May be useful
        public List<string> WriteLines { get; set; } = [];

        // For cliIn.
        //public string ReadLine { get; set; } = "";

        // public void Clear()
        // {
        //     _capture.Clear();
        // }

        public CmdProc(TextReader cliIn, TextWriter cliOut)
        {
            // _cliIn = cliIn;
            // _cliOut = cliOut;
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