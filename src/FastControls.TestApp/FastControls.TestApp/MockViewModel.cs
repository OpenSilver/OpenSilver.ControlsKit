using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using FastGrid.FastGrid;
using OpenSilver.ControlsKit.Annotations;

namespace FastControls.TestApp
{
    public class DummyDate : INotifyPropertyChanged{
        private int operatorRecordId;
        private string operatorReportLabel = "";
        private string password = "";
        private string username = "";
        private string department;
        private string city;
        private int vehicleId;
        private int pulloutId;
        private DateTime time_ = DateTime.Now;

        public int PulloutId {
            get => pulloutId;
            set {
                if (value == pulloutId) return;
                pulloutId = value;
                OnPropertyChanged();
            }
        }

        // must match the Operator.ReportLabel
        public string OperatorReportLabel {
            get => operatorReportLabel;
            set {
                if (value == operatorReportLabel) return;
                operatorReportLabel = value;
                OnPropertyChanged();
            }
        }

        public int OperatorRecordId {
            get => operatorRecordId;
            set {
                if (value == operatorRecordId) return;
                operatorRecordId = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BgColor));
                OnPropertyChanged(nameof(IsActive));
            }
        }

        public string Username
        {
            get => username;
            set
            {
                if (value == username) return;
                username = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get => password;
            set
            {
                if (value == password) return;
                password = value;
                OnPropertyChanged();
            }
        }

        public string Department
        {
            get => department;
            set
            {
                if (value == department) return;
                department = value;
                OnPropertyChanged();
            }
        }

        public string City
        {
            get => city;
            set
            {
                if (value == city) return;
                city = value;
                OnPropertyChanged();
            }
        }

        public DateTime Time {
            get => time_;
            set {
                if (value.Equals(time_)) return;
                time_ = value;
                OnPropertyChanged();
            }
        }

        public string TimeString => Time.ToString("HH:mm");

        public Brush BgColor {
            get {
                switch (OperatorRecordId % 4) {
                case 0: return BrushCache.Inst.GetByColor(Colors.CornflowerBlue);
                case 1: return BrushCache.Inst.GetByColor(Colors.DodgerBlue);
                case 2: return BrushCache.Inst.GetByColor(Colors.LightSeaGreen);
                case 3: return BrushCache.Inst.GetByColor(Colors.DarkSlateBlue);
                default:
                    Debug.Assert(false);
                    return BrushCache.Inst.GetByColor(Colors.DarkSlateBlue);
                }
            }
        }

        public int VehicleId {
            get => vehicleId;
            set {
                if (value == vehicleId) return;
                vehicleId = value;
                OnPropertyChanged();
            }
        }

        public bool IsActive => (OperatorRecordId % 10) == 0;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public class MockViewModel
    {

        public MockViewModel() {
        }

        public IEnumerable<DummyDate> GetPulloutsByCount(int count, int offset = 0) {
            var time = DateTime.Now;
            var incrementTimeSecs = 60;
            for (int i = offset; i < count + offset; ++i) {
                yield return new DummyDate {
                    OperatorReportLabel = $"Operator {i}" , 
                    OperatorRecordId = i,
                    VehicleId = i ,
                    Username = $"User {i}",
                    Password = $"Pass {i}",
                    Department = $"Dep {i}",
                    City = $"City {i}",
                    Time = time,
                };
                time = time.AddSeconds(incrementTimeSecs);
            }

        }

        public IEnumerable<DummyDate> GetPulloutsByCountForTestingFilter(int count, int offset = 0) {
            var time = DateTime.Now;
            var incrementTimeSecs = 10;
            for (int i = offset; i < count + offset; ++i) {
                yield return new DummyDate {
                    OperatorReportLabel = $"Operator {i % 250}" , 
                    OperatorRecordId = i % 97,
                    VehicleId = i ,
                    Username = $"User {i % 29}",
                    Password = $"Pass {i % 37}",
                    Department = $"Dep {i % 61}",
                    City = $"City {i % 23}",
                    Time = time,
                };
                time = time.AddSeconds(incrementTimeSecs);
            }


        }

    }



}
