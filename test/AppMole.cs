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
    public partial class App
    {
        public CliTextWriter CliOut { get; set; } = new();
        public CliTextReader CliIn { get; set; } = new();

        public void HookCli()
        {
            _cliOut = CliOut;
            _cliIn = CliIn;
        }
    }


    // Fake cli output.
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
    }

    // Fake cli input.
    public class CliTextReader: TextReader
    {
        public string NextLine { get;  set; } = "";

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



////////////////////////////////////// mock cli ////////////////////////////////

// extern "C"
// {
// int cli_open()
// {
//     _next_command[0] = 0;
//     _response_lines.clear();
//     return 0;
// }


// int cli_close()
// {
//     // Nothing to do.
//     return 0;
// }


// int cli_printf(const char* format, ...)
// {
//     // Format string.
//     char line[MAX_LINE_LEN];
//     va_list args;
//     va_start(args, format);
//     vsnprintf(line, MAX_LINE_LEN - 1, format, args);
//     va_end(args);

//     std::string str(line);
//     _response_lines.push_back(str);

//     return 0;
// }


// char* cli_gets(char* buff, int len)
// {
//     if (strlen(_next_command) > 0)
//     {
//         strncpy(buff, _next_command, len - 1);
//         return buff;
//     }
//     else
//     {
//         return NULL;
//     }
// }


}