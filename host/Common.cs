using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ephemera.MidiLib;


namespace Ephemera.Nebulua
{
    public class Common
    {
        /// <summary>All the channels - key is user assigned name.</summary>
        public static Dictionary<string, Channel> OutputChannels { get; } = new();

        /// <summary>All the sources - key is user assigned name.</summary>
        public static Dictionary<string, Channel> InputChannels { get; } = new();

        /// <summary>All devices to use for send. Key is my id (not the system driver name).</summary>
        public static Dictionary<string, IOutputDevice> OutputDevices { get; } = new();

        /// <summary>All devices to use for receive. Key is name/id, not the system name.</summary>
        public static Dictionary<string, IInputDevice> InputDevices { get; } = new();
    }
}
