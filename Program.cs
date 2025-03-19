using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;


namespace Nebulua
{
    internal class Program
    {
        /// <summary>
        /// The entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());

            //AppDomain currentDomain = AppDomain.CurrentDomain;
            //currentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // // Process cmd line args.
            // switch (args.Length)
            // {
            //     case 0:
            //         ApplicationConfiguration.Initialize();
            //         Application.Run(new MainForm());
            //         break;

            //     case 1:
            //         var scriptFn = args[0];
            //         RealConsole console = new();
            //         var cli = new Cli(scriptFn, console);
            //         cli.Run();
            //         cli.Dispose();
            //         break;

            //     default:
            //         Console.WriteLine("Invalid command line");
            //         break;
            // }
        }
    }
}
