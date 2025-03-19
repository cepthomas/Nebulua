﻿using System;
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
            //AppDomain currentDomain = AppDomain.CurrentDomain;
            //currentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Process cmd line args.
            switch (args.Length)
            {
                case 0:
                    ApplicationConfiguration.Initialize();
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

        //private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        //{
        //}
    }

    public class RealConsole : IConsole
    {
        public bool KeyAvailable { get => Console.KeyAvailable; }
        public bool CursorVisible { get => Console.CursorVisible; set => Console.CursorVisible = value; }
        public string Title { get => Console.Title; set => Console.Title = value; }
        public int BufferWidth { get => Console.BufferWidth; set => Console.BufferWidth = value; }
        public string? ReadLine() { return Console.ReadLine(); }
        public ConsoleKeyInfo ReadKey(bool intercept) { return Console.ReadKey(intercept); }
        public void Write(string text) { Console.Write(text); }
        public void WriteLine(string text) { Console.WriteLine(text); }
        public void SetCursorPosition(int left, int top) { Console.SetCursorPosition(left, top); }
        public (int left, int top) GetCursorPosition() {  return Console.GetCursorPosition(); }
    }
}
