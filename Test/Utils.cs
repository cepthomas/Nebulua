using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;
using Nebulua;
using System.Runtime.CompilerServices;


namespace Test
{
    /// <summary>Hook used to capture events from test target.</summary>
    public class EventCollector
    {
        public List<EventArgs> Events { get; } = [];
        public List<string> Strings { get; } = [];
        public bool AutoRet { get; set; } = false;
        public int Ret { get; set; } = 0;
        public void Reset()
        {
            Events.Clear();
            Strings.Clear();
            AutoRet = false;
            Ret = 0;
        }

        public void CollectEvent(object? sender, EventArgs e)
        {
            // Fix up the event value.
            if (AutoRet) Ret++;

            switch (e)
            {
                case LogArgs le: le.ret = Ret; break;
                case OpenMidiInputArgs le: le.ret = Ret; break;
                case OpenMidiOutputArgs le: le.ret = Ret; break;
                case SendMidiNoteArgs le: le.ret = Ret; break;
                case SendMidiControllerArgs le: le.ret = Ret; break;
                case SetTempoArgs le: le.ret = Ret; break;
                default: /*throw new NotImplementedException();*/ break;
            }

            Events.Add(e);
            Strings.Add(Format(e));
        }

        public EventCollector()
        {
            // Hook script callbacks.
            Interop.Log += CollectEvent;
            Interop.OpenMidiInput += CollectEvent;
            Interop.OpenMidiOutput += CollectEvent;
            Interop.SendMidiNote += CollectEvent;
            Interop.SendMidiController += CollectEvent;
            Interop.SetTempo += CollectEvent;
        }

        public string Format(EventArgs e)
        {
            return e switch
            {
                LogArgs le => $"Log() level:{le.level} msg:{le.msg} ret:{le.ret}",
                OpenMidiInputArgs ie => $"OpenMidiInput() dev_name:{ie.dev_name} chan_num:{ie.chan_num} ret:{ie.ret}",
                OpenMidiOutputArgs oe => $"OpenMidiOutput() dev_name:{oe.dev_name} chan_num: {oe.chan_num} patch:{oe.patch} ret:{oe.ret}",
                SendMidiNoteArgs ne => $"SendMidiNote() chan_hnd:{ne.chan_hnd} note_num:{ne.note_num} volume:{ne.volume} ret:{ne.ret}",
                SendMidiControllerArgs ce => $"SendMidiController() chan_hnd:{ce.chan_hnd} controller:{ce.controller} value:{ce.value} ret:{ce.ret}",
                SetTempoArgs te => $"SetTempo() bpm:{te.bpm} ret:{te.ret}",
                _ => throw new NotImplementedException(),
            };
        }
    }
}
