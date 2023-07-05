using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Xml.Linq;

namespace FastGrid.FastGrid.Data
{
    // caches rows - and allows reusing them, based on what is actually visible on-screen
    // depending on an object type, I will figure out what Row to return (with the correct DataTemplate)
    internal class FastGridViewRowProvider {
        private FastGridView _self;

        // these are the rows - not all of them need to be visible, since we'll always make sure we have enough rows to accomodate the whole height of the control
        private List<FastGridViewRow> _rows = new List<FastGridViewRow>();

        public FastGridViewRowProvider(FastGridView self) {
            _self = self;
        }


        internal IReadOnlyList<FastGridViewRow> Rows => _rows;

        public FastGridViewRow TryGetRow(object obj)
        {
            foreach (var row in _rows)
                if (_self.RowEquals(row.RowObject,obj) && row.Used)
                    return row;
            return null;
        }

        private HierarchicalCollectionInfo ObjectType(object o) =>_self.ObjectTypeFunc?.Invoke(o) ?? _self.HierarchicalRoot;

        public FastGridViewRow CreateRow(object o) {
            var hci = ObjectType(o);

            if (_self.IsHierarchical) {
                hci.InternalColumns.EnsureExpandColumn();
            }

            var row = new FastGridViewRow(hci, _self.RowHeight) {
                Width = _self.canvas.Width,
                Used = true,
                SelectedBrush = _self.SelectionBackground,
                DataContext = null,
            };
            _rows.Add(row);
            _self.canvas.Children.Add(row);
            row.MouseRightButtonDown += _self.Row_MouseRightButtonDown;
            row.MouseLeftButtonDown += _self.Row_MouseLeftButtonDown;
            FastGridView.Logger($"row created({_self.Name}), rows={_rows.Count}");
            return row;
        }

        public FastGridViewRow TryReuseRow(object o) {
            var hci = ObjectType(o);

            foreach (var row in _rows)
                if (row.DataContext == null && !row.Used && ReferenceEquals(row.HierchicalInfo, hci)) {
                    row.Used = true;
                    return row;
                }

            return null;
        }

        public void SelectedBackgroundChanged()
        {
            foreach (var row in Rows)
                row.SelectedBrush = _self.SelectionBackground;
        }

        public void RowHeightChanged() {
            foreach (var row in Rows)
                row.RowHeight = _self.RowHeight;
        }

        public void ClearRows() {
            _rows.Clear();
        }


        public void SetRowOpacity(double value) {
            foreach (var row in _rows.Where(r => r.IsRowVisible))
                FastGridUtil.SetOpacity(row, value);
        }

        // the idea: when ItemsSource is changed, I want to hide all existing rows first
        // this is important, because on ItemsSource changed, I will postpone an UpdateUI -- but just in case something is keeping that Update from happening (CanDraw() returns false),
        // I want the user to see that the old items are not there anymore
        //
        // IMPORTANT: here, I'm not setting DataContext to null, because it's possible the new ItemsSource contains items from the old ItemsSource as well, for instance, a very inefficient:
        // ctrl.ItemsSource = new List<MyType> { a, b, c, d }; // old
        // ...
        // ctrl.ItemsSource = new List<MyType> { a, b, c  }; // new
        //
        // in this case, a, b, c are still valid objects, and we may have already cached these rows. No point in letting that go to waste
        public void HideAllRows() {
            foreach (var row in _rows) {
                row.Preloaded = false;
                row.IsRowVisible = false;
                FastGridUtil.SetLeft(row, -100000);
            }
        }

        public void HideInvisibleRows() {
            foreach (var row in _rows) {
                var visible = row.IsRowVisible;
                var left = visible ? 0 : -100000;
                FastGridUtil.SetLeft(row, left);

                if (visible) 
                    FastGridUtil.SetOpacity(row, 1);
                else {
                    if (!row.Preloaded) {
                        row.Used = false;
                        FastGridUtil.SetDataContext(row, null, out _);
                    }
                }
            }
        }

        public void SetWidth(double w) {
            foreach (var row in _rows)
                row.Width = w;
        }

        public void UpdateUI() {
            foreach (var row in _rows)
                row.UpdateUI();
        }

        public void OnOffscreen() {
            foreach (var row in _rows) {
                FastGridUtil.SetDataContext(row, null, out _);
                row.Used = false;
                row.RowObject = null;
            }
        }

        public void SetHorizontalOffset(double horizontalOffset) {
            foreach (var row in _rows.Where(r => r.IsRowVisible))
                row.HorizontalOffset = horizontalOffset;
        }

    }
}
