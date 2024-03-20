using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using OpenSilver.Internal.Xaml;
using OpenSilver.Internal.Xaml.Context;

namespace FastGrid.FastGrid
{
    public static class FastGridUtil
    {
        private const double TOLERANCE = 0.0001;
        // easy way to figure out if we added the Expander column, for hierarchical grids
        public const string EXPANDER_COLUMN = "__expander__";
        public const string EXPANDER_BORDER_NAME = "__expander__border__";

        public static void SetPropertyViaReflection(object obj, string propertyName, object value) {
            var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Debug.Assert(prop != null);
            prop.SetValue(obj, value);
        }

        public static DataTemplate CreateDataTemplate(Func<FrameworkElement> creator) {
            var xamlContext = RuntimeHelpers.Create_XamlContext();
            var dt = new DataTemplate();
            Func<FrameworkElement, XamlContext, FrameworkElement> factory = (control, xc) => {
                var fe = creator();
                RuntimeHelpers.SetTemplatedParent(fe, control);
                return fe;
            };

            RuntimeHelpers.SetTemplateContent(dt, xamlContext, factory);
            return dt;
        }
        public static ItemsPanelTemplate CreateItemsPanelTemplate(Func<FrameworkElement> creator) {
            var xamlContext = RuntimeHelpers.Create_XamlContext();
            var dt = new ItemsPanelTemplate();
            Func<FrameworkElement, XamlContext, FrameworkElement> factory = (control, xc) => {
                var fe = creator();
                RuntimeHelpers.SetTemplatedParent(fe, control);
                return fe;
            };

            RuntimeHelpers.SetTemplateContent(dt, xamlContext, factory);
            return dt;
        }

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
            var ipt = CreateItemsPanelTemplate(() => new StackPanel { Orientation = Orientation.Horizontal });
            var ic = new ItemsControl {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                ItemsPanel = ipt,
            };
            return ic;
        }

        internal static FastGridViewColumn FindColumnAtPos(Point p) {
            var ui = VisualTreeHelper.FindElementInHostCoordinates(p);
            while (ui != null) {
                if (ui is FrameworkElement fe && fe.DataContext is FastGridViewColumn column)
                    return column;
                ui = VisualTreeHelper.GetParent(ui) as UIElement;
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

            throw new Exception("FastGridView not found, from column");
        }

    }
}
