using Nebulua;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Nebulua
{
    public class State : INotifyPropertyChanged
    {
        #region Events
        public event PropertyChangedEventHandler? PropertyChanged;

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        //The PropertyChanged event can indicate all properties on the object have changed by using either
        //null or String.Empty as the property name in the PropertyChangedEventArgs. Note that in a UWP application,
        //String.Empty must be used rather than null.
        void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        #region Properties
        /// <summary>The script execution state.</summary>
        public PlayState ScriptState
        {
            get { return _scriptState; }
            set { if (value != _scriptState) { _scriptState = value; NotifyPropertyChanged(); } }
        }

        /// <summary>The app execution state.</summary>
        public bool AppRunning
        {
            get { return _appRunning; }
            set { if (value != _appRunning) { _appRunning = value; NotifyPropertyChanged(); } }
        }

        /// <summary>Length of composition in ticks.</summary>
        public int Length
        {
            get { return _length; }
            set { if (value != _length) { _length = value; NotifyPropertyChanged(); } }
        }

        /// <summary>Current tempo in bpm.</summary>
        public int Tempo
        {
            get { return _tempo; }
            set { if (value != _tempo) { _tempo = value; NotifyPropertyChanged(); } }
        }

        /// <summary>Where are we in composition.</summary>
        public int CurrentTick
        {
            get { return _currentTick; }
            set { if (value != _currentTick) { _currentTick = value; NotifyPropertyChanged(); } }
        }

        /// <summary>Keep going.</summary>
        public bool DoLoop
        {
            get { return _doLoop; }
            set { if (value != _doLoop) { _doLoop = value; NotifyPropertyChanged(); } }
        }

        /// <summary>Loop start tick. -1 means start of composition.</summary>
        public int LoopStart
        {
            get { return _loopStart; }
            set { if (value != _loopStart) { _loopStart = value; NotifyPropertyChanged(); } }
        }

        /// <summary>Loop end tick. -1 means end of composition.</summary>
        public int LoopEnd
        {
            get { return _loopEnd; }
            set { if (value != _loopEnd) { _loopEnd = value; NotifyPropertyChanged(); } }
        }

        /// <summary>Monitor midi input.</summary>
        public bool MonInput
        {
            get { return _monInput; }
            set { if (value != _monInput) { _monInput = value; NotifyPropertyChanged(); } }
        }

        /// <summary>Monitor midi output.</summary>
        public bool MonOutput
        {
            get { return _monOutput; }
            set { if (value != _monOutput) { _monOutput = value; NotifyPropertyChanged(); } }
        }
        #endregion

        #region Backing fields
        PlayState _scriptState = PlayState.Stop;
        bool _appRunning = true;
        int _length = 0;
        int _tempo = 100;
        int _currentTick = 0;
        bool _monInput = false;
        bool _monOutput = false;
        bool _doLoop = false;
        int _loopStart = -1;
        int _loopEnd = -1;
        #endregion
    }


    // This is a simple customer class that implements the IPropertyChange interface.

    public class DemoCustomer : INotifyPropertyChanged
    {
        // These fields hold the values for the public properties.
        private Guid idValue;
        private string customerNameValue;
        private string phoneNumberValue;

        public event PropertyChangedEventHandler? PropertyChanged;

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        //The PropertyChanged event can indicate all properties on the object have changed by using either
        //null or String.Empty as the property name in the PropertyChangedEventArgs. Note that in a UWP application,
        //String.Empty must be used rather than null.
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // The constructor is private to enforce the factory pattern.
        private DemoCustomer()
        {
            idValue = Guid.NewGuid();
            customerNameValue = "Customer";
            phoneNumberValue = "(312)555-0100";
        }

        // This is the public factory method.
        public static DemoCustomer CreateNewCustomer()
        {
            return new DemoCustomer();
        }

        // This property represents an ID, suitable
        // for use as a primary key in a database.
        public Guid ID
        {
            get { return idValue; }
        }

        public string CustomerName
        {
            get { return customerNameValue; }
            set { if (value != customerNameValue) { customerNameValue = value; NotifyPropertyChanged(); } }
        }

        public string PhoneNumber
        {
            get { return phoneNumberValue; }
            set { if (value != phoneNumberValue) { phoneNumberValue = value; NotifyPropertyChanged(); } }
        }
    }
}
