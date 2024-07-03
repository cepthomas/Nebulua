using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;


namespace Nebulua
{
    internal static class Program
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool FreeConsole();

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var args = Environment.GetCommandLineArgs();

            if (args.Count() > 1)
            {
               using var cli = new Cli(); 
            }
            else
            {
                FreeConsole();
                ApplicationConfiguration.Initialize();
                Application.Run(new MainForm());
            }
        }
    }
}