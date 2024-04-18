using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace OpenSilver.ControlsKit.FastGrid.Util
{
    public static class BindingExtensions
    {
        public static T AddBinding<T>(this T fe, DependencyProperty dp, string propertyName, BindingMode mode = BindingMode.OneWay,
                                                  object dataContext = null, 
                                                  IValueConverter converter = null, 
                                                  object convertParameter = null, 
                                                  UpdateSourceTrigger updateTrigger = UpdateSourceTrigger.PropertyChanged) where T : FrameworkElement {
            var binding = new Binding(propertyName) {
                Mode = mode, 
                UpdateSourceTrigger = updateTrigger,
            };
            if (dataContext != null)
                binding.Source = dataContext;
            if (converter != null)
                binding.Converter = converter;
            if (convertParameter != null)
                binding.ConverterParameter = convertParameter;
            fe.SetBinding(dp, binding);

            return fe;
        }
    }
}
