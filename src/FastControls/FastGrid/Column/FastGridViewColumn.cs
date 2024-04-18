using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FastGrid.FastGrid.Filter;
using OpenSilver.ControlsKit.FastGrid.DataTemplate;
using OpenSilver.ControlsKit.FastGrid.Util;

namespace FastGrid.FastGrid
{

    // IMPORTANT: no loger derives from DependencyObject
    //
    // I need this to be extremely lightweight, since I will copy this a lot for hierarchical grids
    //
    // At this time, these properties are not bindable. If at some point this will be needed, I will need to create a LightweightFastGridViewColumn class
    // however, this would be an insane pain, since for hierarchical grids, I need to copy this information, since each child (expanded) control will need to have its own copy of the data
    // (the idea is each child will have its own header, and be able to resize/sort/filter that header)
    [DebuggerDisplay("u={UniqueName} bind={DataBindingPropertyName}")]
    public sealed class FastGridViewColumn : INotifyPropertyChanged
    {
        public double Width {
            get => _width;
            set {
                if (value.Equals(_width)) return;
                _width = value;
                OnPropertyChanged();
            }
        }

        public double MinWidth {
            get => _minWidth;
            set {
                if (value.Equals(_minWidth)) return;
                _minWidth = value;
                OnPropertyChanged();
            }
        }

        public double MaxWidth {
            get => _maxWidth;
            set {
                if (value.Equals(_maxWidth)) return;
                _maxWidth = value;
                OnPropertyChanged();
            }
        }

        // simple for now
        public string HeaderText {
            get => _headerText;
            set {
                if (value == _headerText) return;
                _headerText = value;
                OnPropertyChanged();
            }
        }

        // if true, user can drag this column, in order to move it (reorder columns)
        public bool CanDrag {
            get => _canDrag;
            set {
                if (value == _canDrag) {
                    return;
                }

                _canDrag = value;
                OnPropertyChanged();
            }
        }


        // used in FastGridContentTemplate.DefaultHeaderTemplate
        public Brush HeaderForeground {
            get => _headerForeground;
            set {
                if (value == _headerForeground) return;
                _headerForeground = value;
                OnPropertyChanged();
            }
        }


        // used in FastGridContentTemplate.DefaultHeaderTemplate
        public double HeaderFontSize {
            get => _headerFontSize;
            set {
                if (value == _headerFontSize) return;
                _headerFontSize = value;
                OnPropertyChanged();
            }
        }


        // used in FastGridContentTemplate.DefaultHeaderTemplate
        public FontWeight HeaderFontWeight
        {
            get => _headerFontWeight;
            set
            {
                if (value == _headerFontWeight) return;
                _headerFontWeight = value;
                OnPropertyChanged();
            }
        }

        private static readonly FontFamily DefaultHeaderFontFamily = (FontFamily)System.Windows.Documents.TextElement.FontFamilyProperty.GetMetadata(typeof(FrameworkElement)).DefaultValue;

        // used in FastGridContentTemplate.DefaultHeaderTemplate
        public FontFamily HeaderFontFamily
        {
            get => _headerFontFamily;
            set {
                if (value == null)
                    value = DefaultHeaderFontFamily;

                if (value == _headerFontFamily) return;
                _headerFontFamily = value;
                OnPropertyChanged();
            }
        }

        public bool IsVisible {
            get => _isVisible;
            set {
                if (value == _isVisible) return;
                _isVisible = value;
                OnPropertyChanged();
            }
        }

        public bool CanResize {
            get => _canResize;
            set {
                if (value == _canResize) return;
                _canResize = value;
                OnPropertyChanged();
            }
        }

        public string UniqueName {
            get => _uniqueName;
            set {
                if (value == _uniqueName) return;
                _uniqueName = value;
                OnPropertyChanged();
            }
        }

        public int DisplayIndex {
            get => _displayIndex;
            set {
                if (value == _displayIndex) return;
                _displayIndex = value;
                OnPropertyChanged();
            }
        }

        // allow several columns to be "auto" - take remaining space
        // for now, I only allow for one column
        //
        public int AutoWidth {
            get => _autoWidth;
            set {
                if (value == _autoWidth) return;
                _autoWidth = value;
                OnPropertyChanged();
            }
        }

        // default : true
        public bool IsReadOnly {
            get => _isReadOnly;
            set {
                if (value == _isReadOnly) {
                    return;
                }

                _isReadOnly = value;
                OnPropertyChanged();
            }
        }

        internal bool IsResizingColumn {
            get => _isResizingColumn;
            set {
                if (value == _isResizingColumn) return;
                _isResizingColumn = value;
                OnPropertyChanged();
            }
        }

