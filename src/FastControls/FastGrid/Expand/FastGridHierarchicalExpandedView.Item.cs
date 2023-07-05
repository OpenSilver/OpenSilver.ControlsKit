using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using FastGrid.FastGrid.Data;

namespace FastGrid.FastGrid.Expand {
    internal partial class FastGridHierarchicalExpandedView {

        private class Item : INotifyPropertyChanged {
            private static int _nextHeaderId = 0;

            public Item(RowInfo ri) {
                RowInfo = ri;
            }

            public readonly RowInfo RowInfo ;

            public object RowObject => RowInfo.RowObject;

            public FastGridViewDataHolder DataHolder;
            public IEnumerable Collection;

            public bool IsExpanded => DataHolder != null;

            public bool CanBeExpanded => !IsHeader && Collection != null && CollectionLength() > 0;

            public bool IsHeader => RowInfo.HeaderControl != null;

            public List<Item> Children = new List<Item>();

            private int CollectionLength() {
                if (Collection is IReadOnlyList<object> list)
                    return list.Count;
                return 0;
            }

            private object CollectionItem(int idx) {
                if (Collection is IReadOnlyList<object> list)
                    return list[idx];
                return null;
            }

            public void RefreshChildren(FastGridView self) {
                Debug.Assert(IsExpanded);
                if (!IsExpanded) {
                    FastGridView.Logger("FATAL: trying to refresh children of collapsed sub-hierarchical view");
                    return;
                }

                var oldItems = Children.Where(i => !i.IsHeader).ToDictionary(i => i.RowObject, i => i);

                var headerId = (Children.Count > 0) ? Children[0].RowInfo.HeaderId : ++_nextHeaderId;
                var newChildren = new List<Item> ();
                for (int i = 0; i < DataHolder.HeaderRowCount; ++i)
                    newChildren.Add(
                        new Item(new RowInfo(DataHolder, new object(), headerId, DataHolder.HeaderRowCount, i, RowInfo.IndentLevel + 1 )));

                foreach (var si in DataHolder.SortedItems) {
                    if (oldItems.TryGetValue(si, out var existingChild)) {
                        newChildren.Add(existingChild);
                        oldItems.Remove(si);
                    } else {
                        var coll = self.ExpandFunc(si);
                        newChildren.Add(new Item(new RowInfo(DataHolder, si, RowInfo.IndentLevel + 1)) {
                            Collection = coll,
                        });
                    }
                }

                foreach (var o in oldItems)
                    o.Value.DisposeTree();
                oldItems.Clear();
                Children = newChildren;
            }

            public void Expand(FastGridView self) {
                Debug.Assert(!IsExpanded && RowObject != null);

                // refresh collection, just in case
                Collection = self.ExpandFunc(RowObject);
                if (Collection != null && CollectionLength() > 0) {
                    var hci = self.ObjectTypeFunc( CollectionItem(0));

                    DataHolder = new FastGridViewDataHolder(self, hci.InternalColumns.Clone(), hci.HeaderTemplate, headerRowCount: hci.HeaderRowCount);
                    // this will force a UI redraw, which will then redraw the children
                    DataHolder.SetSource(Collection);
                } else 
                    // expanding yielded nothing
                    OnPropertyChanged(nameof(CanBeExpanded));
                self.OnExpandedFunc?.Invoke(RowObject, true);
            }

            public void Collapse(FastGridView self) {
                if (!IsExpanded)
                    return;

                CollapseTree();
                self.OnExpandedFunc?.Invoke(RowObject, false);
            }

            private void CollapseTree() {
                foreach (var child in Children)
                    child.CollapseTree();

                // this will call SetSource(null), which will force a UI redraw, which will then redraw the children
                DataHolder?.Dispose();
                DataHolder = null;

                Children.Clear();
            }


            public void DisposeTree() {
                foreach (var child in Children)
                    child.DisposeTree();

                DataHolder?.Dispose();
                DataHolder = null;
                Collection = null;

                foreach (var child in Children)
                    child.DataHolder = null;
                Children.Clear();
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
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
}