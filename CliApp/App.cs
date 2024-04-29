using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Nebulua.Common;
using Nebulua.Interop;


// TODO display running tick/bartime somewhere in console?


namespace Nebulua.CliApp
{
    public class App : IDisposable
    {
        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("CliApp");

        /// <summary>Talks to the user.</summary>
        readonly CommandProc _cmdProc;

        /// <summary>Current script.</summary>
        readonly string _scriptFn = "";

        /// <summary>Resource management.</summary>
        bool _disposed = false;

        /// <summary>Common functionality.</summary>
        Core? _core;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor inits stuff.
        /// </summary>
        public App()
        {
            _cmdProc = new(Console.In, Console.Out);
            _cmdProc.Write("Greetings from Nebulua!");

            try
            {
                // Process cmd line args.
                string? configFn = null;
                var args = StringUtils.SplitByToken(Environment.CommandLine, " ");
                args.RemoveAt(0); // remove the binary

                // Hook loog writes.
                LogManager.LogMessage += LogManager_LogMessage;

                foreach (var arg in args)
                {
                    if (arg.EndsWith(".ini")) // optional
                    {
                        configFn = arg;
                    }
                    else if (arg.EndsWith(".lua")) // required
                    {
                        _scriptFn = arg;
                    }
                    else
                    {
                        throw new ApplicationArgumentException($"Invalid command line: {arg}");
                    }
                }

                if (_scriptFn is null)
                {
                    throw new ApplicationArgumentException($"Missing nebulua script file");
                }

                // OK so far. Assemble the engine.
                _core = new Core(configFn);
                _core.RunScript(_scriptFn);
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
        /// <exception cref="NotImplementedException"></exception>
        public void Dispose()
        {
            if (!_disposed)
            {
                _core?.Dispose();
                _disposed = true;
            }
        }
        #endregion

        #region Primary workers
        /// <summary>
        /// Execute the script. Doesn't return until complete.
        /// </summary>
        public NebStatus Run()
        {
            NebStatus stat = NebStatus.Ok;

            try
            {
                var s = $"Running script file {_scriptFn}";
                _logger.Info(s);
                _cmdProc.Write(s);

                // Good to go now. Loop forever doing cmdproc requests.
                while (State.Instance.ExecState != ExecState.Exit)
                {
                    // Should not throw. Command processor will take care of its own errors.
                    _cmdProc.Read();
                }

                // Normal done.
                _cmdProc.Write("shutting down");

                // Wait a bit in case there are some lingering events.
                Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                FatalError(ex, "Run() failed");
            }

            return stat;
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
                    _cmdProc.Write($"Fatal, shutting down:{Environment.NewLine}{e.Message}");
                    State.Instance.ExecState = ExecState.Exit;
                    break;

                case LogLevel.Warn:
                    _cmdProc.Write(e.Message);
                    break;

                default:
                    // ignore
                    break;
            }
        }
        #endregion

        #region Private functions
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

            // Logging error will cause the app to exit.
            _logger.Error(serr);
        }
        #endregion
    }
}
