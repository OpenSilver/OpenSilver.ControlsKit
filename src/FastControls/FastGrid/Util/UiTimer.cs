using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using OpenSilver;

namespace FastGrid.FastGrid
{
    public class UiTimer {
        private DispatcherTimer _timer;

        public Action Tick;
        public Func<Task> AsyncTick;
        private bool _running;
        private int _millis;
        private bool _isEnabled;
        private FrameworkElement _fe;
        // only for debugging
        private string _timerName;

        // bug in Simulator -> timer can get called from another thread
        private bool _runningInSimulator;

        public bool IsEnabled {
            get => _isEnabled;
            set {
                if (_isEnabled == value)
                    return;
                _isEnabled = value;
                UpdateTimer();
            } 
        }

        public int IntervalMillis {
            get => (int)_timer.Interval.TotalMilliseconds;
            set => _timer.Interval = TimeSpan.FromMilliseconds(value);
        }

        public UiTimer(FrameworkElement fe, int millis, string timerName) {
            _millis = millis;
            _timerName = timerName;
            _fe = fe;
            _runningInSimulator = Interop.IsRunningInTheSimulator;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(millis) };
            _timer.Tick += async (s, a) => {
                if (_runningInSimulator) {
                    lock (this) {
                        if (_running)
                            return;
                        _running = true;
                    }
                } else {
                    if (_running)
                        return;
                    _running = true;
                }

                if (_runningInSimulator) {
                    // bug in Simulator -> timer can get called from another thread
                    _fe.Dispatcher.BeginInvoke(async () => await RunTick());
                } else {
                    await RunTick();
                }
            };

            _fe.Loaded += (s, a) => UpdateTimer();
            _fe.Unloaded += (s, a) => UpdateTimer();
        }

        private void UpdateTimer() {
            var run = IsEnabled && _fe.IsLoaded;
            if (run)
                StartImpl();
            else
                StopImpl();
        }

        private async Task RunTick() {
            // set only one of the two
            Debug.Assert(!(Tick != null && AsyncTick != null));

            if (_runningInSimulator)
                lock (this)
                    _running = true;
            else
                _running = true;

            try {
                if (Tick != null)
                    Tick();
                else if (AsyncTick != null)
                    await AsyncTick();
            }
            catch (Exception e) {
                Debug.WriteLine($"ERROR: exception in timer Tick : {e}");
            }

            if (_runningInSimulator)
                lock (this)
                    _running = false;
            else
                _running = false;
        }
        public void Start() {
            IsEnabled = true;
        }

        public void Stop() {
            IsEnabled = false;
        }

        private void StartImpl() {
            if (_timer.IsEnabled)
                return;
            Console.WriteLine($"starting timer {_timerName} for {_fe.Name}");
            _timer.Start();
        }

        private void StopImpl() {
            if (!_timer.IsEnabled)
                return;
            Console.WriteLine($"stopping timer {_timerName} for {_fe.Name}");
            _timer.Stop();
        }
    }
}
