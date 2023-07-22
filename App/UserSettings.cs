using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms.Design;
using System.Drawing.Design;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfUis;
using Ephemera.MidiLib;


namespace Ephemera.Nebulua.App
{
    [Serializable]
    public sealed class UserSettings : SettingsCore
    {
        #region Properties - persisted editable
        [DisplayName("File Log Level")]
        [Description("Log level for file write.")]
        [Browsable(true)]
        public LogLevel FileLogLevel { get; set; } = LogLevel.Trace;
        #endregion

        #region Properties - internal
        [Browsable(false)]
        public bool Valid { get; set; } = false;
        #endregion
    }
}
