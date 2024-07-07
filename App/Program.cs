using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nebulua
{
    internal class Program
    {
        /// <summary>
        ///  The entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Process cmd line args.
            switch (args.Length)
            {
                case 0:
                    {
                        ApplicationConfiguration.Initialize();
                        Application.Run(new MainForm());
                    }
                    break;

                case 1:
                    {
                        var scriptFn = args[0];
                        using var cli = new Cli(scriptFn);
                    }
                    break;

                default:
                    {
                        Console.WriteLine("Invalid command line");
                    }
                    break;
            }
        }
    }
}
