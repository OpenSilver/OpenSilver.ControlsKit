using System;
using System.Windows;
using System.Windows.Controls;

namespace OpenSilver.ControlsKit
{
    public partial class FlexPanel : Panel
    {
        public static readonly DependencyProperty GrowProperty =
                    DependencyProperty.RegisterAttached (
        "Grow",
        typeof (double),
        typeof (FlexPanel),
        new FrameworkPropertyMetadata (0.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        public static void SetGrow(UIElement element, double value) => element.SetValue (GrowProperty, value);
        public static double GetGrow(UIElement element) => (double)element.GetValue (GrowProperty);
    }
}
