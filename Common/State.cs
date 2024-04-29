using Ephemera.NBagOfTricks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;


namespace Nebulua.Common
{
    #region Enums
    /// <summary>Internal status.</summary>
    public enum ExecState { Idle, Run, Kill, Exit, Dead, Reload }
    #endregion

    /// <summary>System globals. Some notify clients.</summary>
    public class State
    {
        #region Backing fields
        ExecState _execState;
        List<(int tick, string name)> _sectionInfo = [];
        int _tempo = 100;
        int _currentTick = 0;
        int _length = 0;
        int _loopStart = -1;
        int _loopEnd = -1;
        #endregion

        #region Properties that notify
        public ExecState ExecState
        {
            get
            {
                return _execState;
            }
            set
            {
                if (value != _execState)
                {
                    _execState = value;
                    NotifyStateChanged();
                }
            }
        }

        /// <summary>Where are we in composition.</summary>
        public int CurrentTick
        {
            get
            {
                return _currentTick;
            }
            set
            {
                _currentTick = value;
                ValidateTimes();
                NotifyStateChanged();
            }
        }

        /// <summary>Current tempo in bpm.</summary>
        public int Tempo
        {
            get
            {
                return _tempo;
            }
            set
            {
                if (value != _tempo)
                {
                    _tempo = value;
                    NotifyStateChanged();
                }
            }
        }
        #endregion

        #region Properties that don't notify
        /// <summary>Parts of the composition plus total length.</summary>
        public List<(int tick, string name)> SectionInfo
        {
            get
            {
                return _sectionInfo;
            }
            set
            {
                // Init internals.
                _sectionInfo = value;
                _length = _sectionInfo.Last().tick;
                //_loopStart = 0;
                //_loopEnd = _length;
                ValidateTimes();
                //NotifyStateChanged();
            }
        }

        /// <summary>Total length of the composition.</summary>
        public int Length
        {
            get
            {
                return _length;
            }
        }

        /// <summary>Start of loop region.</summary>
        public int LoopStart
        {
            get
            {
                return _loopStart < 0 ? 0 : _loopStart;
            }
            set
            {
                _loopStart = value;
                ValidateTimes();
                //NotifyStateChanged();
            }
        }

        /// <summary>End of loop region.</summary>
        public int LoopEnd
        {
            get
            {
                return _loopEnd < 0 ? _length : _loopEnd;
            }
            set
            {
                _loopEnd = value;
                ValidateTimes();
                //NotifyStateChanged();
            }
        }

        /// <summary>Keep going at end of loop.</summary> 
        public bool DoLoop { get; set; } = false;

        /// <summary>Master volume.</summary>
        public double Volume { get; set; } = 0.8;

        /// <summary>Monitor midi input.</summary>
        public bool MonRcv { get; set; } = false;

        /// <summary>Monitor midi output.</summary>
        public bool MonSnd { get; set; } = false;
        #endregion

        #region Events
        public event EventHandler<string>? ValueChangeEvent;
        public void NotifyStateChanged([CallerMemberName] string name = "")
        {
            ValueChangeEvent?.Invoke(this, name);
        }
        #endregion

        #region Lifecycle
        /// <summary>Prevent client instantiation.</summary>
        State()
        {
        }

        /// <summary>The singleton instance.</summary>
        public static State Instance
        {
            get
            {
                _instance ??= new State();
                return _instance;
            }
        }

        /// <summary>The singleton instance.</summary>
        static State? _instance;
        #endregion

        #region Private functions
        /// <summary>
        /// Validate and correct all times.
        /// </summary>
        void ValidateTimes()
        {
            // 0 -> loop-start -> loop-end -> length

            // Must have this.
            if (_length <= 0)
            {
                throw new ScriptSyntaxException("Length not set");
            }

            // Fix loop points.
            int lstart = _loopStart < 0 ? 0 : _loopStart;
            int lend = _loopEnd < 0 ? _length : _loopStart;
            lstart = Math.Min(lstart, _loopEnd);
            lend = Math.Min(lend, _length);
            _currentTick = MathUtils.Constrain(_currentTick, lstart, lend);
        }
        #endregion
    }
}
