using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Ephemera.NBagOfTricks;


namespace Nebulua
{
    /// <summary>System dynamic values for access by all other components. Some notify clients.</summary>
    public class State // TODO1 delete
    {
        #region Backing fields
        List<(int tick, string name)> _sectionInfo = [];
        int _tempo = 100;
        int _currentTick = 0;
        int _length = 0;
        int _loopStart = -1; // unknown
        int _loopEnd = -1; // unknown
        #endregion

        #region Properties that notify
        /// <summary>Where are we in composition.</summary>
        public int CurrentTick
        {
            get { return _currentTick; }
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
            get { return _tempo; }
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
        /// <summary>Parts of the composition plus total length. If empty, notes are generated dynamically.</summary>
        public List<(int tick, string name)> SectionInfo
        {
            get { return _sectionInfo; }
            set
            {
                // Init internals.
                _sectionInfo = value;
                _length = _sectionInfo.Last().tick;
                ValidateTimes();
                //NotifyStateChanged();
            }
        }

        /// <summary>Total length of the composition.</summary>
        public int Length
        {
            get { return _length; }
        }

        /// <summary>Start of loop region.</summary>
        public int LoopStart
        {
            get { return _loopStart < 0 ? 0 : _loopStart; }
            set { _loopStart = value; ValidateTimes(); }
        }

        /// <summary>End of loop region.</summary>
        public int LoopEnd
        {
            get { return _loopEnd < 0 ? _length : _loopEnd; }
            set { _loopEnd = value; ValidateTimes(); }
        }

        /// <summary>Keep going at end of loop.</summary> 
        public bool DoLoop { get; set; } = false;

        /// <summary>Master volume.</summary>
        public double Volume { get; set; } = 0.8;

        /// <summary>Convenience for readability.</summary>
        public bool IsComposition { get { return _length > 0; } }
        #endregion

        #region Events
        public event EventHandler<string>? ValueChangeEvent;
        public void NotifyStateChanged([CallerMemberName] string name = "")
        {
            ValueChangeEvent?.Invoke(this, name);
        }
        #endregion

        #region Lifecycle
        /// <summary>Prevent client multiple instantiation.</summary>
        State() { }

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

        #region Public functions
        /// <summary>
        /// Convert script version into internal format.
        /// </summary>
        /// <param name="sectInfo"></param>
        public void InitSectionInfo(Dictionary<int, string> sectInfo)
        {
            _sectionInfo.Clear();
            _length = 0;
            _loopStart = -1;
            _loopEnd = -1;
            _currentTick = 0;

            if (sectInfo.Count > 0)
            {
                List<(int tick, string name)> sinfo = [];
                var spos = sectInfo.Keys.OrderBy(k => k).ToList();
                spos.ForEach(sp => _sectionInfo.Add((sp, sectInfo[sp])));

                // Also reset position stuff.
                _length = _sectionInfo.Last().tick;
                ValidateTimes();
            }
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Validate and correct all times. 0 -> loop-start -> loop-end -> length
        /// </summary>
        void ValidateTimes()
        {
            if (IsComposition)
            {
                // Fix loop points.
                int lstart = _loopStart < 0 ? 0 : _loopStart;
                int lend = _loopEnd < 0 ? _length : _loopEnd;
                _loopStart = Math.Min(lstart, _loopEnd);
                _loopEnd = Math.Min(lend, _length);
                _currentTick = MathUtils.Constrain(_currentTick, _loopStart, _loopEnd);
            }
            else // dynamic script
            {
                _loopStart = 0;
                _loopEnd = 0;
                //_currentTick = 0;
            }
        }
        #endregion
    }
}
