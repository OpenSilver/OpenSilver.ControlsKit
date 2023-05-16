using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FastGrid.FastGrid.Filter
{
    internal static class FastGridViewFilterUtil
    {
        private class Number {
            // keep it as double to encompass all number - i only want this for the end sorting
            public double AsDouble;
            public object OriginalNumber;
            public string AsString;
        }


        private static bool IsNumber(object a) {
            if (a is double)
                return true;
            if (a is float)
                return true;
            if (a is int)
                return true;
            if (a is short)
                return true;
            if (a is long)
                return true;
            if (a is byte)
                return true;
            if (a is char)
                return true;
            if (a is ushort)
                return true;
            if (a is uint)
                return true;
            if (a is ulong)
                return true;
            if (a is decimal)
                return true;

            return false;
        }

        private static string NumberToString(object a) {
            if (a is double)
                // simple, for now
                return ((double)a).ToString("F3");
            if (a is float)
                // simple, for now
                return ((float)a).ToString("F3");
            if (a is decimal)
                // simple, for now
                return ((decimal)a).ToString("F3");
            if (a is int)
                return ((int)a).ToString();
            if (a is short)
                return ((short)a).ToString();
            if (a is long)
                return ((long)a).ToString();
            if (a is byte)
                return ((byte)a).ToString();
            if (a is char)
                return ((char)a).ToString();
            if (a is ushort)
                return ((ushort)a).ToString();
            if (a is uint)
                return ((uint)a).ToString();
            if (a is ulong)
                return ((ulong)a).ToString();

            Debug.Assert(false);
            throw new Exception($"Not a number: {a}");
        }

        // useful so I can sort by number value
        private static double NumberToDouble(object a) {
            if (a is double)
                // simple, for now
                return ((double)a);
            if (a is float)
                // simple, for now
                return ((float)a);
            if (a is decimal)
                // simple, for now
                return Convert.ToDouble( ((decimal)a) ) ;
            if (a is int)
                return ((int)a);
            if (a is short)
                return ((short)a);
            if (a is long)
                return ((long)a);
            if (a is byte)
                return ((byte)a);
            if (a is char)
                return ((char)a);
            if (a is ushort)
                return ((ushort)a);
            if (a is uint)
                return ((uint)a);
            if (a is ulong)
                return ((ulong)a);

            Debug.Assert(false);
            throw new Exception($"Not a number: {a}");
        }



        // ... sorted by values
        private static IReadOnlyList<(string, object)> ToUniqueValuesNumbers(IReadOnlyList<object> items, string propertyName, PropertyValueCompareEquivalent tolerance) {
            List<Number> numbers = new List<Number>();
            var propertyInfo = items[0].GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            HashSet<long> hashSet = new HashSet<long>();
            foreach (var item in items) {
                var value = propertyInfo.GetValue(item);
                var hash = tolerance.NumberToLongHash(value);
                if (!hashSet.Contains(hash)) {
                    hashSet.Add(hash);
                    var number = new Number {
                        AsDouble = NumberToDouble(value),
                        OriginalNumber = value,
                        AsString = NumberToString(value),
                    };
                    numbers.Add(number);
                }
            }

            return numbers.OrderBy(n => n.AsDouble).Select(n => (n.AsString, n.OriginalNumber)).ToList();
        }

        private class DateTimeValue {
            // keep it as double to encompass all number - i only want this for the end sorting
            public long Hash;
            public object OriginalDate;
            public string AsString;
        }
        // ... sorted by values
        private static IReadOnlyList<(string, object)> ToUniqueValuesDateTime(IReadOnlyList<object> items, string propertyName, PropertyValueCompareEquivalent tolerance) {
            List<DateTimeValue> values = new List<DateTimeValue>();
            var propertyInfo = items[0].GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            HashSet<long> hashSet = new HashSet<long>();
            foreach (var item in items) {
                var value = (DateTime)propertyInfo.GetValue(item);
                var hash = tolerance.DateTimeToLongHash(value);
                if (!hashSet.Contains(hash)) {
                    hashSet.Add(hash);
                    var dt = new DateTimeValue {
                        Hash = hash,
                        OriginalDate = value,
                        AsString = tolerance.DateTimeToString(value),
                    };
                    values.Add(dt);
                }
            }

            return values.OrderBy(n => n.OriginalDate).Select(n => (n.AsString, n.OriginalDate)).ToList();
        }

        private class StringValue {
            public object OriginalValue;
            public string AsString;
            public string AsLocaseString;
        }
        private static IReadOnlyList<(string, object)> ToUniqueValuesStrings(IReadOnlyList<object> items, string propertyName, PropertyValueCompareEquivalent tolerance) {
            List<StringValue> strings = new List<StringValue>();
            var propertyInfo = items[0].GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            HashSet<string> hashSet = new HashSet<string>();
            foreach (var item in items) {
                var value = propertyInfo.GetValue(item);
                var str = value.ToString();
                var locase = str.ToLowerInvariant();
                if (!hashSet.Contains(locase)) {
                    hashSet.Add(locase);
                    var sv = new StringValue {
                        OriginalValue = value, AsString = str, AsLocaseString = locase,
                    };
                    strings.Add(sv);
                }
            }

            return strings.OrderBy(s => s.AsLocaseString).Select(s => (s.AsString, s.OriginalValue)).ToList();
        }

        // ... sorted by values
        public static IReadOnlyList<(string AsString, object OriginalValue)> ToUniqueValues(IReadOnlyList<object> items, string propertyName, PropertyValueCompareEquivalent tolerance) {
            Dictionary<string, object> values = new Dictionary<string, object>();
            if (items.Count < 1)
                return new List<(string, object)>();

            // FIXME at this time, I don't handle date/time
            var propertyInfo = items[0].GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            var firstValue = propertyInfo.GetValue(items[0]);
            if (firstValue is DateTime)
                return ToUniqueValuesDateTime(items, propertyName, tolerance);
            else if (IsNumber(firstValue))
                return ToUniqueValuesNumbers(items, propertyName, tolerance);
            else
                return ToUniqueValuesStrings(items, propertyName, tolerance);
        }
    }
}
