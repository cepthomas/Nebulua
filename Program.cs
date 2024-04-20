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
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}


// using System;
// using System.IO;
// using System.Linq;

// internal class Program
// {
//     private static void Main(string[] _)
//     {
//         var app = new Nebulua.App();
//         app.Run();
//     }
// }
