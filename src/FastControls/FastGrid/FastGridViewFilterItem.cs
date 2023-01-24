using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace FastGrid.FastGrid
{
    public class FastGridViewFilterItem : INotifyPropertyChanged
    {
        public string PropertyName {
            get => propertyName_;
            set {
                if (value == propertyName_) return;
                propertyName_ = value;
                OnPropertyChanged();
            }
        }

        public object PropertyValue {
            get => propertyValue_;
            set {
                if (Equals(value, propertyValue_)) return;
                propertyValue_ = value;
                OnPropertyChanged();
            }
        }

        public CompareType Compare {
            get => compare_;
            set {
                if (value == compare_) return;
                compare_ = value;
                OnPropertyChanged();
            }
        }

        // ... just in case we end up comparing doubles
        public double Tolerance { get; set; } = 0.0000001;

        private PropertyInfo _propertyInfo;
        private object propertyValue_ = null;
        private string propertyName_ = "";
        private CompareType compare_ = CompareType.Equal;

        public enum CompareType {
            Equal, Different, Less, Bigger, LessOrEqual, BiggerOrEqual, 
            StartsWith, EndsWith, Contains,
        }

        public bool Matches(object obj) {
            if (_propertyInfo == null && PropertyName != "") {
                _propertyInfo = obj.GetType().GetProperty(PropertyName, BindingFlags.Public | BindingFlags.Instance);
            }

            if (_propertyInfo == null)
                return true; // could not get property?

            var objValue = _propertyInfo.GetValue(obj);
            return MatchesValue(objValue, PropertyValue);
        }

        private static double? TryGetDouble(object a) {
            if (a is double)
                return (double)a;
            if (a is float)
                return (float)a;

            return null;
        }
        private static long? TryGetInteger(object a) {
            if (a is int)
                return (int)a;
            if (a is short)
                return (short)a;
            if (a is long)
                return (long)a;
            if (a is byte)
                return (byte)a;
            if (a is char)
                return (char)a;
            return null;
        }
        private static ulong? TryGetUnsignedInteger(object a) {
            if (a is ushort)
                return (ushort)a;
            if (a is uint)
                return (uint)a;
            if (a is ulong)
                return (ulong)a;
            return null;
        }
        private DateTime? TryGetDate(object a) {
            if (a is DateTime)
                return (DateTime)a;
            return null;
        }

        private bool MatchesComparison(bool equal, bool less) {
            switch (Compare) {
            case CompareType.Equal:
                return equal;
            case CompareType.Different:
                return !equal;
            case CompareType.Less:
                return less;
            case CompareType.Bigger:
                return !less && !equal;
            case CompareType.LessOrEqual:
                return less || equal;
            case CompareType.BiggerOrEqual:
                return equal || !less;

            case CompareType.StartsWith:
            case CompareType.EndsWith:
            case CompareType.Contains:
                return false;
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        private bool MatchesDouble(object a, object b) {
            var doubleA = TryGetDouble(a);
            var doubleB = TryGetDouble(b);

            if (doubleA == null) {
                var intA = TryGetInteger(a);
                var uintA = TryGetUnsignedInteger(a);
                if (intA != null)
                    doubleA = intA;
                else if (uintA != null)
                    doubleA = uintA;
            }

            if (doubleB == null) {
                var intB = TryGetInteger(b);
                var uintB = TryGetUnsignedInteger(b);
                if (intB != null)
                    doubleB = intB;
                else if (uintB != null)
                    doubleB = uintB;
            }

            if (doubleA == null || doubleB == null)
                return false; // one could convert to double, one could not
            var equal = Math.Abs(doubleA.Value - doubleB.Value) < Tolerance;
            var less = doubleA.Value < doubleB.Value;
            return MatchesComparison(equal, less);
        }
        private bool MatchesUlong(object a, object b) {
            var intA = TryGetUnsignedInteger(a);
            var intB = TryGetUnsignedInteger(b);
            if (intA == null || intB == null)
                return false;

            var equal = intA.Value == intB.Value;
            var less = intA.Value < intB.Value;
            return MatchesComparison(equal, less);
        }
        private bool MatchesLong(object a, object b) {
            var intA = TryGetInteger(a);
            var intB = TryGetInteger(b);
            if (intA == null || intB == null)
                return false;

            var equal = intA.Value == intB.Value;
            var less = intA.Value < intB.Value;
            return MatchesComparison(equal, less);
        }

        private bool MatchesDate(object a, object b) {
            var dateA = TryGetDate(a);
            var dateB = TryGetDate(b);
            if (dateA == null || dateB == null)
                return false;
            var equal = dateA.Value == dateB.Value;
            var less = dateA.Value == dateB.Value;
            return MatchesComparison(equal, less);
        }

        private bool MatchesString(object a, object b) {
            var stringA = a.ToString();
            var stringB = b.ToString();

            switch (Compare) {
            case CompareType.StartsWith:
                return stringA.StartsWith(stringB);
            case CompareType.EndsWith:
                return stringA.EndsWith(stringB);
            case CompareType.Contains:
                return stringA.IndexOf(stringB) >= 0;
            }

            var compare = String.Compare(stringA, stringB, StringComparison.Ordinal);
            var equal = compare == 0;
            var less = compare < 0;
            return MatchesComparison(equal, less);
        }

        private bool MatchesValue(object objValue, object propertyValue) {
            if (objValue == null || propertyValue == null)
                return objValue == null && propertyValue == null;

            if (TryGetDouble(objValue) != null || TryGetDouble(propertyValue) != null)
                return MatchesDouble(objValue, propertyValue);
            if (TryGetInteger(objValue) != null || TryGetInteger(propertyValue) != null)
                return MatchesLong(objValue, propertyValue);
            if (TryGetUnsignedInteger(objValue) != null || TryGetUnsignedInteger(propertyValue) != null)
                return MatchesUlong(objValue, propertyValue);
            if (TryGetDate(objValue) != null || TryGetDate(propertyValue) != null)
                return MatchesDate(objValue, propertyValue);
            // long ulong double
            // datetime

            // string
            // object - convert to string
            return MatchesString(objValue, propertyValue);
        }


        private void vm_propertyChanged(string name) {
            _propertyInfo = null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            vm_propertyChanged(propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
