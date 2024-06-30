using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Nebulua
{
    internal static class Program
    {
        static bool _cli = false;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (_cli)
            {
                using var cli = new Cli();

                // <OutputType>Exe</OutputType>
                // <TargetFramework>net8.0-windows</TargetFramework>
            }
            else
            {
                // To customize application configuration such as set high DPI settings or default font,
                // see https://aka.ms/applicationconfiguration.
                ApplicationConfiguration.Initialize();
                Application.Run(new MainForm());

                // <OutputType>WinExe</OutputType>
                // <TargetFramework>net8.0-windows</TargetFramework>
                // <UseWindowsForms>true</UseWindowsForms>
            }
        }
    }
}