using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using OpenSilver.ControlsKit.FastGrid.Util;
using OpenSilver.Internal.Xaml;
using OpenSilver.Internal.Xaml.Context;

namespace FastGrid.FastGrid
{
    public static class FastGridUtil
    {

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

    }
}
