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
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());

            // CLI way:
            // CommandProc cmdproc = new(Console.In, Console.Out);
            // var app = new Nebulua.App(cmdproc);
            // app.Run();
        }
    }
}
