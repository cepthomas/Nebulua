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
                        RealConsole console = new();
                        using var cli = new Cli(scriptFn, console);
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

    public class RealConsole : IConsole
    {
        public bool KeyAvailable { get => Console.KeyAvailable; }
        public bool CursorVisible { get => Console.CursorVisible; set => Console.CursorVisible = value; }
        public string Title { get => Console.Title; set => Console.Title = value; }

        public string? ReadLine() { return Console.ReadLine(); }
        public void Write(string text) { Console.Write(text); }
        public void WriteLine(string text) { Console.WriteLine(text); }

        public void SetCursorPosition(int left, int top) { Console.SetCursorPosition(left, top); }

        public (int left, int top) GetCursorPosition() {  return Console.GetCursorPosition(); }
    }
}
