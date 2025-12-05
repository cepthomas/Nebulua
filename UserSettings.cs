using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ephemera.NBagOfTricks;


namespace Nebulua
{
    [Serializable]
    public sealed class UserSettings : SettingsCore
    {
        /// <summary>The current settings.</summary>
        public static UserSettings Current { get; set; } = new();

        #region Properties - persisted editable
        [DisplayName("Script Path")]
        [Description("Default location for user scripts.")]
        [Browsable(true)]
        //[Editor(typeof(FolderNameEditor), typeof(UITypeEditor))]
        public string ScriptPath { get; set; } = "";

        [DisplayName("Open Last File")]
        [Description("Open last file on start.")]
        [Browsable(true)]
        public bool OpenLastFile { get; set; } = true;

        [DisplayName("Auto Reload")]
        [Description("Automatically reload current file when play is pressed if it is changed.")]
        [Browsable(true)]
        public bool AutoReload { get; set; } = true;

        [DisplayName("File Log Level")]
        [Description("Log level for file write.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel FileLogLevel { get; set; } = LogLevel.Trace;

        [DisplayName("Notification Log Level")]
        [Description("Log level for UI notification.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel NotifLogLevel { get; set; } = LogLevel.Debug;

        [DisplayName("Background Color")]
        [Description("Overall background.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color BackColor { get; set; } = Color.LightYellow;

        [DisplayName("Icon Color")]
        [Description("Button icons.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color IconColor { get; set; } = Color.Purple;

        [DisplayName("Active Color")]
        [Description("Active control surfaces.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color ControlColor { get; set; } = Color.DodgerBlue;

        [DisplayName("Selected Color")]
        [Description("The color used for selected controls.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color SelectedColor { get; set; } = Color.Moccasin;
        #endregion

        #region Properties - internal
        [Browsable(false)]
        public bool WordWrap { get; set; } = false;

        [Browsable(false)]
        public bool MonitorRcv { get; set; } = false;

        [Browsable(false)]
        public bool MonitorSnd { get; set; } = false;
        #endregion
    }
}
