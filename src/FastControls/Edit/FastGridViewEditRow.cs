using FastGrid.FastGrid;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using FastGrid.FastGrid.Data;
using FastGrid.FastGrid.Row;
using OpenSilver.ControlsKit.Edit;
using OpenSilver.ControlsKit.Edit.Args;
using OpenSilver.ControlsKit.FastGrid.Util;

namespace OpenSilver.ControlsKit.FastGrid.Row
{
    // IMPORTANT: for now, allow edit only in non-hierarchical grids
    internal class FastGridViewEditRow : INotifyPropertyChanged
    {
        private HierarchicalCollectionInfo _hierchicalInfo;
        private FastGridView _self;
        
        private bool _isEditing;

        private object _editObject;
        private int _editRowIdx = -1;
        private int _editColumnIdx = -1;
        private double _editLeft, _editTop;
        private PropertyInfo _editProperty;
        // old value -> in case we need to cancel
        private object _oldValue;

        public bool CanEdit(int columnIdx) => TryGetCell(columnIdx) != null;
        public bool IsEditing => _isEditing;

        public HierarchicalCollectionInfo HierchicalInfo {
            get => _hierchicalInfo;
            private set {
                if (Equals(value, _hierchicalInfo)) {
                    return;
                }

                _hierchicalInfo = value;
                OnPropertyChanged();
            }
        }

        public int IndentLevel {
            get => _indentLevel;
            set {
                if (value == _indentLevel) {
                    return;
                }

                _indentLevel = value;
                OnPropertyChanged();
            }
        }

        public double HorizontalOffset {
            get => _horizontalOffset;
            set {
                if (value.Equals(_horizontalOffset)) {
                    return;
                }

                _horizontalOffset = value;
                OnPropertyChanged();
            }
        }

        // if true, when editing, we update the datacontext of all the row, so that moving between cells in the same row would be faster
        // note: I'm not sure yet if setting this to true will actually improve anything
        public bool SetDataContextToAllRow { get; set; } = false;

        // note: where a cell is not editable, this will contain null
        private List<FastGridViewEditCell> _editCells = new List<FastGridViewEditCell>();
        private int _indentLevel = 0;
        private double _horizontalOffset = 0;

        public IReadOnlyList<FastGridViewColumn> SortedColumns => _self.SortedColumns;

        public FastGridViewColumn FirstEditableColumn() => SortedColumns.FirstOrDefault(c => !c.IsReadOnly && c.CellEditTemplate != null);

        public FastGridViewEditRow(FastGridView self, HierarchicalCollectionInfo hierchicalInfo) {
            HierchicalInfo = hierchicalInfo;
            _self = self;
            RecreateEditCells();
        }

        private void UpdateDataContext(object o) {
            FastGridUtil.SetDataContext(TryGetCell(_editColumnIdx), o);

            if (SetDataContextToAllRow)
                foreach (var cell in _editCells.Where(cell => cell != null))
                    FastGridUtil.SetDataContext(cell, o);
        }

        internal void RecreateEditCells() {
            var oldCells = _self.canvas.Children.OfType<FastGridViewEditCell>().ToList();
            foreach (var cell in oldCells)
                _self.canvas.Children.Remove(cell);

            _editCells.Clear();
            for (int i = 0; i < SortedColumns.Count; i++) {
                FastGridViewColumn col = SortedColumns[i];
                if (col.CellEditTemplate != null) {
                    var edit = new FastGridViewEditCell(col) {
                        ContentTemplate = col.CellEditTemplate,
                        Width = col.Width, 
                        MinWidth = double.IsNaN(col.MinWidth) ? 0 : col.MinWidth,
                        MaxWidth = double.IsNaN(col.MaxWidth) ? double.MaxValue : col.MaxWidth,
                        Height = _self.RowHeight,
                    };
                    _editCells.Add(edit);
                    _self.canvas.Children.Add(edit);
                    FastGridUtil.SetLeft(edit, FastGridViewDrawController.OUTSIDE_SCREEN);
                    FastGridUtil.SetZIndex(edit, 2000); // on top of anything else
                } else 
                    _editCells.Add(null);
            }
        }

        private FastGridViewEditCell TryGetCell(int idx) {
            if (idx < 0 || idx >= SortedColumns.Count)
                return null;
            return (SortedColumns[idx].CellEditTemplate != null) ? _editCells[idx] : null;
        }
        private FastGridViewEditCell TryGetCell(string name) {
            for (int i = 0; i < SortedColumns.Count; i++) {
                FastGridViewColumn col = SortedColumns[i];
                if (col.DataBindingPropertyName == name)
                    return TryGetCell(i);
            }

            return null;
        }

