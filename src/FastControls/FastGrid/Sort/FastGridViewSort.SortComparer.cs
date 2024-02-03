using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace FastGrid.FastGrid {
    internal partial class FastGridViewSort {
        private class SortComparer : IComparer<object>, IDisposable {
            private FastGridViewSort _self;

            private class PropertyComparer {
                public PropertyInfo Property;
                public bool Ascending;
                public Func<object, object, int> SortFunc;
            }

            private List<PropertyComparer> _compareProperties;

            public SortComparer(FastGridViewSort self) {
                _self = self;
            }

            public void RecomputeProperties() {
                _compareProperties = null;
            }

            private void RecomputeProperties(object obj) {
                _compareProperties = new List<PropertyComparer>();
                var type = obj.GetType();
                foreach (var col in _self._self.SortDescriptors.Columns) {
                    var pi = type.GetProperty(col.Column.DataBindingPropertyName, BindingFlags.Instance | BindingFlags.Public);
                    if (pi == null)
                        throw new Exception($"Fastgrid: can't find property {col.Column.DataBindingPropertyName}");

                    _compareProperties.Add(new PropertyComparer {
                        Property = pi, 
                        Ascending = col.SortDirection == SortDirection.Ascending, 
                        SortFunc = col.Column.SortFunc,
                    });
                }
            }

            public int Compare(object a, object b) {
                Debug.Assert(a != null && b != null);
                if (_compareProperties == null)
                    RecomputeProperties(a);

                foreach (var prop in _compareProperties) {
                    var aValue = prop.Property.GetValue(a);
                    var bValue = prop.Property.GetValue(b);

                    var compare = prop.SortFunc?.Invoke(aValue,bValue) ?? CompareValue(aValue, bValue);
                    if (compare != 0)
                        return prop.Ascending ? compare : -compare;
                }

                return 0;
            }

            private int CompareValue(object a, object b) {
                if (a is int)
                    return (int)a - (int)b;
                if (a is uint)
                    return (uint)a < (uint)b ? -1 : ( (uint)a > (uint)b ? 1 : 0 );
                if (a is long)
                    return (long)a < (long)b ? -1 : ( (long)a > (long)b ? 1 : 0 );
                if (a is short)
                    return (short)a < (short)b ? -1 : ( (short)a > (short)b ? 1 : 0 );
                if (a is ulong)
                    return (ulong)a < (ulong)b ? -1 : ( (ulong)a > (ulong)b ? 1 : 0 );
                if (a is ushort)
                    return (ushort)a < (ushort)b ? -1 : ( (ushort)a > (ushort)b ? 1 : 0 );

                if (a is byte)
                    return (byte)a < (byte)b ? -1 : ( (byte)a > (byte)b ? 1 : 0 );
                if (a is char)
                    return (char)a < (char)b ? -1 : ( (char)a > (char)b ? 1 : 0 );

                if (a is double)
                    return (double)a < (double)b ? -1 : ( (double)a > (double)b ? 1 : 0 );
                if (a is decimal)
                    return (decimal)a < (decimal)b ? -1 : ( (decimal)a > (decimal)b ? 1 : 0 );
                if (a is float)
                    return (float)a < (float)b ? -1 : ( (float)a > (float)b ? 1 : 0 );
                if (a is string) {
                    var aString = (string)a;
                    var bString = (string)b;
                    return String.Compare(aString, bString, StringComparison.Ordinal);
                }
                if (a is DateTime)
                    return (DateTime)a < (DateTime)b ? -1 : ( (DateTime)a > (DateTime)b ? 1 : 0 );

                if (a == null || b == null) {
                    if (a == null && b == null)
                        return 0;
                    return a == null ? -1 : 1;
                }
                Debug.Assert(false);
                throw new Exception($"Type {a.GetType().ToString()} -- don't know how to Compare");
            }

            public void Dispose() {
                _compareProperties?.Clear();
                _self = null;
            }
        }
    }
}