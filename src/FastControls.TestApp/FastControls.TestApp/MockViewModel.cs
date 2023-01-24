using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using FastGrid.FastGrid;

namespace FastControls.TestApp
{

    public class Pullout : INotifyPropertyChanged{
        private int operatorRecordId;
        private string operatorReportLabel = "";
        private string password = "";
        private string username = "";
        private string department;
        private string city;
        private int vehicleId;
        private int pulloutId;

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

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public class MockViewModel
    {

        public MockViewModel() {
        }

        public IEnumerable<Pullout> GetPulloutsByCount(int count, int offset = 0) {
            for (int i = offset; i < count + offset; ++i)
                yield return new Pullout {
                    OperatorReportLabel = $"Operator {i}" , 
                    OperatorRecordId = i,
                    VehicleId = i ,
                    Username = $"User {i}",
                    Password = $"Pass {i}",
                    Department = $"Dep {i}",
                    City = $"City {i}",
                };

        }

    }







}