        private void HideEditedCell() {
            if (_editColumnIdx < 0)
                return;
            var cell = TryGetCell(_editColumnIdx);
            FastGridUtil.SetLeft(cell, FastGridViewDrawController.OUTSIDE_SCREEN);
            FastGridUtil.SetEnabled(cell, false);
        }

        public FastGridViewColumn LeftToColumn(double left) {
            if (left < 0)
                return null;
            var colLeft = 0d;
            foreach (var col in SortedColumns)
                if (left >= colLeft && left < colLeft + col.Width)
                    return col;
                else
                    colLeft += col.Width;
            return null;
        }

        public void BeginEdit(FastGridViewRow row, FastGridViewColumn col) {
            var colIdx = FastGridUtil.RefIndex(SortedColumns, col);
            var colLeft = SortedColumns.Take(colIdx).Sum(c => c.Width);
            var rowTop = Canvas.GetTop(row);
            if (rowTop >= 0)
                BeginEdit(row.RowObject, row.RowIndex, colIdx, colLeft, rowTop);
        }

        private void BeginEdit(object o, int rowIdx, int columnIdx, double left, double top) {
            if (_isEditing)
                CommitEditImpl(keepEditing: true);

            var cell = TryGetCell(columnIdx);
            if (cell == null)
                throw new FastGridViewException($"column {columnIdx} is not editable");

            _isEditing = true;
            _editObject = o;
            _editLeft = left;
            _editTop = top;
            _editRowIdx = rowIdx;
            _editColumnIdx = columnIdx;
            var propertyName = SortedColumns[columnIdx].DataBindingPropertyName;
            _editProperty = o.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            _oldValue = _editProperty.GetValue(o);
            UpdateDataContext(o);

            UpdateUI();

            var args = new CellEditBeginArgs { Row = _editRowIdx, Column = _editColumnIdx, RowObject = _editObject };
            _self.RaiseCellEditBegin(args);
        }

        private void UpdateUI() {
            if (_editColumnIdx < 0)
                return; // not editing
            var cell = TryGetCell(_editColumnIdx);
            FastGridUtil.SetEnabled(cell, true);
            FastGridUtil.SetLeft(cell, _editLeft - HorizontalOffset);
            FastGridUtil.SetTop(cell, _editTop);
            FastGridUtil.SetWidth(cell, SortedColumns[_editColumnIdx].Width);
            FastGridUtil.SetHeight(cell, _self.RowHeight);
        }

        public void CommitEdit() {
            CommitEditImpl(keepEditing: false);
        }
        private void CommitEditImpl(bool keepEditing) {
            var args = new CellEditEndingArgs {Row = _editRowIdx, Column = _editColumnIdx, RowObject = _editObject};
            _self.RaiseCellEditEnding(args);

            if (!args.Cancel) {
                try {
                    if (_editObject != null)
                        _editProperty?.SetValue(_editObject, _oldValue);
                }
                finally {
                    HideEditedCell();
                    if (!keepEditing) {
                        _isEditing = false;
                        UpdateDataContext(null);
                        _editObject = null;
                        _editProperty = null;
                        _editColumnIdx = -1;
                        _self.Focus();
                    }
                }
            } else 
                CancelEditImpl();
        }

        public void CancelEdit() {
            // communicate to the client that we've been cancelled
            var args = new CellEditEndingArgs { Cancel = true, Row = _editRowIdx, Column = _editColumnIdx, RowObject = _editObject };
            _self.RaiseCellEditEnding(args);

            CancelEditImpl();
        }

        private void CancelEditImpl() {
            var args = new CellEditEndedArgs();
            _self.RaiseCellEditEnded(args);

            try {
                _editProperty.SetValue(_editObject, _oldValue);
            }
            finally {
                _isEditing = false;
                HideEditedCell();
                UpdateDataContext(null);
                _editObject = null;
                _editProperty = null;
                _editColumnIdx = -1;
                _self.Focus();
            }
        }

        private void vm_propertyChanged(string propertyName) {
            switch (propertyName) {
                case "HorizontalOffset":
                    // can happen when user enters edit, but uses the horizontal scrollbar
                    UpdateUI();
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
