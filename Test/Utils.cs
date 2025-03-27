using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;
using Nebulua;


namespace Test
{
    /// <summary>Hook used to capture events from test target.</summary>
    public class EventCollector
    {
        public List<EventArgs> CollectedEvents { get; } = [];

        public void CollectEvent(object? sender, EventArgs e)
        {
            CollectedEvents.Add(e);
            //TODO1 like e.ret = 0x0102;
        }

        public EventCollector()
        {
            // Hook script callbacks.
            Interop.Log += CollectEvent;
            Interop.CreateInputChannel += CollectEvent;
            Interop.CreateOutputChannel += CollectEvent;
            Interop.SendNote += CollectEvent;
            Interop.SendController += CollectEvent;
            Interop.SetTempo += CollectEvent;
        }

        public string Format(EventArgs e)
        {
            return e switch
            {
                LogArgs le => $"Log level:{le.level} msg:{le.msg}",
                CreateInputChannelArgs ie => $"CreateInputChannel dev_name:{ie.dev_name} chan_num:{ie.chan_num}",
                CreateOutputChannelArgs oe => $"CreateOutputChannel dev_name:{oe.dev_name} chan_num: {oe.chan_num} patch:{oe.patch}",
                SendNoteArgs ne => $"SendNote chan_hnd:{ne.chan_hnd} note_num:{ne.note_num} volume:{ne.volume}",
                SendControllerArgs ce => $"Send SendController chan_hnd:{ce.chan_hnd} controller:{ce.controller} value:{ce.value}",
                SetTempoArgs te => $"SetTempo Bpm:{te.bpm}",
                _ => throw new NotImplementedException(),
            };
        }
    }
}
