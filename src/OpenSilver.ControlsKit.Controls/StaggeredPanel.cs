/*===================================================================================
*
*   Copyright (c) Userware (OpenSilver.net)
*
*   This file is part of the OpenSilver.ControlsKit (https://opensilver.net), which
*   is licensed under the MIT license (https://opensource.org/licenses/MIT).
*
*   As stated in the MIT license, "the above copyright notice and this permission
*   notice shall be included in all copies or substantial portions of the Software."
*
*====================================================================================*/

// Adapted from Microsoft.Toolkit.Uwp.UI.Controls.StaggeredPanel
// cf. https://github.com/CommunityToolkit/WindowsCommunityToolkit/blob/main/Microsoft.Toolkit.Uwp.UI.Controls.Primitives/StaggeredPanel/StaggeredPanel.cs
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows;
//using Windows.Foundation;
//using Windows.UI.Xaml;
//using Windows.UI.Xaml.Controls;

namespace OpenSilver.ControlsKit
{
    /// <summary>
    /// Arranges child elements into a staggered grid pattern where items are added to the column that has used least amount of space.
    /// </summary>
    public class StaggeredPanel : Panel
    {
        private double _columnWidth;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaggeredPanel"/> class.
        /// </summary>
        public StaggeredPanel()
        {
            //ProgressiveRenderingChunkSize = 1;
            //RegisterPropertyChangedCallback(Panel.HorizontalAlignmentProperty, OnHorizontalAlignmentChanged);
        }

