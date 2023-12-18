using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NAudio.Midi;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfTricks;
using Ephemera.MidiLib;
using KeraLuaEx;


namespace Ephemera.Nebulua
{
    public partial class Script
    {
        #region Host required to implement
        /// <summary>
        /// Handle script exception.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        static bool ErrorHandler(Exception e)
        {
            // Do something with this.
            if (_instance!._l.ThrowOnError)
            {
                throw e;
            }
            else
            {
                _logger.Error(e.Message);
                _logger.Error(e.StackTrace ?? "No stack");
            }
            return false;
        }
        #endregion

        #region Work functions for lua-calls-host - see api_spec.lua for documentation
        static bool Log_Work(int? level, string? msg)
        {
            // Do the work.
            _logger.Log((LogLevel)level!, msg ?? "");

            return true;
        }

        static bool SendNote_Work(string? channel, int? notenum, double? volume, double? dur = 0.1)
        {
            // Validate.
            if (channel is null || !Common.InputChannels.ContainsKey(channel))
            {
                throw new ArgumentException($"Invalid channel: {channel}");
            }
            if (notenum is null || volume is null || dur is null)
            {
                throw new ArgumentException($"Null argument arg");
            }

            // Do the work.
            var ch = Common.InputChannels[channel];
            int absnote = MathUtils.Constrain(Math.Abs((int)notenum!), MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);

            // If vol is positive it's note on else note off.
            if (volume > 0)
            {
                double vel = ch.NextVol((double)volume!) * _instance!._masterVolume;
                int velPlay = (int)(vel * MidiDefs.MAX_MIDI);
                velPlay = MathUtils.Constrain(velPlay, MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);

                //NoteOnEvent evt = new(StepTime.TotalSubbeats, ch.ChannelNumber, absnote, velPlay, dur.TotalSubbeats);TODO
                NoteOnEvent evt = new(9999, ch.ChannelNumber, absnote, velPlay, 8888);
                ch.SendEvent(evt);
            }
            else
            {
                //NoteEvent evt = new(StepTime.TotalSubbeats, ch.ChannelNumber, MidiCommandCode.NoteOff, absnote, 0);TODO
                NoteEvent evt = new(9999, ch.ChannelNumber, MidiCommandCode.NoteOff, absnote, 0);
                ch.SendEvent(evt);
            }

            // Return results.
            return true;
        }

        static bool SendNoteOn_Work(string? channel, int? notenum, double? volume)
        {
            // Do the work.
            SendNote_Work(channel, notenum, volume);

            // Return results.
            return true;
        }

        static bool SendNoteOff_Work(string? channel, int? notenum)
        {
            // Do the work.
            SendNote_Work(channel, notenum, 0);

            // Return results.
            return true;
        }

        static bool SendController_Work(string? channel, int? ctlr, int? value)
        {
            // Validate.
            if (channel is null || !Common.InputChannels.ContainsKey(channel))
            {
                throw new ArgumentException($"Invalid channel: {channel}");
            }
            if (ctlr is null || value is null)
            {
                throw new ArgumentException($"Null argument arg");
            }

            // Do the work.
            var ch = Common.OutputChannels[channel];
            ch.SendController((MidiController)ctlr, (int)value);

            // Return results.
            return true;
        }

        static bool SendPatch_Work(string? channel, int? patch)
        {
            // Validate.
            if (channel is null || !Common.InputChannels.ContainsKey(channel))
            {
                throw new ArgumentException($"Invalid channel: {channel}");
            }
            if (patch is null)
            {
                throw new ArgumentException($"Null argument arg");
            }

            // Do the work.
            var ch = Common.OutputChannels[channel];
            ch.Patch = (int)patch!;
            ch.SendPatch();

            // Return results.
            return true;
        }
        #endregion
    }
}