        // true -> ascending, false -> descending, null -> none
        public bool? Sort {
            get => _sort;
            set {
                if (value == _sort) return;
                _sort = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSortAscending));
                OnPropertyChanged(nameof(IsSortDescending));
                OnPropertyChanged(nameof(IsSortNone));
                OnPropertyChanged(nameof(SortArrowAngle));
            }
        }

        public bool IsEditingFilter {
            get => _isEditingFilter;
            set {
                if (value == _isEditingFilter) return;
                _isEditingFilter = value;
                OnPropertyChanged();
            }
        }

        public string ColumnGroupName {
            get => _columnGroupName;
            set {
                if (value == _columnGroupName) return;
                _columnGroupName = value;
                OnPropertyChanged();
            }
        }

        // Gets or sets the data template for the cell in edit mode.
        public DataTemplate CellEditTemplate {
            get => _cellEditTemplate;
            set {
                if (Equals(value, _cellEditTemplate)) return;
                _cellEditTemplate = value;
                OnPropertyChanged();
            }
        }

        // Gets or sets the data template for the cell in view mode.
        public DataTemplate CellTemplate {
            get => _cellTemplate;
            set {
                if (Equals(value, _cellTemplate)) return;
                _isDummyCellTemplate = false;
                _cellTemplate = value;
                OnPropertyChanged();
            }
        }

        public bool IsFilterable { get; set; } = true;
        public bool IsSortable { get; set; } = true;

        // the idea: this is the name of the underlying property for this column. You only need to set this
        // if you want sorting and/or filtering
        public string DataBindingPropertyName { get; set; } = "";

        // normally, when you filter by a property, and the user clicks to filter, you'll show the names from DataBindingPropertyName
        // which are also used for sorting
        //
        // however, there will be a few cases when you want to show a friendly name instead. 
        // Example: you're sorting by an integer, but visually you want to show something friendly (like, "Early", "Inactive", etc)
        public string FilterFriendlyPropertyName { get; set; } = "";

        // sort function - set this only if the default sorting (which uses default comparison) doesn't work
        public Func<object, object, int> SortFunc { get; set; } = null;

        public string FilterPropertyName() => FilterFriendlyPropertyName != "" ? FilterFriendlyPropertyName : DataBindingPropertyName;

        public string ToolTipPropertyName { get; set; } = "";

        public FastGridViewColumn Clone(FastGridViewStyler styler) {
            var clone = new FastGridViewColumn {
                Width = Width, MinWidth = MinWidth, MaxWidth = MaxWidth,
                HeaderText = HeaderText, IsVisible = IsVisible, CanResize = CanResize, UniqueName = UniqueName,
                DisplayIndex = DisplayIndex, IsResizingColumn = IsResizingColumn, Sort = Sort, IsEditingFilter = IsEditingFilter,
                IsFilterable = IsFilterable, IsSortable = IsSortable,
                DataBindingPropertyName = DataBindingPropertyName,
                FilterFriendlyPropertyName = FilterFriendlyPropertyName,
                ToolTipPropertyName = ToolTipPropertyName,
                ColumnGroupName = ColumnGroupName,
            };
            styler.StyleColumn(clone);
            return clone;
        }

        public bool IsSortAscending => Sort == true;
        public bool IsSortDescending => Sort == false;
        public bool IsSortNone => Sort == null;
        public double SortArrowAngle => IsSortAscending ? 0 : 180;

        internal static readonly Point InvalidMousePos = new Point(-100000, -100000);
        internal Point MouseLeftDown { get; set; } = InvalidMousePos;
        internal bool IsDragging { get; set; }

        public void ForceUpdateColor() {
            // FIXME for some strange reason, the "Filter.Color" binding doesn't work for filters that are non-root
            // (in hierarchical context)
        }

        public FastGridViewFilterItem Filter { get; } = new FastGridViewFilterItem();
        // allow setting equivalence for this column
        public PropertyValueCompareEquivalent FilterCompareEquivalent => Filter.CompareEquivalent;


        public string FriendlyName() => UniqueName != "" ? UniqueName : DataBindingPropertyName != "" ? DataBindingPropertyName : DisplayIndex.ToString();

        private static DataTemplate DummyDataTemplate() {
            var dt = FastGridUtil.CreateDataTemplate(() => new Canvas());
            return dt;
        }

        public FastGridViewColumn CreateDefaultDataTemplate(object o, bool preferDate = true) {
            Debug.Assert(DataBindingPropertyName != "");
            if (_isDummyCellTemplate)
                CellTemplate = FastGridViewCellTemplate.Default(o, DataBindingPropertyName, preferDate);
            if (!IsReadOnly && CellEditTemplate == null)
                CellEditTemplate = FastGridViewCellTemplate.DefaultEdit(o, DataBindingPropertyName, preferDate);
            return this;
        }


        /// <summary>
        /// Identifies the CellTemplate property.
        /// </summary>
        private bool? _sort = null;
        private bool _isResizingColumn = false;
        private bool _isEditingFilter = false;
        private DataTemplate _cellEditTemplate = null;
        private bool _isDummyCellTemplate = true;
        private DataTemplate _cellTemplate = DummyDataTemplate();
        private int _displayIndex = -1;
        private string _uniqueName = "";
        private bool _canResize = true;
        private bool _isVisible = true;
        private string _headerText = "";
        private Brush _headerForeground = new SolidColorBrush(Colors.White);
        private double _headerFontSize = 14;
        private FontWeight _headerFontWeight = FontWeights.Normal;
        private FontFamily _headerFontFamily = DefaultHeaderFontFamily;
        private double _maxWidth = double.MaxValue;
        private double _minWidth = 0;
        private double _width = 0;
        private int _autoWidth = 0;
        private string _columnGroupName = "";
        private bool _canDrag = false;
        private bool _isReadOnly = true;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) {
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
