using System;
using System.Windows;
using System.Windows.Controls;

namespace OpenSilver.ControlsKit
{
    public enum JustifyContent
    {
        Start,
        End,
        Center,
        SpaceBetween,
        SpaceAround,
        SpaceEvenly,
        SpaceAuto
    }

    public enum AlignContent
    {
        Start,
        Center,
        End
    }

    public class FlexPanel : Panel
    {
        public Orientation Orientation
        {
            get { return (Orientation)GetValue (OrientationProperty); }
            set { SetValue (OrientationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Orientation.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register ("Orientation", typeof (Orientation), typeof (FlexPanel), new PropertyMetadata (Orientation.Horizontal, PropertyChangedCallback));


        public JustifyContent Justify
        {
            get { return (JustifyContent)GetValue (JustifyProperty); }
            set { SetValue (JustifyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Justify.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty JustifyProperty =
            DependencyProperty.Register ("Justify", typeof (JustifyContent), typeof (FlexPanel), new PropertyMetadata (JustifyContent.Center, PropertyChangedCallback));

        public AlignContent Align
        {
            get { return (AlignContent)GetValue (AlignProperty); }
            set { SetValue (AlignProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Justify.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AlignProperty =
            DependencyProperty.Register ("Align", typeof (AlignContent), typeof (FlexPanel), new PropertyMetadata (AlignContent.Center, PropertyChangedCallback));

        public double AddHeight
        {
            get { return (double)GetValue (AddHeightProperty); }
            set { SetValue (AddHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AddHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AddHeightProperty =
            DependencyProperty.Register ("AddHeight", typeof (double), typeof (FlexPanel), new PropertyMetadata (0.0, PropertyChangedCallback));

        protected override Size MeasureOverride(Size constraint)
        {
            if (this.Children.Count == 0)
                return base.MeasureOverride (constraint);


            if (Orientation == Orientation.Horizontal)
            {
                double totalDesiredWidth = 0;
                double maxDesiredHeight = 0;

                foreach (UIElement child in Children)
                {
                    // 1. 자식 Panel에게 부모(당신의 커스텀 Panel)가 제공하는 availableSize를 전달하여 측정하게 합니다.
                    // 이때, 자식 Panel은 자신의 내부 로직에 따라 자신의 DesiredSize를 계산할 것입니다.
                    // 예를 들어, 자식 Panel도 자신의 자식들의 크기를 합산하거나, Stretch 동작을 하거나 할 것입니다.
                    child.Measure (constraint);

                    // 2. 자식 Panel의 DesiredSize를 얻습니다.
                    // 이 DesiredSize는 자식 Panel이 원하는 '최소한의 크기'입니다.
                    // Width나 Height 속성이 NaN인 경우에도 DesiredSize는 유효한 값을 가집니다.
                    // 예를 들어, 자식 Panel 안에 TextBlock이 있다면, TextBlock의 크기에 따라 DesiredSize가 계산됩니다.


                    Size childDesiredSize = child.DesiredSize;

                    totalDesiredWidth += childDesiredSize.Width;
                    maxDesiredHeight = Math.Max (maxDesiredHeight, childDesiredSize.Height);
                }
                return new Size (
                                    double.IsPositiveInfinity (constraint.Width) ? totalDesiredWidth : Math.Min (totalDesiredWidth, constraint.Width),
                                    double.IsPositiveInfinity (constraint.Height) ? maxDesiredHeight + AddHeight : Math.Min (maxDesiredHeight, constraint.Height) + AddHeight
                                );
            }

            double totalDesiredHeight = 0;
            double maxDesiredWidth = 0;

            foreach (UIElement child in Children)
            {
                // 1. 자식 Panel에게 부모(당신의 커스텀 Panel)가 제공하는 availableSize를 전달하여 측정하게 합니다.
                // 이때, 자식 Panel은 자신의 내부 로직에 따라 자신의 DesiredSize를 계산할 것입니다.
                // 예를 들어, 자식 Panel도 자신의 자식들의 크기를 합산하거나, Stretch 동작을 하거나 할 것입니다.
                child.Measure (constraint);

                // 2. 자식 Panel의 DesiredSize를 얻습니다.
                // 이 DesiredSize는 자식 Panel이 원하는 '최소한의 크기'입니다.
                // Width나 Height 속성이 NaN인 경우에도 DesiredSize는 유효한 값을 가집니다.
                // 예를 들어, 자식 Panel 안에 TextBlock이 있다면, TextBlock의 크기에 따라 DesiredSize가 계산됩니다.


                Size childDesiredSize = child.DesiredSize;

                maxDesiredWidth = Math.Max (maxDesiredWidth, childDesiredSize.Width);
                totalDesiredHeight += childDesiredSize.Height;
            }
            return new Size (
                                double.IsPositiveInfinity (constraint.Width) ? maxDesiredWidth : Math.Min (maxDesiredWidth, constraint.Width),
                                double.IsPositiveInfinity (constraint.Height) ? totalDesiredHeight + AddHeight : Math.Min (totalDesiredHeight, constraint.Height) + AddHeight
                            );

        }

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue == e.NewValue)
                return;
            try
            {
                var FlexPanel = ((FlexPanel)d);

                FlexPanel.InvalidateMeasure ();
                FlexPanel.InvalidateArrange ();
            }
            catch (Exception ex)
            {

            }
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            Arrange (finalSize);
            return finalSize;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged (sizeInfo);
            Arrange (this.RenderSize);
        }


        private void Arrange(Size finalSize)
        {
            if (Children.Count == 0)
                return;

            if (Children.Count == 1)
            {
                FrameworkElement child = (FrameworkElement)this.Children[0];
                child.Arrange (new Rect (0, 0, child.DesiredSize.Width, child.DesiredSize.Height));
                if (Align == AlignContent.Start)
                {
                    child.SetValue (VerticalAlignmentProperty, VerticalAlignment.Top);
                }
                else if (Align == AlignContent.Center)
                {
                    child.SetValue (VerticalAlignmentProperty, VerticalAlignment.Center);
                }
                else if (Align == AlignContent.End)
                {
                    child.SetValue (VerticalAlignmentProperty, VerticalAlignment.Bottom);
                }

                if (Justify == JustifyContent.Start)
                {
                    child.SetValue (HorizontalAlignmentProperty, HorizontalAlignment.Left);
                }
                else if (Justify == JustifyContent.Center)
                {
                    child.SetValue (HorizontalAlignmentProperty, HorizontalAlignment.Center);
                }
                else if (Justify == JustifyContent.End)
                {
                    child.SetValue (HorizontalAlignmentProperty, HorizontalAlignment.Right);
                }

                return;
            }

            if (Orientation == Orientation.Horizontal)
            {
                Make (finalSize);
                return;
            }
            VerticalMake (finalSize);
            return;
        }


        private Size Make(Size finalSize)
        {
            int childrenCount = this.Children.Count;
            double totalWidth = 0.0;
            double maxWidth = 0.0;
            for (int i = 0; i < childrenCount; i++)
            {
                UIElement child = this.Children[i];
                totalWidth += child.DesiredSize.Width;
                maxWidth = maxWidth > child.DesiredSize.Width ? maxWidth : child.DesiredSize.Width;
            }
            double xOffset = 0;
            double spacing = 0;
            double remainWidth = finalSize.Width - (maxWidth * childrenCount);
            if (Justify == JustifyContent.SpaceBetween)
            {
                UIElement firstElement = this.Children[0];
                UIElement lastElement = this.Children[childrenCount - 1];

                totalWidth = totalWidth - (firstElement.DesiredSize.Width + lastElement.DesiredSize.Width); // 실제 컨트롤 Width

                var temp = finalSize.Width - (firstElement.DesiredSize.Width + lastElement.DesiredSize.Width); // 전체 크기에서 첫번째와 마지막 컨트롤 뺀 크기

                var temp2 = temp - totalWidth;  // 실제 남는 공간
                spacing = temp2 / (childrenCount - 1);
            }
            else if (Justify == JustifyContent.SpaceAround)
            {
                var temp = finalSize.Width - totalWidth; // 남는 공간

                xOffset = temp / (childrenCount * 2);
                spacing = xOffset * 2;
            }
            else if (Justify == JustifyContent.SpaceEvenly)
            {
                var temp = finalSize.Width - totalWidth; // 남는 공간

                xOffset = temp / (childrenCount + 1);
                spacing = xOffset;
            }
            else if (Justify == JustifyContent.SpaceAuto)
            {
                spacing = maxWidth / 2;
                xOffset = (remainWidth - (spacing * (childrenCount - 1))) / 2;
            }
            else if (Justify == JustifyContent.Start)
            {
                xOffset = 0;
            }
            else if (Justify == JustifyContent.Center)
            {
                xOffset = (finalSize.Width - (maxWidth * childrenCount)) / 2;
            }
            else if (Justify == JustifyContent.End)
            {
                xOffset = finalSize.Width - (maxWidth * childrenCount);
            }

            for (int i = 0; i < childrenCount; i++)
            {
                FrameworkElement child = (FrameworkElement)this.Children[i];

                Rect childRect = new Rect (xOffset, 0, child.DesiredSize.Width, finalSize.Height);

                child.Arrange (childRect);

                if (Align == AlignContent.Start)
                {
                    child.SetValue (VerticalAlignmentProperty, VerticalAlignment.Top);
                }
                else if (Align == AlignContent.Center)
                {
                    child.SetValue (VerticalAlignmentProperty, VerticalAlignment.Center);
                }
                else if (Align == AlignContent.End)
                {
                    child.SetValue (VerticalAlignmentProperty, VerticalAlignment.Bottom);
                }

                xOffset += child.DesiredSize.Width + spacing;
            }
            return finalSize;
        }

        private Size VerticalMake(Size finalSize)
        {
            int childrenCount = this.Children.Count;
            double totalHeight = 0.0;
            double maxHeight = 0.0;

            for (int i = 0; i < childrenCount; i++)
            {
                UIElement child = this.Children[i];
                totalHeight += child.DesiredSize.Height;
                maxHeight = maxHeight > child.DesiredSize.Height ? maxHeight : child.DesiredSize.Height;
            }

            double yOffset = 0;
            double spacing = 0;
            double remainHeight = finalSize.Height - (maxHeight * childrenCount);

            if (Justify == JustifyContent.SpaceBetween)
            {
                spacing = remainHeight / (childrenCount - 1);
            }
            else if (Justify == JustifyContent.SpaceAround)
            {
                spacing = remainHeight / (childrenCount * 2);
                yOffset = spacing;
                spacing = spacing * 2;
            }
            else if (Justify == JustifyContent.SpaceEvenly)
            {
                spacing = remainHeight / (childrenCount + 1);
                yOffset = spacing;
            }
            else if (Justify == JustifyContent.SpaceAuto)
            {
                spacing = maxHeight / 2;
                yOffset = (remainHeight - (spacing * (childrenCount - 1))) / 2;
            }
            else if (Justify == JustifyContent.Start)
            {
                yOffset = 0;
            }
            else if (Justify == JustifyContent.Center)
            {
                yOffset = (finalSize.Height - (maxHeight * childrenCount)) / 2;
            }
            else if (Justify == JustifyContent.End)
            {
                yOffset = finalSize.Height - (maxHeight * childrenCount);
            }

            for (int i = 0; i < childrenCount; i++)
            {
                FrameworkElement child = (FrameworkElement)this.Children[i];

                Rect childRect = new Rect (0, yOffset, finalSize.Width, child.DesiredSize.Height);

                child.Arrange (childRect);

                if (Align == AlignContent.Start)
                {
                    child.SetValue (HorizontalAlignmentProperty, HorizontalAlignment.Left);
                }
                else if (Align == AlignContent.Center)
                {
                    child.SetValue (HorizontalAlignmentProperty, HorizontalAlignment.Center);
                }
                else if (Align == AlignContent.End)
                {
                    child.SetValue (HorizontalAlignmentProperty, HorizontalAlignment.Right);
                }

                yOffset += child.DesiredSize.Height + spacing;
            }

            return finalSize;
        }
    }
}
