using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Nebulua.Common;
using Nebulua.Interop;


namespace Nebulua.CliApp
{
    public class App : IDisposable
    {
        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("CliApp");

        /// <summary>Talks to the user.</summary>
        readonly CommandProc _cmdProc = new(Console.In, Console.Out);

        /// <summary>Current script.</summary>
        readonly string _scriptFn = "";

        /// <summary>Resource management.</summary>
        bool _disposed = false;

        /// <summary>Common functionality.</summary>
        readonly Core? _core;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor inits stuff.
        /// </summary>
        public App()
        {
            try
            {
                // Set up log.
                string appDir = MiscUtils.GetAppDataDir("Nebulua", "Ephemera");
                string logFileName = Path.Combine(appDir, "applog.txt");
                LogManager.LogMessage += LogManager_LogMessage;
                LogManager.Run(logFileName, 100000);
                _logger.Debug($"CliApp.CliApp() this={GetHashCode()}");

                // Process cmd line args.
                var args = Environment.GetCommandLineArgs();
                if (args.Length == 2 && args[1].EndsWith(".lua") && Path.Exists(args[1]))
                {
                    _scriptFn = args[1];
                }
                else
                {
                    throw new ApplicationArgumentException($"Invalid nebulua script file: {args[1]}");
                }

                // OK so far. Assemble the engine.
                _cmdProc.Write("Greetings from Nebulua!");
                _core = new Core();
                State.Instance.ValueChangeEvent += State_ValueChangeEvent;

                _logger.Debug($"CliApp.CliApp()2 this={GetHashCode()} _core={_core.GetHashCode()}");

                // Run it.
                var s = $"Running script file {_scriptFn}";
                _logger.Info(s);
                _cmdProc.Write(s);
                _core.RunScript(_scriptFn);

                // Loop forever doing cmdproc requests.
                while (State.Instance.ExecState != ExecState.Exit)
                {
                    // Should not throw. Command processor will take care of its own errors.
                    _cmdProc.Read();
                }

                // Normal done. Wait a bit in case there are some lingering events.
                Thread.Sleep(100);
            }
            // Anything that throws is fatal.
            catch (Exception ex)
            {
                FatalError(ex, "App() failed.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            _logger.Debug($"CliApp.Dispose()1 this={this.GetHashCode()} _core={_core?.GetHashCode()}");

            if (!_disposed)
            {
                _core?.Dispose();
                _disposed = true;
            }

            GC.SuppressFinalize(this);

            _logger.Debug($"CliApp.Dispose()2 this={this.GetHashCode()} _core={_core?.GetHashCode()}");
        }

        /// <summary>Gotta start somewhere.</summary>
        static void Main()
        {
            using var app = new App(); // guarantees Dispose()
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Handler for state changes
        /// </summary>
        /// <param name="_"></param>
        /// <param name="name">Specific State value.</param>
        void State_ValueChangeEvent(object? _, string name)
        {
            if (name == "CurrentTick")
            {
                // TODO display running tick/bartime somewhere in console?
            }
        }

        /// <summary>
        /// Capture bad events and display them to the user. If error shut down.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogManager_LogMessage(object? sender, LogMessageEventArgs e)
        {
            switch (e.Level)
            {
                case LogLevel.Error:
                    // Fatal error, shut down.
                    _cmdProc.Write($"Fatal shutting down: {Environment.NewLine}{e.Message}");
                    State.Instance.ExecState = ExecState.Exit;
                    Environment.Exit(1);
                    break;

                case LogLevel.Warn:
                    _cmdProc.Write(e.Message);
                    break;

                default:
                    // ignore
                    break;
            }
        }

        /// <summary>
        /// General purpose handler for fatal errors. Causes app exit.
        /// </summary>
        /// <param name="ex">The exception</param>
        /// <param name="info">Extra info</param>
        void FatalError(Exception e, string info)
        {
            State.Instance.ExecState = ExecState.Dead;
            string serr = e switch
            {
                ApiException ex => $"Api Error: {ex.Message}: {info}{Environment.NewLine}{ex.ApiError}",
                ConfigException ex => $"Config File Error: {ex.Message}: {info}",
                ScriptSyntaxException ex => $"Script Syntax Error: {ex.Message}: {info}",
                ApplicationArgumentException ex => $"Application Argument Error: {ex.Message}: {info}",
                _ => $"Other error: {e}{Environment.NewLine}{e.StackTrace}",
            };

            // Logging an error will cause the app to exit.
            _logger.Error(serr);
        }
        #endregion
    }
}
