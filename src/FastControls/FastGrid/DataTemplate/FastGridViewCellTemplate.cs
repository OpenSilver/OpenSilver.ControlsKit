using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using FastGrid.FastGrid;
using FastGrid.FastGrid.Filter;
using OpenSilver.ControlsKit.FastGrid.Util;

namespace OpenSilver.ControlsKit.FastGrid.DataTemplate
{
    public static class FastGridViewCellTemplate
    {
        private class ReadOnlyBoolConverter : IValueConverter {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
                if (value is bool boolVal) {
                    return boolVal ? "Yes" : "No";
                }

                return "";
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
                return null;
            }
        }

        public static System.Windows.DataTemplate ReadOnlyBool(object o, string propertyName) {
            return FastGridUtil.CreateDataTemplate(() => {
                var grid = new Grid();
                var tb = new TextBlock {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                }.AddBinding(TextBlock.TextProperty, propertyName, converter: new ReadOnlyBoolConverter());
                grid.Children.Add(tb);
                return grid;
            });
        }
        public static System.Windows.DataTemplate EditableBool(object o, string propertyName) {
            return FastGridUtil.CreateDataTemplate(() => {
                var grid = new Grid();
                var tb = new CheckBox {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                }.AddBinding(CheckBox.IsCheckedProperty, propertyName, mode: BindingMode.TwoWay);
                grid.Children.Add(tb);
                return grid;
            });
        }
        public static System.Windows.DataTemplate ReadOnlyText(object o, string propertyName) {
            return FastGridUtil.CreateDataTemplate(() => {
                var grid = new Grid();
                var tb = new TextBlock {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                }.AddBinding(TextBlock.TextProperty, propertyName);
                grid.Children.Add(tb);
                return grid;
            });
        }
        public static System.Windows.DataTemplate EditableText(object o, string propertyName) {
            return FastGridUtil.CreateDataTemplate(() => {
                var grid = new Grid();
                var tb = new TextBox {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                }.AddBinding(TextBox.TextProperty, propertyName, mode: BindingMode.TwoWay);
                grid.Children.Add(tb);
                return grid;
            });
        }
        public static System.Windows.DataTemplate ReadOnlyCombo(object o, string propertyName) {
            return ReadOnlyText(o, propertyName);
        }
        public static System.Windows.DataTemplate EditableCombo(object o, string propertyName) {
            // I should create the combo box from the enum values in the enum type
            Debug.Assert(false);
            return EditableText(o, propertyName);
        }
        public static System.Windows.DataTemplate ReadOnlyDate(object o, string propertyName) {
            return ReadOnlyText(o, propertyName);
        }
        public static System.Windows.DataTemplate EditableDate(object o, string propertyName) {
            // I should create the datetime control here
            Debug.Assert(false);
            return EditableText(o, propertyName);
        }


        public static System.Windows.DataTemplate Default(object o, string propertyName) {
            var pi = o.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            var isBool = FastGridViewFilterUtil.IsBool(pi);
            var isEnum = FastGridViewFilterUtil.IsEnum(pi);
            var isString = FastGridViewFilterUtil.IsNumber(pi) || FastGridViewFilterUtil.IsString(pi);
            var isDate = FastGridViewFilterUtil.IsDateTime(pi);
            if (isBool)
                return ReadOnlyBool(o, propertyName);
            if (isEnum)
                return ReadOnlyCombo(o, propertyName);
            if (isDate)
                return ReadOnlyDate(o, propertyName);

            return ReadOnlyText(o, propertyName);
        }


        // creates the CellEditTemplate
        public static System.Windows.DataTemplate DefaultEdit(object o, string propertyName) {
            var pi = o.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            var isBool = FastGridViewFilterUtil.IsBool(pi);
            var isEnum = FastGridViewFilterUtil.IsEnum(pi);
            var isString = FastGridViewFilterUtil.IsNumber(pi) || FastGridViewFilterUtil.IsString(pi);
            var isDate = FastGridViewFilterUtil.IsDateTime(pi);
            if (isBool)
                return EditableBool(o, propertyName);
            if (isEnum)
                return EditableCombo(o, propertyName);
            if (isDate)
                return EditableDate(o, propertyName);
            if (isString)
                return EditableText(o, propertyName);

            return ReadOnlyText(o, propertyName);
        }
    }
}
