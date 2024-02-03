using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
                throw new Exception($"column {name} not found");
            }
        }

    }
}
