using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Interop;


namespace Ephemera.Nebulua.CliApp
{
    public class App : IDisposable
    {
        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("App");

        /// <summary>Talks to the user.</summary>
        readonly CommandProc _cmdProc;

        /// <summary>Current script.</summary>
        readonly string _scriptFn = "";

        /// <summary>Resource management.</summary>
        bool _disposed = false;

        /// <summary>Common functionality.</summary>
        Core _core;

        /// <summary>Detect changed script files.</summary>
        readonly MultiFileWatcher _watcher = new();
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
                string? configFn = null; // optional
                var args = StringUtils.SplitByToken(Environment.CommandLine, " ");
                args.RemoveAt(0); // remove the binary

                foreach (var arg in args)
                {
                    if (arg.EndsWith(".ini"))
                    {
                        configFn = arg;
                    }
                    else if (arg.EndsWith(".lua"))
                    {
                        _scriptFn = arg;
                    }
                    else
                    {
                        throw new ApplicationArgumentException($"Invalid command line: {arg}");
                    }
                }

                if (_scriptFn is null) // required
                {
                    throw new ApplicationArgumentException($"Missing nebulua script file");
                }

                // OK so far.
                // Hook loog writes.
                LogManager.LogMessage += LogManager_LogMessage;

                // Create.
                _core = new Core(configFn);
                _core.Run(_scriptFn);

                // Update file watcher. TODO1 also any required files in script.
                _watcher.Add(_scriptFn);

                // State change handler.
                State.Instance.PropertyChangeEvent += State_PropertyChangeEvent;
            }
            // Anything that throws is fatal.
            catch (Exception ex)
            {
                FatalError(ex, "App constructor failed.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void Dispose()
        {
            if (_disposed)
            {
                _core = null;
                // _core.Dispose();
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
            Console.WriteLine("Heeeeer");

            try
            {
                var s = $"Running script file {_scriptFn}";
                _logger.Info(s);
                _cmdProc.Write(s);

                ///// Good to go now. Loop forever doing cmdproc requests. /////

                while (State.Instance.ExecState != ExecState.Exit)
                {
                    // Should not throw. Command processor will take care of its own errors.
                    _cmdProc.Read();
                }

                ///// Normal done. /////

                _cmdProc.Write("shutting down");

                // Wait a bit in case there are some lingering events.
                Thread.Sleep(100);

                //// Just in case.
                //KillAll();
            }
            catch (Exception ex)
            {
                FatalError(ex, "Run() failed");
            }

            return stat;
        }

        /// <summary>
        /// Handler for state changes. Some may originate in this component, others from elsewhere.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void State_PropertyChangeEvent(object? sender, string name)
        {
            switch (name)
            {
                // case "CurrentTick":
                // case "Tempo":

                case "ScriptState":
                    switch (State.Instance.ExecState)
                    {
                        case ExecState.Idle:
                        case ExecState.Run:
                        case ExecState.Exit:
                            break;

                        case ExecState.Kill:
                            // KillAll();
                            State.Instance.ExecState = ExecState.Idle;
                            break;
                    }
                    break;
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
                    _cmdProc.Write(e.Message);
                    // Fatal, shut down.
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
            string serr;

            switch (e)
            {
                case ApiException ex:
                    serr = $"Api Error: {ex.Message}: {info}{Environment.NewLine}{ex.ApiError}";
                    //// Could remove unnecessary detail for user.
                    //int pos = ex.ApiError.IndexOf("stack traceback");
                    //var s = pos > 0 ? StringUtils.Left(ex.ApiError, pos) : ex.ApiError;
                    //serr = $"Api Error: {ex.Message}{Environment.NewLine}{s}";
                    //// Log the detail.
                    //_logger.Debug($">>>>{ex.ApiError}");
                    break;

                case ConfigException ex:
                    serr = $"Config File Error: {ex.Message}: {info}";
                    break;

                case ScriptSyntaxException ex:
                    serr = $"Script Syntax Error: {ex.Message}: {info}";
                    break;

                case ApplicationArgumentException ex:
                    serr = $"Application Argument Error: {ex.Message}: {info}";
                    break;

                default:
                    serr = $"Other error: {e}{Environment.NewLine}{e.StackTrace}";
                    break;
            }

            // This will cause the app to exit.
            _logger.Error(serr);
        }
        #endregion
    }
}
