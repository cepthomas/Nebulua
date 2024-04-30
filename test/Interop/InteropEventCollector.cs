using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Ephemera.NBagOfTricks;
using Nebulua.Interop;


namespace Nebulua.Test
{
    /// <summary>Used to capture events from test target.</summary>
    public class InteropEventCollector
    {
        public List<string> CollectedEvents { get; set; }

        readonly Api _api;

        public InteropEventCollector(Api api)
        {
            _api = api;
            CollectedEvents = [];

            // Hook script events.
            Api.CreateChannel += Interop_CreateChannel;
            Api.Send += Interop_Send;
            Api.Log += Interop_Log;
            Api.PropertyChange += Interop_PropertyChange;
        }

        void Interop_CreateChannel(object? sender, CreateChannelArgs e)
        {
            string s = $"CreateChannel DevName:{e.DevName} ChanNum:{e.ChanNum} IsOutput:{e.IsOutput} Patch:{e.Patch}";
            CollectedEvents.Add(s);
            e.Ret = 0x0102;
        }

        void Interop_Send(object? sender, SendArgs e)
        {
            string s = $"Send ChanHnd:{e.ChanHnd} IsNote:{e.IsNote} What:{e.What} Value:{e.Value}";
            CollectedEvents.Add(s);
            e.Ret = (int)NebStatus.Ok;
        }

        void Interop_Log(object? sender, LogArgs e)
        {
            string s = $"Log LogLevel:{e.LogLevel} Msg:{e.Msg}";
            CollectedEvents.Add(s);
            e.Ret = (int)NebStatus.Ok;
        }

        void Interop_PropertyChange(object? sender, PropertyArgs e)
        {
            string s = $"PropertyChange Bpm:{e.Bpm}";
            CollectedEvents.Add(s);
            e.Ret = (int)NebStatus.Ok;
        }
    }
}