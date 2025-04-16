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
            Utils.TimeIt.Snap("Main() entry");

            // Process cmd line args.
            switch (args.Length)
            {
                case 0:
                    ApplicationConfiguration.Initialize();
                    Utils.TimeIt.Snap("Application.Run()");
                    Application.Run(new MainForm());
                    break;

                case 1:
                    var scriptFn = args[0];
                    RealConsole console = new();
                    var cli = new Cli(scriptFn, console);
                    cli.Run();
                    cli.Dispose();
                    break;

                default:
                    Console.WriteLine("Invalid command line");
                    break;
            }
        }
    }
}