        /// <summary>
        /// Gets or sets the desired width for each column.
        /// </summary>
        /// <remarks>
        /// The width of columns can exceed the DesiredColumnWidth if the HorizontalAlignment is set to Stretch.
        /// </remarks>
        public double DesiredColumnWidth
        {
            get { return (double)GetValue(DesiredColumnWidthProperty); }
            set { SetValue(DesiredColumnWidthProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="DesiredColumnWidth"/> dependency property.
        /// </summary>
        /// <returns>The identifier for the <see cref="DesiredColumnWidth"/> dependency property.</returns>
        public static readonly DependencyProperty DesiredColumnWidthProperty = DependencyProperty.Register(
            nameof(DesiredColumnWidth),
            typeof(double),
            typeof(StaggeredPanel),
            new PropertyMetadata(250d, OnDesiredColumnWidthChanged));

        /// <summary>
        /// Gets or sets the distance between the border and its child object.
        /// </summary>
        /// <returns>
        /// The dimensions of the space between the border and its child as a Thickness value.
        /// Thickness is a structure that stores dimension values using pixel measures.
        /// </returns>
        public Thickness Padding
        {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        /// <summary>
        /// Identifies the Padding dependency property.
        /// </summary>
        /// <returns>The identifier for the <see cref="Padding"/> dependency property.</returns>
        public static readonly DependencyProperty PaddingProperty = DependencyProperty.Register(
            nameof(Padding),
            typeof(Thickness),
            typeof(StaggeredPanel),
            new PropertyMetadata(default(Thickness), OnPaddingChanged));

        /// <summary>
        /// Gets or sets the spacing between columns of items.
        /// </summary>
        public double ColumnSpacing
        {
            get { return (double)GetValue(ColumnSpacingProperty); }
            set { SetValue(ColumnSpacingProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ColumnSpacing"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColumnSpacingProperty = DependencyProperty.Register(
            nameof(ColumnSpacing),
            typeof(double),
            typeof(StaggeredPanel),
            new PropertyMetadata(0d, OnPaddingChanged));

        /// <summary>
        /// Gets or sets the spacing between rows of items.
        /// </summary>
        public double RowSpacing
        {
            get { return (double)GetValue(RowSpacingProperty); }
            set { SetValue(RowSpacingProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="RowSpacing"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowSpacingProperty = DependencyProperty.Register(
            nameof(RowSpacing),
            typeof(double),
            typeof(StaggeredPanel),
            new PropertyMetadata(0d, OnPaddingChanged));

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            double availableWidth = availableSize.Width - Padding.Left - Padding.Right;
            double availableHeight = availableSize.Height - Padding.Top - Padding.Bottom;

            _columnWidth = Math.Min(DesiredColumnWidth, availableWidth);
            int numColumns = Math.Max(1, availableWidth == double.PositiveInfinity ? -1 : (int)Math.Floor(availableWidth / _columnWidth));

            // adjust for column spacing on all columns expect the first
            double totalWidth = _columnWidth + ((numColumns - 1) * (_columnWidth + ColumnSpacing));
            if (totalWidth > availableWidth)
            {
                numColumns--;
            }
            else if (double.IsInfinity(availableWidth))
            {
                availableWidth = totalWidth;
            }

            if (HorizontalAlignment == HorizontalAlignment.Stretch)
            {
                availableWidth = availableWidth - ((numColumns - 1) * ColumnSpacing);
                _columnWidth = availableWidth / numColumns;
            }

            if (Children.Count == 0)
            {
                return new Size(0, 0);
            }

            var columnHeights = new double[numColumns];
            var itemsPerColumn = new double[numColumns];

            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                child.Measure(new Size(availableWidth, availableHeight));
                var elementSize = child.DesiredSize;

                var columnIndex = GetPlacementInformations(columnHeights, elementSize, out double newHeight, out int heightDefiningColumnIndex);
                int elementColumnSpan = GetElementColumnSpan(elementSize.Width);

                for (int k = 0; k < elementColumnSpan; ++k)
                {
                    columnHeights[columnIndex + k] = newHeight + (itemsPerColumn[columnIndex] > 0 ? RowSpacing : 0);
                    itemsPerColumn[columnIndex + k]++;
                }
                itemsPerColumn[columnIndex]++;
            }

            double desiredHeight = columnHeights.Max();

            return new Size(availableWidth, desiredHeight);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            double horizontalOffset = Padding.Left;
            double verticalOffset = Padding.Top;
            int numColumns = Math.Max(1, (int)Math.Floor(finalSize.Width / _columnWidth));

            // adjust for horizontal spacing on all columns expect the first
            double totalWidth = _columnWidth + ((numColumns - 1) * (_columnWidth + ColumnSpacing));
            #region Explanation for the -0.01 in the if below:
            // we compare to totalWidth -0.01 because such a small overflow wouldn't be visible and it is still a likely scenario:
            // If we are Stretched, MeasureOverride will have changed _columnWidth to availableWidth/numColumns, which can be rounded higher than the actual value.
            // For example, we can end up with 100.16666667, which is higher than the non-rounded value of 100.166666666...
            // A simplified version of what we actually do, assuming ColumnSpacing is 0, would result in something akin to saying: (availableWidth / numColumns) * numcolumns > availableWidth which does not make sense.
            #endregion
            if (totalWidth - 0.01 > finalSize.Width)
            {
                numColumns--;

                // Need to recalculate the totalWidth for a correct horizontal offset
                totalWidth = _columnWidth + ((numColumns - 1) * (_columnWidth + ColumnSpacing));
            }

            if (HorizontalAlignment == HorizontalAlignment.Right)
            {
                horizontalOffset += finalSize.Width - totalWidth;
            }
            else if (HorizontalAlignment == HorizontalAlignment.Center)
            {
                horizontalOffset += (finalSize.Width - totalWidth) / 2;
            }

            var columnHeights = new double[numColumns];
            var itemsPerColumn = new double[numColumns];

            for (int i = 0; i < Children.Count; i++)
            {

                var child = Children[i];
                var elementSize = child.DesiredSize;
                double elementHeight = elementSize.Height;

                //get the element's column span:
                double elementWidth = elementSize.Width;
                int elementColumnsSpan = Math.Max(1, (int)Math.Ceiling((elementWidth - _columnWidth) / (_columnWidth + ColumnSpacing)) + 1);

                int columnIndex;

                columnIndex = GetPlacementInformations(columnHeights, elementSize, out double newHeight, out int heightDefiningColumnIndex);

                double itemHorizontalOffset = horizontalOffset + (_columnWidth * columnIndex) + (ColumnSpacing * columnIndex);
                double itemVerticalOffset = columnHeights[heightDefiningColumnIndex] + verticalOffset + (RowSpacing * itemsPerColumn[heightDefiningColumnIndex]);

                Rect bounds = new Rect(itemHorizontalOffset, itemVerticalOffset, elementWidth, elementHeight);
                child.Arrange(bounds);

                for (int k = 0; k < elementColumnsSpan; ++k)
                {
                    columnHeights[columnIndex + k] = newHeight;
                    itemsPerColumn[columnIndex + k]++;
                }
            }

            return base.ArrangeOverride(finalSize);
        }

        private static void OnDesiredColumnWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (StaggeredPanel)d;
            panel.InvalidateMeasure();
        }

        private static void OnPaddingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (StaggeredPanel)d;
            panel.InvalidateMeasure();
        }

        private void OnHorizontalAlignmentChanged(DependencyObject sender, DependencyProperty dp)
        {
            InvalidateMeasure();
        }

        private int GetElementColumnSpan(double elementWidth)
        {
            return Math.Max(1, (int)Math.Ceiling((elementWidth - _columnWidth) / (_columnWidth + ColumnSpacing)) + 1);
        }

        private int GetPlacementInformations(double[] columnHeights, Size elementSize, out double newHeight, out int heightDefiningColumnIndex)
        {
            double elementHeight = elementSize.Height;

            //get the element's column span:
            double elementWidth = elementSize.Width;
            int elementColumnSpan = GetElementColumnSpan(elementWidth);

            int columnIndex = 0;
            if (elementColumnSpan == 1)
            {
                columnIndex = GetColumnIndex(columnHeights);
                newHeight = columnHeights[columnIndex] + elementHeight;
                heightDefiningColumnIndex = columnIndex;
            }
            else
            {
                columnIndex = GetColumnIndex(columnHeights, elementColumnSpan, out newHeight, out heightDefiningColumnIndex);
                newHeight += elementHeight;
            }
            return columnIndex;
        }

        private int GetColumnIndex(double[] columnHeights)
        {
            int columnIndex = 0;
            double height = columnHeights[0];
            for (int j = 1; j < columnHeights.Length; j++)
            {
                if (columnHeights[j] < height)
                {
                    columnIndex = j;
                    height = columnHeights[j];
                }
            }

            return columnIndex;
        }

        private int GetColumnIndex(double[] columnHeights, int elementColumnSpan, out double bestHeight, out int heightDefiningColumnIndex)
        {
            if (columnHeights.Length <= elementColumnSpan)
            {
                // there is no option on where to put the element anyway so let's just return 0.
                bestHeight = columnHeights[0];
                heightDefiningColumnIndex = 0;
                return 0;
            }

            //initialization:
            int bestIndex = 0;
            bestHeight = double.MinValue;
            heightDefiningColumnIndex = 0;
            for (int i = 0; i < elementColumnSpan; ++i)
            {
                if (bestHeight < columnHeights[i])
                {
                    bestHeight = columnHeights[i];
                    heightDefiningColumnIndex = i;
                }
            }
            int lastColumnIndex = columnHeights.Length - elementColumnSpan + 1;

            //we look for the set of columns that will allow us to put the element with the smallest vertical offset:
            for (int j = 1; j < lastColumnIndex; j++)
            {
                //We get the height of the new column to consider:
                double newHeight = columnHeights[j + elementColumnSpan - 1];
                if (newHeight > bestHeight)
                {
                    //we can exclude any set of columns that include the new column since at best, it won't be as good as what we have already found:
                    j += elementColumnSpan - 1; // -1 because the loop will also add 1.
                    continue;
                }

                //Calculate the height of the current set of columns:
                //Note: (perf) we could also read the height on j-1 and if that column's height is < bestHeight, it means that column was not the limiting one in the previous loop so no need to recalculate.
                double currentHeight = double.MinValue;
                for (int i = 0; i < elementColumnSpan; ++i)
                {
                    if (currentHeight < columnHeights[j + i])
                    {
                        currentHeight = columnHeights[j + i];
                        heightDefiningColumnIndex = j + i;
                    }
                }

                if (currentHeight < bestHeight)
                {
                    bestHeight = currentHeight;
                    bestIndex = j;
                }

            }

            return bestIndex;
        }
    }
}