using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
using NAudio.Midi;


namespace Nebulua
{
    #region Exports from interop
    public enum NebStatus
    {
        Ok = 0,
        // Api returns these:
        SyntaxError = 10, RunError = 11, ApiError = 12, FileError = 13,
        // App level errors:
        AppInternalError = 20,
    };

    /// <summary>Script creates a channel.</summary>
    public class CreateChannelArgs
    {
        public int Sender;    // unique/opaque id or 0 for generic
        public int Ret;       // handler return value
        public string? DevName;
        public int ChanNum;
        public bool IsOutput; // else input
        public int Patch;     // output only
    }

    /// <summary>Script wants to send a midi event.</summary>
    public class SendArgs
    {
        public int Sender;    // unique/opaque id or 0 for generic
        public int Ret;       // handler return value
        public int ChanHnd;
        public bool IsNote;   // else controller
        public int What;      // note number or controller id
        public int Value;     // note velocity or controller payload
    }

    /// <summary>Script has something to say to host.</summary>
    public class PropertyArgs
    {
        public int Sender;    // unique/opaque id or 0 for generic
        public int Ret;       // handler return value
        public int Bpm;       // Tempo - optional
    }

    /// <summary>Script wants to log something.</summary>
    public class LogArgs
    {
        public int Sender;    // unique/opaque id or 0 for generic
        public int Ret;       // handler return value
        public int LogLevel;
        public string? Msg;
    }
    #endregion

    public class Core
    {
        public Core()
        {
            // // Set up runtime lua environment.
            // var exePath = Environment.CurrentDirectory; // where exe lives
            // _luaPath.Add($@"{exePath}\lua  _code"); // app lua files

            // // Hook script callbacks.
            // Api.CreateChannel += Interop_CreateChannel;
            // Api.Send += Interop_Send;
            // Api.Log += Interop_Log;
            // Api.PropertyChange += Interop_PropertyChange;

            // // State change handler.
            // State.Instance.ValueChangeEvent += State_ValueChangeEvent;
        }

        public void Dispose()
        {

        }

        public NebStatus LoadScript(string? scriptFn = null)
        {
            NebStatus stat = NebStatus.Ok;

            return stat;
        }

        public void InjectReceiveEvent(string devName, int channel, int noteNum, int velocity)
        {
        }

        public void KillAll()
        {
            // Hard reset.
            // State.Instance.ExecState = ExecState.Idle;
        }

        void State_ValueChangeEvent(object? sender, string name)
        {
            //switch (name)
            //{
            //    case "CurrentTick":
            //        break;

            //    case "Tempo":
            //        SetTimer(State.Instance.Tempo);
            //        break;
            //}
        }

        void MmTimer_Callback(double totalElapsed, double periodElapsed)
        {
        }

        void Midi_ReceiveEvent(object? sender, MidiEvent e)
        {
        }

        void Interop_CreateChannel(object? sender, CreateChannelArgs e)
        {
        }

        void Interop_Send(object? _, SendArgs e)
        {
        }

        void Interop_Log(object? sender, LogArgs e)
        {
        }

        void Interop_PropertyChange(object? sender, PropertyArgs e)
        {
        }
    }
}
