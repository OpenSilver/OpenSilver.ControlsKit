using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OpenSilver.Internal.Xaml;
using OpenSilver.Internal.Xaml.Context;

namespace FastGrid.FastGrid
{
    internal static class FastGridUtil
    {
        private const double TOLERANCE = 0.0001;

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
            if (Math.Abs(fe.Width - width) > TOLERANCE)
                fe.Width = width;
        }
        public static void SetHeight(FrameworkElement fe, double height)
        {
            if (Math.Abs(fe.Height - height) > TOLERANCE)
                fe.Height = height;
        }

        public static void SetOpacity(FrameworkElement fe, double opacity) {
            if (Math.Abs(fe.Opacity - opacity) > TOLERANCE)
                fe.Opacity = opacity;
        }

        public static void SetDataContext(FrameworkElement fe, object context) {
            if (!ReferenceEquals(fe.DataContext, context))
                fe.DataContext = context;
        }

        public static void SetIsVisible(FrameworkElement fe, bool isVisible) {
            if ((fe.Visibility == Visibility.Visible) != isVisible)
                fe.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
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
            if (fe is Control control) 
                control.Background = bg;
            else if (fe is Panel panel)
                panel.Background = bg;
            else if (fe is Border border)
                border.Background = bg;
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
    }
}
