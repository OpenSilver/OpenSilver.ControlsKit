using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OpenSilver.ControlsKit.FastGrid.Util;

namespace FastGrid.FastGrid
{
    public class FastGridViewColumnCollection : ObservableCollection<FastGridViewColumn>
    {
        protected FastGridViewColumnCollection() {

        }
        protected FastGridViewColumnCollection(IEnumerable<FastGridViewColumn> list) : base(list) {

        }
        public FastGridViewColumn this[string name] {
            get {
                foreach (var col in this) {
                    if (col.UniqueName == "" && col.DataBindingPropertyName == name)
                        return col;

                    if (col.UniqueName == name)
                        return col;
                }
                throw new FastGridViewException($"column {name} not found");
            }
        }

    }
}
