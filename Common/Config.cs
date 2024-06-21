using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
//using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace Nebulua.Common
{
    public class Config
    {
        #region Fields
        /// <summary>Look up table for log level.</summary>
        readonly Dictionary<string, LogLevel> xlatLevel = new()
        {
            { "trace", LogLevel.Trace }, { "debug", LogLevel.Debug },
            { "info", LogLevel.Info }, { "warn", LogLevel.Warn }, { "error", LogLevel.Error }
        };

        /// <summary>Look up table for truth.</summary>
        readonly Dictionary<string, bool> xlatBoolean = new()
        {
            { "true", true }, { "on", true }, { "1", true },
            { "false", true }, { "off", true }, { "0", true },
        };
        #endregion

        #region Properties
        /// <summary>CLI prompt.</summary>
        public string Prompt { get { return _prompt; } }
        readonly string _prompt = ">";

        /// <summary>Log file.</summary>
        public string LogFilename { get { return _logFn; } }
        readonly string _logFn = "_log.txt";

        /// <summary>Min level to log to file.</summary>
        public LogLevel FileLevel { get { return _fileLevel; } }
        readonly LogLevel _fileLevel = LogLevel.Debug;

        /// <summary>Min level to log to user.</summary>
        public LogLevel NotifLevel { get { return _notifLevel; } }
        readonly LogLevel _notifLevel = LogLevel.Warn;
        #endregion

        /// <summary>
        /// Constructor inits stuff. Throws exceptions for invalid entries.
        /// </summary>
        public Config(string? configFn) //TODO1 UserSettings?
        {
            if (configFn is not null)
            {
                foreach (var item in File.ReadLines(configFn))
                {
                    var sitem = item.Trim();
                    if (!sitem.StartsWith('#') && sitem.Length > 0) // ignore comments and empty lines
                    {
                        var parts = StringUtils.SplitByTokens(sitem, "=");
                        if (parts.Count == 2)
                        {
                            switch (parts[0].ToLower())
                            {
                                case "log_filename":
                                    _logFn = parts[1];
                                    break;

                                case "log_to_file":
                                    if (!xlatLevel.TryGetValue(parts[1].ToLower(), out _fileLevel))
                                    {
                                        throw new ConfigException($"Invalid log_to_file value: {parts[1]}");
                                    }
                                    break;

                                case "log_to_notif":
                                    if (!xlatLevel.TryGetValue(parts[1].ToLower(), out _notifLevel))
                                    {
                                        throw new ConfigException($"Invalid log_to_notif value: {parts[1]}");
                                    }
                                    break;

                                case "cli_prompt":
                                    _prompt = parts[1];
                                    break;

                                case "mon_midi_rcv":
                                    if (!xlatBoolean.TryGetValue(parts[1].ToLower(), out bool br))
                                    {
                                        throw new ConfigException($"Invalid mon_midi_rcv value: {parts[1]}");
                                    }
                                    else
                                    {
                                        State.Instance.MonRcv = br;
                                    }
                                    break;

                                case "mon_midi_snd":
                                    if (!xlatBoolean.TryGetValue(parts[1].ToLower(), out bool bs))
                                    {
                                        throw new ConfigException($"Invalid mon_midi_snd value: {parts[1]}");
                                    }
                                    else
                                    {
                                        State.Instance.MonSnd = bs;
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            throw new ConfigException($"Invalid config line: {sitem}");
                        }
                    }
                }
            }
        }
    }
}
