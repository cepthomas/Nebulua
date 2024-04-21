using System;
using System.Windows.Forms;

namespace Ephemera.Nebulua
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // UI way:
            //ApplicationConfiguration.Initialize();
            //Application.Run(new MainForm());

            // CLI way:
            //CommandProc cmdProc = new(Console.In, Console.Out);
            //var app = new Nebulua.App(cmdProc);
            var app = new Nebulua.App();
            app.Run();
        }
    }
}
