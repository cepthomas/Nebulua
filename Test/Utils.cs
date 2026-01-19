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
                default: break; // handle?
            }

            Events.Add(e);
            Strings.Add(Format(e));
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
                _ => throw new Exception("???"),
            };
        }


        public class MockConsole //: IConsole
        {
            #region Fields
            readonly StringBuilder _capture = new();
            #endregion

            #region Internals
            public List<string> Capture { get { return StringUtils.SplitByTokens(_capture.ToString(), Environment.NewLine); } }
            public string NextReadLine { get; set; } = "";
            public void Reset() => _capture.Clear();
            #endregion

            #region IConsole implementation
            public bool KeyAvailable { get => NextReadLine.Length > 0; }
            public string Title { get; set; } = "";

            public string? ReadLine()
            {
                if (NextReadLine == "")
                {
                    return null;
                }
                else
                {
                    var ret = NextReadLine;
                    NextReadLine = "";
                    return ret;
                }
            }

            public ConsoleKeyInfo ReadKey(bool intercept)
            {
                if (KeyAvailable)
                {
                    var key = NextReadLine[0];
                    NextReadLine = NextReadLine.Substring(1);
                    return new ConsoleKeyInfo(key, (ConsoleKey)key, false, false, false);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            public void Write(string text) => _capture.Append(text);

            public void WriteLine(string text) => _capture.Append(text + Environment.NewLine);
            #endregion
        }


    }
}
