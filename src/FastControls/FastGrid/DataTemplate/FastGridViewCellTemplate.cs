using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using DotNetForHtml5.Core;
using FastGrid.FastGrid;
using FastGrid.FastGrid.Filter;
using OpenSilver.ControlsKit.FastGrid.Util;

namespace OpenSilver.ControlsKit.FastGrid.DataTemplate
{
    public static partial class FastGridViewCellTemplate
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
        private class BoolToOpacityConverter : IValueConverter {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
                if (value is bool boolVal) {
                    return boolVal ? 1 : 0;
                }

                return 0;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
                return null;
            }
        }

        private class SimpleDateTimeConverter : IValueConverter {
            private bool _convertToDate;

            public SimpleDateTimeConverter(bool convertToDate) {
                _convertToDate = convertToDate;
            }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
                if (value is DateTime dt) {
                    return (_convertToDate) ? dt.ToShortDateString() : dt.ToShortTimeString();
                }
                return null;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
                return null;
            }
        }

        public static System.Windows.DataTemplate ReadOnlyBool(object o, string propertyName) {
            /*
             <Border Background="#7FA9A9A9" BorderBrush="#00FFFFFF" BorderThickness="2" CornerRadius="3" Width="14" Height="14" >
                   <Path x:Name="CheckIcon"
                     Fill="#FF333333"
                     Stretch="Fill"
                     Width="10.5"
                     Height="10"
                     Data="M102.03442,598.79645 L105.22962,597.78918 L106.78825,600.42358 C106.78825,600.42358 108.51028,595.74304 110.21724,593.60419 C112.00967,591.35822 114.89314,591.42316 114.89314,591.42316 C114.89314,591.42316 112.67844,593.42645 111.93174,594.44464 C110.7449,596.06293 107.15683,604.13837 107.15683,604.13837 z"
                     FlowDirection="LeftToRight" 
                         />
               </Border>
             */
            return FastGridUtil.CreateDataTemplate(() => {
                var grid = new Grid();
                var border = new Border {
                    Background = BrushCache.Inst.GetByColor(Color.FromArgb(0x7f, 0xa9, 0xa9, 0xa9)),
                    BorderBrush = BrushCache.Inst.GetByColor(Colors.Transparent), 
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(3),
                    Width = 14, Height = 14, 
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                };

                var geometry = TypeFromStringConverters.ConvertFromInvariantString(typeof(Geometry),
                    "M102.03442,598.79645 L105.22962,597.78918 L106.78825,600.42358 C106.78825,600.42358 108.51028,595.74304 110.21724,593.60419 C112.00967,591.35822 114.89314,591.42316 114.89314,591.42316 C114.89314,591.42316 112.67844,593.42645 111.93174,594.44464 C110.7449,596.06293 107.15683,604.13837 107.15683,604.13837 z") as Geometry;
                var path = new Path {
                    Data = geometry, 
                    Stretch = Stretch.Fill, 
                    Fill = BrushCache.Inst.GetByColor(Color.FromRgb(0x33,0x33,0x33)),
                    Margin = new Thickness(0),
                    Width = 10.5, Height = 10, 
                    FlowDirection = FlowDirection.LeftToRight,
                };
                path.AddBinding(Path.OpacityProperty, propertyName, converter: new BoolToOpacityConverter());
                border.Child = path;
                grid.Children.Add(border);
                return grid;
            });
        }
        public static System.Windows.DataTemplate EditableBool(object o, string propertyName) {
            return FastGridUtil.CreateDataTemplate(() => {
                var grid = new Grid ();
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
                }.AddBinding(TextBox.TextProperty, propertyName, mode: BindingMode.TwoWay, updateTrigger: UpdateSourceTrigger.PropertyChanged);
                grid.Children.Add(tb);
                return grid;
            });
        }
        public static System.Windows.DataTemplate ReadOnlyCombo(object o, string propertyName) {
            return ReadOnlyText(o, propertyName);
        }

        private static IReadOnlyList<object> GetEnumValues(object o) {
            // Get the type of the enum
            Type enumType = o.GetType();
            Debug.Assert(enumType.IsEnum);

            List<object> enums = new List<object>();
            foreach (var value in Enum.GetValues(enumType))
                enums.Add(value);
            return enums;
        }

        public static System.Windows.DataTemplate EditableCombo(object o, string propertyName) {
            var prop = o.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var value = prop.GetValue(o);

            return FastGridUtil.CreateDataTemplate(() => {
                var grid = new Grid();
                var cb = new ComboBox {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                cb.ItemsSource = GetEnumValues(value);
                cb.AddBinding(ComboBox.SelectedItemProperty, propertyName, mode: BindingMode.TwoWay, updateTrigger: UpdateSourceTrigger.Default);
                grid.Children.Add(cb);
                return grid;
            });
        }
        public static System.Windows.DataTemplate ReadOnlyDate(object o, string propertyName) {
            return FastGridUtil.CreateDataTemplate(() => {
                var grid = new Grid();
                var tb = new TextBlock {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                }.AddBinding(TextBlock.TextProperty, propertyName, converter: new SimpleDateTimeConverter(convertToDate: true));
                grid.Children.Add(tb);
                return grid;
            });
        }
        public static System.Windows.DataTemplate EditableDate(object o, string propertyName) {
            return FastGridUtil.CreateDataTemplate(() => {
                var grid = new Grid();
                var picker = new DatePicker {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                }.AddBinding(DatePicker.SelectedDateProperty, propertyName, mode: BindingMode.TwoWay, updateTrigger: UpdateSourceTrigger.Default);
                grid.Children.Add(picker);
                return grid;
            });
        }
        public static System.Windows.DataTemplate ReadOnlyTime(object o, string propertyName) {
            return FastGridUtil.CreateDataTemplate(() => {
                var grid = new Grid();
                var tb = new TextBlock {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                }.AddBinding(TextBlock.TextProperty, propertyName, converter: new SimpleDateTimeConverter(convertToDate: false));
                grid.Children.Add(tb);
                return grid;
            });
        }
        public static System.Windows.DataTemplate EditableTime(object o, string propertyName, Brush backgroundBrush = null) {
            if (backgroundBrush == null)
                // the idea -- otherwise, we would have no background, and overlap with the read-only time, looks horrible
                backgroundBrush = BrushCache.Inst.GetByColor(Colors.GhostWhite);

            return FastGridUtil.CreateDataTemplate(() => {
                var grid = new Grid();
                var picker = new TimePicker {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Background = backgroundBrush,
                }.AddBinding(TimePicker.ValueProperty, propertyName, mode: BindingMode.TwoWay, updateTrigger: UpdateSourceTrigger.Default);
                grid.Children.Add(picker);
                return grid;
            });
        }


        public static System.Windows.DataTemplate Default(object o, string propertyName, bool preferDate = true) {
            var pi = o.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var isBool = FastGridViewFilterUtil.IsBool(pi);
            var isEnum = FastGridViewFilterUtil.IsEnum(pi);
            var isString = FastGridViewFilterUtil.IsNumber(pi) || FastGridViewFilterUtil.IsString(pi);
            var isDate = FastGridViewFilterUtil.IsDateTime(pi);
            if (isBool)
                return ReadOnlyBool(o, propertyName);
            if (isEnum)
                return ReadOnlyCombo(o, propertyName);
            if (isDate)
                return preferDate ? ReadOnlyDate(o, propertyName) : ReadOnlyTime(o, propertyName);

            return ReadOnlyText(o, propertyName);
        }


        // creates the CellEditTemplate
        public static System.Windows.DataTemplate DefaultEdit(object o, string propertyName, bool preferDate = true) {
            var pi = o.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var isBool = FastGridViewFilterUtil.IsBool(pi);
            var isEnum = FastGridViewFilterUtil.IsEnum(pi);
            var isString = FastGridViewFilterUtil.IsNumber(pi) || FastGridViewFilterUtil.IsString(pi);
            var isDate = FastGridViewFilterUtil.IsDateTime(pi);
            if (isBool)
                return EditableBool(o, propertyName);
            if (isEnum)
                return EditableCombo(o, propertyName);
            if (isDate)
                return preferDate ? EditableDate(o, propertyName) : EditableTime(o, propertyName);
            if (isString)
                return EditableText(o, propertyName);

            return ReadOnlyText(o, propertyName);
        }
    }
}
