using FastGrid.FastGrid;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;
using OpenSilver.ControlsKit.Edit;

namespace OpenSilver.ControlsKit.FastGrid.Util
{
    internal static class FastGridInternalUtil
    {
        private const double TOLERANCE = 0.0001;
        // easy way to figure out if we added the Expander column, for hierarchical grids
        public const string EXPANDER_COLUMN = "__expander__";
        public const string EXPANDER_BORDER_NAME = "__expander__border__";

        public static void SetLeft(FrameworkElement fe, double left)
        {
            if (Math.Abs(Canvas.GetLeft(fe) - left) > TOLERANCE)
                Canvas.SetLeft(fe, left);
        }
        public static void SetTop(FrameworkElement fe, double top)
        {
            if (Math.Abs(Canvas.GetTop(fe) - top) > TOLERANCE)
                Canvas.SetTop(fe, top);
        }

        public static void SetWidth(FrameworkElement fe, double width)
        {
            if (Math.Abs(fe.Width - width) > TOLERANCE || double.IsNaN(fe.Width))
                fe.Width = width;
        }
        public static void SetHeight(FrameworkElement fe, double height)
        {
            if (Math.Abs(fe.Height - height) > TOLERANCE || double.IsNaN(fe.Height))
                fe.Height = height;
        }
        public static void SetZIndex(FrameworkElement fe, int zindex) {
            if (Canvas.GetZIndex(fe) != zindex)
                Canvas.SetZIndex(fe, zindex);
        }


        public static void SetOpacity(FrameworkElement fe, double opacity) {
            if (Math.Abs(fe.Opacity - opacity) > TOLERANCE)
                fe.Opacity = opacity;
        }

        public static void SetEnabled(FrameworkElement fe, bool enabled) {
            if (fe.IsEnabled != enabled)
                fe.IsEnabled = enabled;
        }

        public static void SetDataContext(FrameworkElement fe, object context) {
            SetDataContext(fe, context, out _);
        }

        public static void SetDataContext(FrameworkElement fe, object context, out bool changed) {
            changed = false;
            if (!ReferenceEquals(fe.DataContext, context)) {
                fe.DataContext = context;
                changed = true;
            }
        }

        public static void SetMaximum(RangeBase fe, double max) {
            if (Math.Abs(fe.Maximum - max) > TOLERANCE)
                fe.Maximum = max;
        }

        public static void SetIsVisible(FrameworkElement fe, bool isVisible) {
            if ((fe.Visibility == Visibility.Visible) != isVisible)
                fe.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public static void SetCustomLayout(FrameworkElement fe, bool val) {
            if (fe.CustomLayout != val)
                fe.CustomLayout = val;
        }

        public static void SetHorizontalAlignment(FrameworkElement fe, HorizontalAlignment val) {
            if (fe.HorizontalAlignment != val)
                fe.HorizontalAlignment = val;
        }

        public static void SetVerticalAlignment(FrameworkElement fe, VerticalAlignment val) {
            if (fe.VerticalAlignment != val)
                fe.VerticalAlignment = val;
        }

        public static void SetText(TextBlock tb, string text) {
            if (tb.Text != text)
                tb.Text = text;
        }



        public static T TryGetAscendant<T>(FrameworkElement fe) where T : FrameworkElement {
            while (fe != null) {
                if (fe is T t)
                    return t;
                fe = VisualTreeHelper.GetParent(fe) as FrameworkElement;
            }

            return null;
        }

        public static Brush ControlBackground(FrameworkElement fe) {
            if (fe is Control control)
                return control.Background;
            else if (fe is Panel panel)
                return panel.Background;
            else if (fe is Border border)
                return border.Background;
            return null;
        }

        public static void SetControlBackground(FrameworkElement fe, Brush bg) {
            if (fe is Control control) {
                if (!ReferenceEquals(control.Background, bg))
                    control.Background = bg;
            }
            else if (fe is Panel panel) {
                if (!ReferenceEquals(panel.Background, bg))
                    panel.Background = bg;
            }
            else if (fe is Border border) {
                if (!ReferenceEquals(border.Background, bg))
                    border.Background = bg;
            }
        }


        public static int RefIndex<T>(IReadOnlyList<T> list, T value) {
            var idx = 0;
            foreach (var i in list)
                if (ReferenceEquals(i, value))
                    return idx;
                else
                    ++idx;
            return -1;
        }

        public static bool SameColor(Brush a, Brush b) {
            if (a is SolidColorBrush aSolid && b is SolidColorBrush bSolid && aSolid.Color == bSolid.Color)
                return true;

            // FIXME care about lineargradientbrush as well
            return false;
        }

        internal static FastGridViewColumn NewExpanderColumn() {
            return new FastGridViewColumn {
                HeaderText = "",
                IsFilterable = false,
                IsSortable = false,
                Width = FastGridViewRow.WIDTH_PER_INDENT_LEVEL,
                CellTemplate = FastGridContentTemplate.DefaultExpanderTemplate(),
                DataBindingPropertyName = EXPANDER_COLUMN,
                UniqueName = EXPANDER_COLUMN,
            };
        }

        internal static ItemsControl NewHeaderControl() {
            var ipt = FastGridUtil. CreateItemsPanelTemplate(() => new StackPanel { Orientation = Orientation.Horizontal });
            var ic = new ItemsControl {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                ItemsPanel = ipt,
            };
            return ic;
        }

        internal static FastGridViewColumn FindColumnAtPos(Point p) {
            // note: commented out: this is an internal function
            //var ui = VisualTreeHelper.FindElementInHostCoordinates(p);
            //while (ui != null) {
            //    if (ui is FrameworkElement fe && fe.DataContext is FastGridViewColumn column)
            //        return column;
            //    ui = VisualTreeHelper.GetParent(ui) as UIElement;
            //}
            //return null;

            foreach (var element in VisualTreeHelper.FindElementsInHostCoordinates(p, null)) {
                var ui = element;
                while (ui != null) {
                    if (ui is FrameworkElement fe && fe.DataContext is FastGridViewColumn column)
                        return column;
                    ui = VisualTreeHelper.GetParent(ui) as UIElement;
                }
            }
            return null;
        }

        internal static FastGridView ColumnToView(FrameworkElement ctrl) {
            Debug.Assert(ctrl.DataContext is FastGridViewColumn);
            while (ctrl != null) {
                if (ctrl is FastGridView view)
                    return view;
                ctrl = VisualTreeHelper.GetParent(ctrl) as FrameworkElement;
            }

            throw new FastGridViewException("FastGridView not found, from column");
        }

        public static KeyNavigateAction KeyToNavigateAction(KeyEventArgs e) {
            var shift = (e.KeyModifiers & ModifierKeys.Shift) != 0;
            switch (e.Key) {
                case Key.Up: return KeyNavigateAction.Up;
                case Key.Down : return KeyNavigateAction.Down;
                case Key.Left: return KeyNavigateAction.Prev;
                case Key.Right: return KeyNavigateAction.Next;
                case Key.Tab: return shift ? KeyNavigateAction.PrevOrUp : KeyNavigateAction.NextOrDown;

                case Key.Enter: return KeyNavigateAction.Down;
                case Key.Escape: return KeyNavigateAction.Escape;

                case Key.Space: return KeyNavigateAction.Toggle;
            }

            return KeyNavigateAction.None;
        }
    }
}
