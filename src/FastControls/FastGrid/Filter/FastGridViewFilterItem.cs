using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Media;
using FastGrid.FastGrid.Filter;

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

        // the idea: "AsString" - I only want it for fast comparison when I need to edit the UI
        // (so I will know what's checked and what's not checked)
        public IReadOnlyList<(string AsString, object OriginalValue)> PropertyValues {
            get => propertyValues_;
            set {
                if (Equals(value, propertyValues_)) return;
                propertyValues_ = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Color));
            }
        }

        public enum CompareType {
            Equal, Different, Less, Bigger, LessOrEqual, BiggerOrEqual, 
            InBetween,
            StartsWith, EndsWith, Contains,
        }
        public IReadOnlyList<string> CompareList = new[] {
            "Equal to",
            "Different from", "Less than", "Bigger than", "Less or Equal", "Bigger or Equal",
            "In Between",
            "Starts with", "Ends with", "Contains",
        };

        public CompareType Compare {
            get => compare_;
            set {
                if (value == compare_) return;
                compare_ = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CompareIdx));
                OnPropertyChanged(nameof(IsInBetween));
                OnPropertyChanged(nameof(IsNotInBetween));
            }
        }

        public int CompareIdx {
            get => (int)Compare;
            set => Compare = (CompareType)value;
        }

        public bool IsInBetween => Compare == CompareType.InBetween;
        public bool IsNotInBetween => Compare != CompareType.InBetween;


        // in case the user is manually filling what to compare against
        public string CompareToValue {
            get => compareToValue_;
            set {
                if (value == compareToValue_) return;
                compareToValue_ = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Color));
            }
        }

        // in case the user is manually filling what to compare against (for "in-between")
        public string CompareToValue2 {
            get => compareToValue2_;
            set {
                if (value == compareToValue2_) return;
                compareToValue2_ = value;
                OnPropertyChanged();
            }
        }

        public void RefreshFilter() {
            OnPropertyChanged("ForceRefreshFilter");
        }

        public bool IsEmpty => PropertyValues.Count == 0 && CompareToValue == "";
        public bool IsUsed => PropertyValues.Count > 0 || CompareToValue != "";

        // used to color the filter
        public Brush Color => new SolidColorBrush(IsUsed ? Colors.DarkOrange : System.Windows.Media.Color.FromArgb(0xaf, 0x80, 0x80, 0x80));

        // ... just in case we end up comparing doubles
        public PropertyValueCompareEquivalent CompareEquivalent { get; } = new PropertyValueCompareEquivalent();

        private PropertyInfo _propertyInfo;
        private IReadOnlyList<(string, object)> propertyValues_ = new List<(string, object)>();
        private string propertyName_ = "";
        private CompareType compare_ = CompareType.Equal;
        private bool caseSensitive_ = false;
        private string compareToValue_ = "";
        private string compareToValue2_ = "";

        // for string comparisons
        public bool CaseSensitive {
            get => caseSensitive_;
            set {
                if (value == caseSensitive_) return;
                caseSensitive_ = value;
                OnPropertyChanged();
            }
        }

        private bool recomputeObjectValue_ = false;
        private bool recomputeObjectValue2_ = false;
        private object compareToObjectValue_ = null;
        private object compareToObjectValue2_ = null;

        // returns null if we can't convert
        private object TryConvertStringToObject(string s, object obj) {
            if (obj is double) {
                if (double.TryParse(s, out var value))
                    return value;
            }
            if (obj is float){
                if (float.TryParse(s, out var value))
                    return value;
            }
            if (obj is decimal){
                if (decimal.TryParse(s, out var value))
                    return value;
            }
            if (obj is int){
                if (int.TryParse(s, out var value))
                    return value;
            }
            if (obj is short){
                if (short.TryParse(s, out var value))
                    return value;
            }
            if (obj is long){
                if (long.TryParse(s, out var value))
                    return value;
            }
            if (obj is byte){
                if (byte.TryParse(s, out var value))
                    return value;
            }
            if (obj is char){
                if (char.TryParse(s, out var value))
                    return value;
            }
            if (obj is ushort){
                if (ushort.TryParse(s, out var value))
                    return value;
            }
            if (obj is uint){
                if (uint.TryParse(s, out var value))
                    return value;
            }
            if (obj is ulong){
                if (ulong.TryParse(s, out var value))
                    return value;
            }
            if (obj is DateTime){
                if (DateTime.TryParse(s, out var value))
                    return value;
                if (DateTime.TryParseExact(s, CompareEquivalent.DateTimeFormat, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var dt))
                    return dt;
            }

            if (obj is string)
                return s;

            return null;
        }

        private object CompareToObjectValue(object o) {
            if (recomputeObjectValue_) {
                recomputeObjectValue_ = false;
                compareToObjectValue_ = TryConvertStringToObject(CompareToValue, o);
            }
            return compareToObjectValue_;
        }

        private object CompareToObjectValue2(object o) {
            if (recomputeObjectValue2_) {
                recomputeObjectValue2_ = false;
                compareToObjectValue2_ = TryConvertStringToObject(CompareToValue2, o);
            }
            return compareToObjectValue2_;
        }

        public bool Matches(object obj) {
            if (IsEmpty)
                return true;

            if (_propertyInfo == null && PropertyName != "") {
                _propertyInfo = obj.GetType().GetProperty(PropertyName, BindingFlags.Public | BindingFlags.Instance);
            }

            if (_propertyInfo == null)
                return true; // could not get property?

            var objValue = _propertyInfo.GetValue(obj);
            if (CompareToValue != "") {
                var canCompare = CompareToObjectValue(objValue) != null;
                if (Compare == CompareType.InBetween)
                    canCompare = canCompare && CompareToObjectValue2(objValue) != null;
                if (!canCompare)
                    // the idea - if any strings can't be converted to what we need, ignore the filter
                    return true;

                bool ok;
                if (Compare != CompareType.InBetween)
                    ok = MatchesValue(objValue, CompareToObjectValue(objValue));
                else
                    ok = MatchesValue(objValue, CompareToObjectValue2(objValue)) && MatchesValue(CompareToObjectValue(objValue), objValue);
                return ok;
            }

            if (PropertyValues.Count == 1)
                return MatchesValue(objValue, PropertyValues[0].OriginalValue);

            // several values - it's an OR
            foreach (var pv in PropertyValues)
                if (MatchesValue(objValue, pv.OriginalValue))
                    return true;
            return false;
        }

        private static double? TryGetDouble(object a) {
            if (a is double)
                return (double)a;
            if (a is float)
                return (float)a;
            if (a is decimal)
                return Convert.ToDouble((decimal)a);

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
            var compare = Compare;
            if (CompareToValue == "")
                compare = CompareType.Equal;

            switch (compare) {
            case CompareType.Equal:
                return equal;
            case CompareType.Different:
                return !equal;
            case CompareType.Less:
                return less;
            case CompareType.Bigger:
                return !less && !equal;

            case CompareType.InBetween:
                // for In Between -> I will do 2 "<=" comparisons
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
            var equal = Math.Abs(doubleA.Value - doubleB.Value) < CompareEquivalent.DoubleTolerance;
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
            var equal = CompareEquivalent.DateTimeToLongHash(dateA.Value)  == CompareEquivalent.DateTimeToLongHash(dateB.Value) ;
            var less = dateA.Value < dateB.Value;
            return MatchesComparison(equal, less);
        }

        private bool MatchesString(object a, object b) {
            var stringA = a.ToString();
            var stringB = b.ToString();

            var comparison = CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            switch (Compare) {
            case CompareType.StartsWith:
                return stringA.StartsWith(stringB, comparison);
            case CompareType.EndsWith:
                return stringA.EndsWith(stringB, comparison);
            case CompareType.Contains:
                return stringA.IndexOf(stringB, comparison) >= 0;
            }

            var compare = String.Compare(stringA, stringB, comparison);
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
            return MatchesString(objValue, propertyValue);
        }


        private void vm_propertyChanged(string name) {
            _propertyInfo = null;
            switch (name) {
            case "CompareToValue":
                // force recompute
                recomputeObjectValue_ = true;
                break;
            case "CompareToValue2": 
                // force recompute
                recomputeObjectValue2_ = true;
                break;
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            vm_propertyChanged(propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null) {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

    }
}
