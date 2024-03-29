﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;


namespace Nebulua
{
    /// <summary>System globals. Some notify clients.</summary>
    public class State
    {
        #region Properties
        public ExecState ExecState
        {
            get { return _execState; }
            set { if (value != _execState) { _execState = value; NotifyStateChanged(); } }
        }
        ExecState _execState;

        /// <summary>Current tempo in bpm.</summary>
        public int Tempo
        {
            get { return _tempo; }
            set { if (value != _tempo) { _tempo = value; NotifyStateChanged(); } }
        }
        int _tempo = 100;

        /// <summary>Where are we in composition.</summary>
        public int CurrentTick
        {
            get { return _currentTick; }
            set { if (value != _currentTick) { _currentTick = value; NotifyStateChanged(); } }
        }
        int _currentTick = 0;

        /// <summary>Length of composition in ticks.</summary>
        public int Length { get; set; }

        /// <summary>Keep going at end of loop.</summary>
        public bool DoLoop { get; set; }

        /// <summary>Loop start tick. -1 means start of composition.</summary>
        public int LoopStart { get; set; }

        /// <summary>Loop end tick. -1 means end of composition.</summary>
        public int LoopEnd { get; set; }

        /// <summary>Monitor midi input.</summary>
        public bool MonInput { get; set; }

        /// <summary>Monitor midi output.</summary>
        public bool MonOutput { get; set; }
        #endregion

        #region Events
        public event EventHandler<string>? PropertyChangeEvent;
        public void NotifyStateChanged([CallerMemberName] string name = "")
        {
            PropertyChangeEvent?.Invoke(this, name);
        }
        #endregion

        #region Lifecycle
        /// <summary>Prevent client instantiation.</summary>
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
    }
}
