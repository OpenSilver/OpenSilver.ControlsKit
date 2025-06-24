using System;
using System.Collections.Generic;
using System.Linq;
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
        SpaceEvenly
    }

    public enum AlignContent
    {
        Start,
        Center,
        End,
        SpaceBetween,
        SpaceAround,
        SpaceEvenly
    }

    public enum AlignItems
    {
        Start,
        Center,
        End,
        Stretch
    }

    public partial class FlexPanel : Panel
    {
        public Orientation Orientation
        {
            get => (Orientation)GetValue (OrientationProperty);
            set => SetValue (OrientationProperty, value);
        }

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register (nameof (Orientation), typeof (Orientation), typeof (FlexPanel),
                new FrameworkPropertyMetadata (Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsArrange));

        public JustifyContent JustifyContent
        {
            get => (JustifyContent)GetValue (JustifyContentProperty);
            set => SetValue (JustifyContentProperty, value);
        }

        public static readonly DependencyProperty JustifyContentProperty =
            DependencyProperty.Register (nameof (JustifyContent), typeof (JustifyContent), typeof (FlexPanel),
                new FrameworkPropertyMetadata (JustifyContent.Start, FrameworkPropertyMetadataOptions.AffectsArrange));

        public AlignContent AlignContent
        {
            get => (AlignContent)GetValue (AlignContentProperty);
            set => SetValue (AlignContentProperty, value);
        }

        public static readonly DependencyProperty AlignContentProperty =
            DependencyProperty.Register (nameof (AlignContent), typeof (AlignContent), typeof (FlexPanel),
                new FrameworkPropertyMetadata (AlignContent.Start, FrameworkPropertyMetadataOptions.AffectsArrange));

        public AlignItems AlignItems
        {
            get => (AlignItems)GetValue (AlignItemsProperty);
            set => SetValue (AlignItemsProperty, value);
        }

        public static readonly DependencyProperty AlignItemsProperty =
            DependencyProperty.Register (nameof (AlignItems), typeof (AlignItems), typeof (FlexPanel),
                new FrameworkPropertyMetadata (AlignItems.Start, FrameworkPropertyMetadataOptions.AffectsArrange));
        public double Spacing
        {
            get => (double)GetValue (SpacingProperty);
            set => SetValue (SpacingProperty, value);
        }

        public static readonly DependencyProperty SpacingProperty =
            DependencyProperty.Register (nameof (Spacing), typeof (double), typeof (FlexPanel),
                new FrameworkPropertyMetadata (0.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        protected override Size MeasureOverride(Size availableSize)
        {
            bool isHorizontal = Orientation == Orientation.Horizontal;
            double maxPrimary = isHorizontal ? availableSize.Width : availableSize.Height;

            double panelPrimary = 0;
            double panelCross = 0;

            double curPrimary = 0;
            double curCross = 0;

            bool disableSpacing = IsSpacingDisabledForJustify ();

            foreach (UIElement child in Children)
            {
                child.Measure (availableSize);
                var desired = child.DesiredSize;
                double childPrimary = isHorizontal ? desired.Width : desired.Height;
                double childCross = isHorizontal ? desired.Height : desired.Width;

                double spacing = (!disableSpacing && curPrimary > 0) ? Spacing : 0;

                if (curPrimary + childPrimary + spacing > maxPrimary && curPrimary > 0)
                {
                    panelPrimary = Math.Max (panelPrimary, curPrimary);
                    panelCross += curCross;
                    curPrimary = childPrimary;
                    curCross = childCross;
                }
                else
                {
                    curPrimary += childPrimary + spacing;
                    curCross = Math.Max (curCross, childCross);
                }
            }

            panelPrimary = Math.Max (panelPrimary, curPrimary);
            panelCross += curCross;

            return isHorizontal ? new Size (panelPrimary, panelCross) : new Size (panelCross, panelPrimary);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            bool isHorizontal = Orientation == Orientation.Horizontal;
            double maxPrimary = isHorizontal ? finalSize.Width : finalSize.Height;

            var lines = new List<List<UIElement>> ();
            var lineSizes = new List<Size> ();

            double curPrimary = 0;
            double curCross = 0;
            var curLine = new List<UIElement> ();

            bool disableSpacing = JustifyContent == JustifyContent.SpaceBetween ||
                                  JustifyContent == JustifyContent.SpaceAround ||
                                  JustifyContent == JustifyContent.SpaceEvenly;

            // 1. Line construction: Group child elements into lines based on available primary size.
            foreach (UIElement child in Children)
            {
                var desired = child.DesiredSize;
                double childPrimary = isHorizontal ? desired.Width : desired.Height;
                double childCross = isHorizontal ? desired.Height : desired.Width;

                double spacing = (!disableSpacing && curPrimary > 0) ? Spacing : 0;

                if (curPrimary + childPrimary + spacing > maxPrimary && curPrimary > 0)
                {
                    lines.Add (curLine);
                    lineSizes.Add (isHorizontal ? new Size (curPrimary, curCross) : new Size (curCross, curPrimary));

                    curLine = new List<UIElement> { child };
                    curPrimary = childPrimary;
                    curCross = childCross;
                }
                else
                {
                    curLine.Add (child);
                    curPrimary += childPrimary + spacing;
                    curCross = Math.Max (curCross, childCross);
                }
            }

            if (curLine.Count > 0)
            {
                lines.Add (curLine);
                lineSizes.Add (isHorizontal ? new Size (curPrimary, curCross) : new Size (curCross, curPrimary));
            }

            // 2. Handle AlignContent: Calculate spacing between lines based on remaining cross size.
            double totalCrossSize = lineSizes.Sum (sz => isHorizontal ? sz.Height : sz.Width);
            double remainingCross = (isHorizontal ? finalSize.Height : finalSize.Width) - totalCrossSize;

            double crossOffsetBase = 0;
            double crossSpacing = 0;

            switch (AlignContent)
            {
                case AlignContent.Start:
                    crossOffsetBase = 0;
                    crossSpacing = 0;
                    break;
                case AlignContent.Center:
                    crossOffsetBase = remainingCross / 2;
                    crossSpacing = 0;
                    break;
                case AlignContent.End:
                    crossOffsetBase = remainingCross;
                    crossSpacing = 0;
                    break;
                case AlignContent.SpaceBetween:
                    crossSpacing = lines.Count > 1 ? remainingCross / (lines.Count - 1) : 0;
                    break;
                case AlignContent.SpaceAround:
                    crossSpacing = lines.Count > 0 ? remainingCross / lines.Count : 0;
                    crossOffsetBase = crossSpacing / 2;
                    break;
                case AlignContent.SpaceEvenly:
                    crossSpacing = lines.Count > 0 ? remainingCross / (lines.Count + 1) : 0;
                    crossOffsetBase = crossSpacing;
                    break;
            }

            // 3. Arrange each line: Position and space items within each line based on JustifyContent.
            double crossPos = crossOffsetBase;

            foreach (var (line, lineSize) in lines.Zip (lineSizes, Tuple.Create))
            {
                double totalPrimary = line.Sum (child => isHorizontal ? child.DesiredSize.Width : child.DesiredSize.Height);
                double totalFlexGrow = line.Sum (child => GetGrow (child));

                double remainingPrimary = maxPrimary - totalPrimary - (disableSpacing ? 0 : Spacing * (line.Count - 1));

                double spacingBetweenChildren = 0;
                double primaryOffset = 0;

                if (disableSpacing)
                {
                    switch (JustifyContent)
                    {
                        case JustifyContent.SpaceBetween:
                            spacingBetweenChildren = line.Count > 1 ? remainingPrimary / (line.Count - 1) : 0;
                            primaryOffset = 0;
                            break;
                        case JustifyContent.SpaceAround:
                            spacingBetweenChildren = line.Count > 0 ? remainingPrimary / line.Count : 0;
                            primaryOffset = spacingBetweenChildren / 2;
                            break;
                        case JustifyContent.SpaceEvenly:
                            spacingBetweenChildren = line.Count > 0 ? remainingPrimary / (line.Count + 1) : 0;
                            primaryOffset = spacingBetweenChildren;
                            break;
                    }
                }
                else
                {
                    spacingBetweenChildren = Spacing;

                    switch (JustifyContent)
                    {
                        case JustifyContent.Start:
                            primaryOffset = 0;
                            break;
                        case JustifyContent.Center:
                            primaryOffset = remainingPrimary / 2;
                            break;
                        case JustifyContent.End:
                            primaryOffset = remainingPrimary;
                            break;
                    }
                }

                // 4. Arrange items within the line: Compute actual position and size for each child element.

                double curPrimaryPos = primaryOffset;

                foreach (var child in line)
                {
                    var desired = child.DesiredSize;
                    double childPrimary = isHorizontal ? desired.Width : desired.Height;
                    double childCross = isHorizontal ? desired.Height : desired.Width;

                    // Apply flex-grow:
                    // Only applies when JustifyContent is Start, Center, or End.
                    // Distributes remaining space proportionally based on Grow value.

                    if (totalFlexGrow > 0 &&
                        (JustifyContent == JustifyContent.Start ||
                         JustifyContent == JustifyContent.Center ||
                         JustifyContent == JustifyContent.End))
                    {
                        double flexGrow = GetGrow (child);
                        double extra = remainingPrimary * (flexGrow / totalFlexGrow);
                        childPrimary += extra;
                    }
                    
                    double crossAlignOffset = AlignItems switch
                    {
                        AlignItems.Start => 0,
                        AlignItems.Center => ((isHorizontal ? lineSize.Height : lineSize.Width) - childCross) / 2,
                        AlignItems.End => ((isHorizontal ? lineSize.Height : lineSize.Width) - childCross),
                        AlignItems.Stretch => 0,
                        _ => 0
                    };
                    if(IsSpacingDisabledForJustify () && IsSpacingDisabledForAlignContent())
                    {
                        crossAlignOffset = 0;
                    }
                    double x, y, w, h;

                    if (isHorizontal)
                    {
                        x = curPrimaryPos;
                        y = crossPos + crossAlignOffset;
                        w = childPrimary;
                        h = (AlignItems == AlignItems.Stretch) ? finalSize.Height : desired.Height;
                    }
                    else
                    {
                        x = crossPos + crossAlignOffset;
                        y = curPrimaryPos;
                        w = (AlignItems == AlignItems.Stretch) ? finalSize.Width : desired.Width;
                        h = childPrimary;
                    }

                    child.Arrange (new Rect (x, y, w, h));
                    curPrimaryPos += childPrimary + spacingBetweenChildren;
                }

                crossPos += (isHorizontal ? lineSize.Height : lineSize.Width) + crossSpacing;
            }

            return finalSize;
        }
        // Check if spacing should be ignored for JustifyContent modes
        // like SpaceBetween, SpaceAround, or SpaceEvenly.

        private bool IsSpacingDisabledForJustify()
        {
            return JustifyContent == JustifyContent.SpaceBetween
                || JustifyContent == JustifyContent.SpaceAround
                || JustifyContent == JustifyContent.SpaceEvenly;
        }

        // Check if spacing should be ignored for AlignContent modes
        // like SpaceBetween, SpaceAround, or SpaceEvenly.

        private bool IsSpacingDisabledForAlignContent()
        {
            return AlignContent == AlignContent.SpaceBetween
                || AlignContent == AlignContent.SpaceAround
                || AlignContent == AlignContent.SpaceEvenly;
        }
    }
}
