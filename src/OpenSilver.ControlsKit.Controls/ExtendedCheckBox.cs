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

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OpenSilver.ControlsKit.Controls
{
    /// <summary>
    /// Specifies the size of the checkbox control.
    /// </summary>
    public enum CheckBoxSize
    {
        /// <summary>
        /// Small checkbox size.
        /// </summary>
        Small,
        /// <summary>
        /// Medium checkbox size.
        /// </summary>
        Medium,
        /// <summary>
        /// Large checkbox size.
        /// </summary>
        Large,
        /// <summary>
        /// Custom checkbox size.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Specifies the style of the check mark.
    /// </summary>
    public enum CheckMarkStyle
    {
        /// <summary>
        /// Check mark is visible using the current CheckMarkGeometry.
        /// </summary>
        Show,
        /// <summary>
        /// No check mark displayed.
        /// </summary>
        None
    }

    /// <summary>
    /// Specifies the animation style for the checkbox transition.
    /// </summary>
    public enum CheckBoxAnimationStyle
    {
        /// <summary>
        /// Smooth linear animation.
        /// </summary>
        Linear,
        /// <summary>
        /// Ease-in-out animation.
        /// </summary>
        EaseInOut,
        /// <summary>
        /// Bounce animation.
        /// </summary>
        Bounce,
        /// <summary>
        /// No animation.
        /// </summary>
        None
    }

    /// <summary>
    /// Specifies the position of the text relative to the checkbox.
    /// </summary>
    public enum TextPosition
    {
        /// <summary>
        /// Text is positioned to the left of the checkbox.
        /// </summary>
        Left,
        /// <summary>
        /// Text is positioned above the checkbox.
        /// </summary>
        Top,
        /// <summary>
        /// Text is positioned to the right of the checkbox.
        /// </summary>
        Right,
        /// <summary>
        /// Text is positioned below the checkbox.
        /// </summary>
        Bottom
    }

    /// <summary>
    /// A customizable checkbox control with smooth animations, hover effects, and flexible styling options.
    /// Provides advanced visual capabilities including shadows, custom colors for different states, and smooth transitions.
    /// </summary>
    public class ExtendedCheckBox : CheckBox
    {
        #region Private Fields
        private Border _checkBoxBorder;
        private Path _checkMarkPath;
        private Grid _contentGrid;
        private TextBlock _textBlock;
        private DropShadowEffect _shadowEffect;
        private bool _isHovered;
        private bool _templateApplied;
        private DispatcherTimer _animationTimer;
        private DateTime _animationStartTime;
        private IEasingFunction _currentEasingFunction;
        private double _animationStartOpacity;
        private double _animationTargetOpacity;
        private TransformGroup _checkMarkTransformGroup;
        private ScaleTransform _checkMarkScaleTransform;
        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="CheckBoxSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CheckBoxSizeProperty =
            DependencyProperty.Register(nameof(CheckBoxSize), typeof(CheckBoxSize), typeof(ExtendedCheckBox),
                new PropertyMetadata(CheckBoxSize.Medium, OnCheckBoxSizeChanged));

        /// <summary>
        /// Identifies the <see cref="BoxWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BoxWidthProperty =
            DependencyProperty.Register(nameof(BoxWidth), typeof(double), typeof(ExtendedCheckBox),
                new PropertyMetadata(20.0, OnLayoutPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="BoxHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BoxHeightProperty =
            DependencyProperty.Register(nameof(BoxHeight), typeof(double), typeof(ExtendedCheckBox),
                new PropertyMetadata(20.0, OnLayoutPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(ExtendedCheckBox),
                new PropertyMetadata(new CornerRadius(3), OnCornerRadiusChanged));

        /// <summary>
        /// Identifies the <see cref="CheckMarkStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CheckMarkStyleProperty =
            DependencyProperty.Register(nameof(CheckMarkStyle), typeof(CheckMarkStyle), typeof(ExtendedCheckBox),
                new PropertyMetadata(CheckMarkStyle.Show, OnCheckMarkStyleChanged));

        /// <summary>
        /// Identifies the <see cref="CheckMarkGeometry"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CheckMarkGeometryProperty =
            DependencyProperty.Register(nameof(CheckMarkGeometry), typeof(Geometry), typeof(ExtendedCheckBox),
                new PropertyMetadata(GetDefaultCheckMarkGeometry(), OnCheckMarkGeometryChanged));

        /// <summary>
        /// Identifies the <see cref="CheckMarkBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CheckMarkBrushProperty =
            DependencyProperty.Register(nameof(CheckMarkBrush), typeof(Brush), typeof(ExtendedCheckBox),
                new PropertyMetadata(new SolidColorBrush(Colors.White), OnCheckMarkBrushChanged));

        /// <summary>
        /// Identifies the <see cref="CheckMarkSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CheckMarkSizeProperty =
            DependencyProperty.Register(nameof(CheckMarkSize), typeof(double), typeof(ExtendedCheckBox),
                new PropertyMetadata(12.0, OnCheckMarkSizeChanged));

        /// <summary>
        /// Identifies the <see cref="BoxBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BoxBrushProperty =
            DependencyProperty.Register(nameof(BoxBrush), typeof(Brush), typeof(ExtendedCheckBox),
                new PropertyMetadata(new SolidColorBrush(Colors.White), OnBoxBrushChanged));

        /// <summary>
        /// Identifies the <see cref="CheckedBoxBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CheckedBoxBrushProperty =
            DependencyProperty.Register(nameof(CheckedBoxBrush), typeof(Brush), typeof(ExtendedCheckBox),
                new PropertyMetadata(new SolidColorBrush(Colors.DodgerBlue), OnBoxBrushChanged));

        /// <summary>
        /// Identifies the <see cref="HoverBoxBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HoverBoxBrushProperty =
            DependencyProperty.Register(nameof(HoverBoxBrush), typeof(Brush), typeof(ExtendedCheckBox),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="HoverCheckedBoxBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HoverCheckedBoxBrushProperty =
            DependencyProperty.Register(nameof(HoverCheckedBoxBrush), typeof(Brush), typeof(ExtendedCheckBox),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="BoxBorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BoxBorderBrushProperty =
            DependencyProperty.Register(nameof(BoxBorderBrush), typeof(Brush), typeof(ExtendedCheckBox),
                new PropertyMetadata(new SolidColorBrush(Colors.Gray), OnBoxBorderBrushChanged));

        /// <summary>
        /// Identifies the <see cref="CheckedBoxBorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CheckedBoxBorderBrushProperty =
            DependencyProperty.Register(nameof(CheckedBoxBorderBrush), typeof(Brush), typeof(ExtendedCheckBox),
                new PropertyMetadata(null, OnBoxBorderBrushChanged));

        /// <summary>
        /// Identifies the <see cref="BoxBorderThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BoxBorderThicknessProperty =
            DependencyProperty.Register(nameof(BoxBorderThickness), typeof(Thickness), typeof(ExtendedCheckBox),
                new PropertyMetadata(new Thickness(1), OnBoxBorderThicknessChanged));

        /// <summary>
        /// Identifies the <see cref="HasShadow"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HasShadowProperty =
            DependencyProperty.Register(nameof(HasShadow), typeof(bool), typeof(ExtendedCheckBox),
                new PropertyMetadata(false, OnShadowChanged));

        /// <summary>
        /// Identifies the <see cref="AnimationDuration"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AnimationDurationProperty =
            DependencyProperty.Register(nameof(AnimationDuration), typeof(TimeSpan), typeof(ExtendedCheckBox),
                new PropertyMetadata(TimeSpan.FromMilliseconds(200)));

        /// <summary>
        /// Identifies the <see cref="AnimationStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AnimationStyleProperty =
            DependencyProperty.Register(nameof(AnimationStyle), typeof(CheckBoxAnimationStyle), typeof(ExtendedCheckBox),
                new PropertyMetadata(CheckBoxAnimationStyle.EaseInOut));

        /// <summary>
        /// Identifies the <see cref="Text"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(ExtendedCheckBox),
                new PropertyMetadata("", OnTextChanged));

        /// <summary>
        /// Identifies the <see cref="TextPosition"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextPositionProperty =
            DependencyProperty.Register(nameof(TextPosition), typeof(TextPosition), typeof(ExtendedCheckBox),
                new PropertyMetadata(TextPosition.Right, OnLayoutPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="TextMargin"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextMarginProperty =
            DependencyProperty.Register(nameof(TextMargin), typeof(Thickness), typeof(ExtendedCheckBox),
                new PropertyMetadata(new Thickness(8, 0, 0, 0), OnLayoutPropertyChanged));

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the predefined size of the checkbox.
        /// </summary>
        /// <value>The checkbox size. Default is Medium.</value>
        public CheckBoxSize CheckBoxSize
        {
            get => (CheckBoxSize)GetValue(CheckBoxSizeProperty);
            set => SetValue(CheckBoxSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the width of the checkbox.
        /// </summary>
        /// <value>The box width in device-independent pixels. Default is 20.</value>
        public double BoxWidth
        {
            get => (double)GetValue(BoxWidthProperty);
            set => SetValue(BoxWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the height of the checkbox.
        /// </summary>
        /// <value>The box height in device-independent pixels. Default is 20.</value>
        public double BoxHeight
        {
            get => (double)GetValue(BoxHeightProperty);
            set => SetValue(BoxHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the corner radius of the checkbox.
        /// </summary>
        /// <value>The corner radius. Default is 3. Set to half of box size for circular appearance.</value>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Gets or sets the style of the check mark.
        /// </summary>
        /// <value>The check mark style. Default is Show.</value>
        public CheckMarkStyle CheckMarkStyle
        {
            get => (CheckMarkStyle)GetValue(CheckMarkStyleProperty);
            set => SetValue(CheckMarkStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the geometry used for the check mark.
        /// </summary>
        /// <value>The check mark geometry. Default is Material Design check mark.</value>
        public Geometry CheckMarkGeometry
        {
            get => (Geometry)GetValue(CheckMarkGeometryProperty);
            set => SetValue(CheckMarkGeometryProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for the check mark.
        /// </summary>
        /// <value>The check mark brush. Default is white.</value>
        public Brush CheckMarkBrush
        {
            get => (Brush)GetValue(CheckMarkBrushProperty);
            set => SetValue(CheckMarkBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the size of the check mark.
        /// </summary>
        /// <value>The check mark size in device-independent pixels. Default is 12.</value>
        public double CheckMarkSize
        {
            get => (double)GetValue(CheckMarkSizeProperty);
            set => SetValue(CheckMarkSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for the checkbox background when unchecked.
        /// </summary>
        /// <value>The box brush. Default is white.</value>
        public Brush BoxBrush
        {
            get => (Brush)GetValue(BoxBrushProperty);
            set => SetValue(BoxBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for the checkbox background when checked.
        /// </summary>
        /// <value>The checked box brush. Default is dodger blue.</value>
        public Brush CheckedBoxBrush
        {
            get => (Brush)GetValue(CheckedBoxBrushProperty);
            set => SetValue(CheckedBoxBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for the checkbox when hovered and unchecked.
        /// </summary>
        /// <value>The hover box brush. If null, a calculated lighter color is used.</value>
        public Brush HoverBoxBrush
        {
            get => (Brush)GetValue(HoverBoxBrushProperty);
            set => SetValue(HoverBoxBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for the checkbox when hovered and checked.
        /// </summary>
        /// <value>The hover checked box brush. If null, a calculated lighter color is used.</value>
        public Brush HoverCheckedBoxBrush
        {
            get => (Brush)GetValue(HoverCheckedBoxBrushProperty);
            set => SetValue(HoverCheckedBoxBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the border brush of the checkbox when unchecked.
        /// </summary>
        /// <value>The box border brush. Default is gray.</value>
        public Brush BoxBorderBrush
        {
            get => (Brush)GetValue(BoxBorderBrushProperty);
            set => SetValue(BoxBorderBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the border brush of the checkbox when checked.
        /// </summary>
        /// <value>The checked box border brush. If null, uses the same as CheckedBoxBrush.</value>
        public Brush CheckedBoxBorderBrush
        {
            get => (Brush)GetValue(CheckedBoxBorderBrushProperty);
            set => SetValue(CheckedBoxBorderBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the border thickness of the checkbox.
        /// </summary>
        /// <value>The box border thickness. Default is 1.</value>
        public Thickness BoxBorderThickness
        {
            get => (Thickness)GetValue(BoxBorderThicknessProperty);
            set => SetValue(BoxBorderThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the checkbox has a drop shadow effect.
        /// </summary>
        /// <value>true if the checkbox has a shadow; otherwise, false. Default is false.</value>
        public bool HasShadow
        {
            get => (bool)GetValue(HasShadowProperty);
            set => SetValue(HasShadowProperty, value);
        }

        /// <summary>
        /// Gets or sets the duration of the check animation.
        /// </summary>
        /// <value>The animation duration. Default is 200 milliseconds.</value>
        public TimeSpan AnimationDuration
        {
            get => (TimeSpan)GetValue(AnimationDurationProperty);
            set => SetValue(AnimationDurationProperty, value);
        }

        /// <summary>
        /// Gets or sets the animation style for the check transition.
        /// </summary>
        /// <value>The animation style. Default is EaseInOut.</value>
        public CheckBoxAnimationStyle AnimationStyle
        {
            get => (CheckBoxAnimationStyle)GetValue(AnimationStyleProperty);
            set => SetValue(AnimationStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the text content of the checkbox.
        /// </summary>
        /// <value>The text content. Takes precedence over the Content property for text display.</value>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Gets or sets the position of the text relative to the checkbox.
        /// </summary>
        /// <value>The text position. Default is Right.</value>
        public TextPosition TextPosition
        {
            get => (TextPosition)GetValue(TextPositionProperty);
            set => SetValue(TextPositionProperty, value);
        }

        /// <summary>
        /// Gets or sets the margin around the text.
        /// </summary>
        /// <value>The text margin. Default is 8,0,0,0 (8px right margin).</value>
        public Thickness TextMargin
        {
            get => (Thickness)GetValue(TextMarginProperty);
            set => SetValue(TextMarginProperty, value);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Static constructor to override metadata for IsChecked property.
        /// </summary>
        static ExtendedCheckBox()
        {
            // Override IsChecked property to handle programmatic changes
            IsCheckedProperty.OverrideMetadata(typeof(ExtendedCheckBox),
                new FrameworkPropertyMetadata(OnIsCheckedChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedCheckBox"/> class.
        /// </summary>
        public ExtendedCheckBox()
        {
            DefaultStyleKey = typeof(ExtendedCheckBox);
            Loaded += ExtendedCheckBox_Loaded;
            Cursor = Cursors.Hand;
            FontSize = 13;
            SetupEventHandlers();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sets up event handlers for checkbox interactions.
        /// </summary>
        private void SetupEventHandlers()
        {
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
        }

        /// <summary>
        /// Handles the Loaded event to create the checkbox template if not already applied.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ExtendedCheckBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_templateApplied)
            {
                CreateTemplate();
            }
        }

        /// <summary>
        /// Creates the visual template for the checkbox programmatically.
        /// </summary>
        private void CreateTemplate()
        {
            ApplySizePreset();

            var initialBackground = IsChecked == true ? CheckedBoxBrush : BoxBrush;
            var initialBorderBrush = IsChecked == true
                ? (CheckedBoxBorderBrush ?? CheckedBoxBrush)
                : BoxBorderBrush;

            _checkBoxBorder = new Border
            {
                Width = BoxWidth,
                Height = BoxHeight,
                CornerRadius = CornerRadius,
                Background = initialBackground,
                BorderBrush = initialBorderBrush,
                BorderThickness = BoxBorderThickness,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _checkMarkScaleTransform = new ScaleTransform { ScaleX = 0, ScaleY = 0 };
            _checkMarkTransformGroup = new TransformGroup();
            _checkMarkTransformGroup.Children.Add(_checkMarkScaleTransform);

            _checkMarkPath = new Path
            {
                Data = CheckMarkGeometry,
                Fill = CheckMarkBrush,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransform = _checkMarkTransformGroup,
                RenderTransformOrigin = new Point(0.5, 0.5),
                Opacity = 0
            };

            var checkMarkViewBox = new Viewbox
            {
                Width = CheckMarkSize,
                Height = CheckMarkSize,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = _checkMarkPath
            };

            var boxGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            boxGrid.Children.Add(checkMarkViewBox);
            _checkBoxBorder.Child = boxGrid;

            _textBlock = new TextBlock
            {
                Text = !string.IsNullOrEmpty(Text) ? Text : (Content?.ToString() ?? ""),
                Foreground = Foreground ?? new SolidColorBrush(Colors.Black),
                FontFamily = FontFamily,
                FontSize = FontSize,
                FontWeight = FontWeight,
                Margin = TextMargin,
                VerticalAlignment = VerticalAlignment.Center
            };

            _shadowEffect = new DropShadowEffect
            {
                BlurRadius = 6,
                Direction = 270,
                ShadowDepth = 2,
                Opacity = 0.25,
                Color = Colors.Black
            };

            _contentGrid = new Grid();
            SetupLayout();

            Content = _contentGrid;

            UpdateCheckMarkVisibility(false);
            UpdateShadow();
            _templateApplied = true;
        }



        /// <summary>
        /// Gets the default check mark geometry.
        /// </summary>
        /// <returns>The default check mark path geometry.</returns>
        private static Geometry GetDefaultCheckMarkGeometry()
        {
            // Material Design check mark
            return Geometry.Parse("M21,7L9,19L3.5,13.5L4.91,12.09L9,16.17L19.59,5.59L21,7Z");
        }

        /// <summary>
        /// Applies predefined size settings based on CheckBoxSize property.
        /// </summary>
        private void ApplySizePreset()
        {
            switch (CheckBoxSize)
            {
                case CheckBoxSize.Small:
                    SetValue(BoxWidthProperty, 16.0);
                    SetValue(BoxHeightProperty, 16.0);
                    SetValue(CheckMarkSizeProperty, 10.0);
                    break;
                case CheckBoxSize.Medium:
                    SetValue(BoxWidthProperty, 20.0);
                    SetValue(BoxHeightProperty, 20.0);
                    SetValue(CheckMarkSizeProperty, 12.0);
                    break;
                case CheckBoxSize.Large:
                    SetValue(BoxWidthProperty, 24.0);
                    SetValue(BoxHeightProperty, 24.0);
                    SetValue(CheckMarkSizeProperty, 16.0);
                    break;
                case CheckBoxSize.Custom:
                    // Keep current values
                    break;
            }
        }

        /// <summary>
        /// Sets up the layout of the checkbox and text based on TextPosition.
        /// </summary>
        private void SetupLayout()
        {
            if (_contentGrid == null) return;

            // Clear previous layout
            _contentGrid.Children.Clear();
            _contentGrid.RowDefinitions.Clear();
            _contentGrid.ColumnDefinitions.Clear();

            bool hasText = !string.IsNullOrEmpty(Text) || (Content != null && !string.IsNullOrEmpty(Content.ToString()));

            if (!hasText)
            {
                // Only checkbox
                _contentGrid.Children.Add(_checkBoxBorder);
            }
            else
            {
                // Checkbox with text
                switch (TextPosition)
                {
                    case TextPosition.Left:
                        SetupHorizontalLayout(textFirst: true);
                        break;
                    case TextPosition.Right:
                        SetupHorizontalLayout(textFirst: false);
                        break;
                    case TextPosition.Top:
                        SetupVerticalLayout(textFirst: true);
                        break;
                    case TextPosition.Bottom:
                        SetupVerticalLayout(textFirst: false);
                        break;
                }

                _contentGrid.Children.Add(_checkBoxBorder);
                _contentGrid.Children.Add(_textBlock);
            }
        }

        /// <summary>
        /// Sets up horizontal layout (Left/Right text positions).
        /// </summary>
        /// <param name="textFirst">Whether the text should be placed first.</param>
        private void SetupHorizontalLayout(bool textFirst)
        {
            _contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            if (textFirst)
            {
                Grid.SetColumn(_textBlock, 0);
                Grid.SetColumn(_checkBoxBorder, 1);
                _textBlock.Margin = new Thickness(TextMargin.Left, TextMargin.Top, TextMargin.Right, TextMargin.Bottom);
                _checkBoxBorder.Margin = new Thickness(8, 0, 0, 0);
            }
            else
            {
                Grid.SetColumn(_checkBoxBorder, 0);
                Grid.SetColumn(_textBlock, 1);
                _checkBoxBorder.Margin = new Thickness(0);
                _textBlock.Margin = TextMargin;
            }
        }

        /// <summary>
        /// Sets up vertical layout (Top/Bottom text positions).
        /// </summary>
        /// <param name="textFirst">Whether the text should be placed first.</param>
        private void SetupVerticalLayout(bool textFirst)
        {
            _contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            if (textFirst)
            {
                Grid.SetRow(_textBlock, 0);
                Grid.SetRow(_checkBoxBorder, 1);
                _textBlock.Margin = new Thickness(TextMargin.Left, TextMargin.Top, TextMargin.Right, TextMargin.Bottom);
                _checkBoxBorder.Margin = new Thickness(0, 8, 0, 0);
            }
            else
            {
                Grid.SetRow(_checkBoxBorder, 0);
                Grid.SetRow(_textBlock, 1);
                _checkBoxBorder.Margin = new Thickness(0);
                _textBlock.Margin = TextMargin;
            }

            _textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            _checkBoxBorder.HorizontalAlignment = HorizontalAlignment.Center;
        }

        /// <summary>
        /// Updates the check mark geometry based on current settings.
        /// </summary>
        private void UpdateCheckMarkGeometry()
        {
            if (_checkMarkPath == null) return;

            // Simple logic: Show uses CheckMarkGeometry, None hides it
            Geometry geometry = CheckMarkStyle == CheckMarkStyle.Show ? CheckMarkGeometry : null;

            _checkMarkPath.Data = geometry;
            _checkMarkPath.Visibility = geometry != null ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Updates the visibility and animation of the check mark.
        /// </summary>
        /// <param name="animate">Whether to animate the transition.</param>
        private void UpdateCheckMarkVisibility(bool animate)
        {
            if (_checkMarkPath == null) return;

            bool shouldShow = IsChecked == true && CheckMarkStyle == CheckMarkStyle.Show;

            if (animate && AnimationStyle != CheckBoxAnimationStyle.None)
            {
                AnimateCheckMark(shouldShow);
            }
            else
            {
                _checkMarkScaleTransform.ScaleX = shouldShow ? 1 : 0;
                _checkMarkScaleTransform.ScaleY = shouldShow ? 1 : 0;
                _checkMarkPath.Opacity = shouldShow ? 1 : 0;
            }
        }

        /// <summary>
        /// Animates the check mark appearance/disappearance.
        /// </summary>
        /// <param name="show">Whether to show or hide the check mark.</param>
        private void AnimateCheckMark(bool show)
        {
            // Stop any existing animation
            if (_animationTimer != null)
            {
                _animationTimer.Stop();
                _animationTimer = null;
            }

            if (AnimationStyle == CheckBoxAnimationStyle.None)
            {
                _checkMarkScaleTransform.ScaleX = show ? 1 : 0;
                _checkMarkScaleTransform.ScaleY = show ? 1 : 0;
                _checkMarkPath.Opacity = show ? 1 : 0;
                return;
            }

            // Setup animation parameters
            _animationStartOpacity = _checkMarkPath.Opacity;
            _animationTargetOpacity = show ? 1 : 0;
            _animationStartTime = DateTime.Now;

            // Set easing function
            switch (AnimationStyle)
            {
                case CheckBoxAnimationStyle.EaseInOut:
                    _currentEasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut };
                    break;
                case CheckBoxAnimationStyle.Bounce:
                    _currentEasingFunction = new BounceEase { EasingMode = EasingMode.EaseOut, Bounces = 2, Bounciness = 2 };
                    break;
                case CheckBoxAnimationStyle.Linear:
                default:
                    _currentEasingFunction = null;
                    break;
            }

            // Create and start timer
            _animationTimer = new DispatcherTimer();
            _animationTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60fps
            _animationTimer.Tick += OnCheckMarkAnimationTick;
            _animationTimer.Start();
        }

        /// <summary>
        /// Handles animation timer tick for check mark animation.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnCheckMarkAnimationTick(object sender, EventArgs e)
        {
            if (_animationTimer == null) return;

            var elapsed = DateTime.Now - _animationStartTime;
            var progress = Math.Min(1.0, elapsed.TotalMilliseconds / AnimationDuration.TotalMilliseconds);

            // Apply easing function if available
            var easedProgress = progress;
            if (_currentEasingFunction != null)
            {
                easedProgress = _currentEasingFunction.Ease(progress);
            }

            // Update check mark scale and opacity
            _checkMarkScaleTransform.ScaleX = easedProgress * (_animationTargetOpacity > 0 ? 1 : 0) +
                                             (1 - easedProgress) * (_animationTargetOpacity > 0 ? 0 : 1);
            _checkMarkScaleTransform.ScaleY = _checkMarkScaleTransform.ScaleX;

            var opacityDistance = _animationTargetOpacity - _animationStartOpacity;
            _checkMarkPath.Opacity = _animationStartOpacity + (opacityDistance * easedProgress);

            // Check if animation is complete
            if (progress >= 1.0)
            {
                _animationTimer.Stop();
                _animationTimer = null;

                // Ensure final values are set
                _checkMarkScaleTransform.ScaleX = _animationTargetOpacity > 0 ? 1 : 0;
                _checkMarkScaleTransform.ScaleY = _animationTargetOpacity > 0 ? 1 : 0;
                _checkMarkPath.Opacity = _animationTargetOpacity;
            }
        }

        /// <summary>
        /// Updates the shadow effect based on the HasShadow property.
        /// </summary>
        private void UpdateShadow()
        {
            if (_checkBoxBorder == null) return;
            _checkBoxBorder.Effect = HasShadow ? _shadowEffect : null;
        }

        /// <summary>
        /// Updates the visual state of the checkbox.
        /// </summary>
        private void UpdateVisualState()
        {
            if (_checkBoxBorder == null) return;

            // Update background color
            Brush backgroundBrush;
            Brush borderBrush;

            if (_isHovered)
            {
                backgroundBrush = IsChecked == true
                    ? (HoverCheckedBoxBrush ?? GetCalculatedHoverColor(CheckedBoxBrush))
                    : (HoverBoxBrush ?? GetCalculatedHoverColor(BoxBrush));
            }
            else
            {
                backgroundBrush = IsChecked == true ? CheckedBoxBrush : BoxBrush;
            }

            borderBrush = IsChecked == true
                ? (CheckedBoxBorderBrush ?? CheckedBoxBrush)
                : BoxBorderBrush;

            _checkBoxBorder.Background = backgroundBrush;
            _checkBoxBorder.BorderBrush = borderBrush;
        }

        /// <summary>
        /// Calculates a lighter color variant for hover states.
        /// </summary>
        /// <param name="originalBrush">The original brush.</param>
        /// <returns>A brush with a lighter color, or the original brush if not a solid color.</returns>
        private Brush GetCalculatedHoverColor(Brush originalBrush)
        {
            if (originalBrush is SolidColorBrush solidBrush)
            {
                var color = solidBrush.Color;
                return new SolidColorBrush(Color.FromArgb(
                    color.A,
                    (byte)Math.Min(255, color.R + 20),
                    (byte)Math.Min(255, color.G + 20),
                    (byte)Math.Min(255, color.B + 20)
                ));
            }
            return originalBrush;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the mouse enter event to trigger hover state.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The mouse event arguments.</param>
        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (IsEnabled)
            {
                _isHovered = true;
                UpdateVisualState();
            }
        }

        /// <summary>
        /// Handles the mouse leave event to return to normal state.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The mouse event arguments.</param>
        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (IsEnabled)
            {
                _isHovered = false;
                UpdateVisualState();
            }
        }

        #endregion

        #region Property Change Handlers

        /// <summary>
        /// Handles changes to the IsChecked property (including programmatic changes).
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedCheckBox checkBox)
            {
                if (checkBox._templateApplied)
                {
                    checkBox.UpdateCheckMarkVisibility(true);
                    checkBox.UpdateVisualState();
                }
                // If template is not applied yet, the CreateTemplate() method will handle the initial state
            }
        }

        /// <summary>
        /// Handles changes to the CheckBoxSize property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnCheckBoxSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedCheckBox checkBox)
            {
                checkBox.ApplySizePreset();
                if (checkBox._templateApplied)
                {
                    checkBox.UpdateLayout();
                }
            }
        }

        /// <summary>
        /// Handles changes to layout-affecting properties.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedCheckBox checkBox && checkBox._templateApplied)
            {
                checkBox.UpdateLayout();
            }
        }

        /// <summary>
        /// Updates layout when properties change.
        /// </summary>
        private void UpdateLayout()
        {
            if (_checkBoxBorder == null) return;

            _checkBoxBorder.Width = BoxWidth;
            _checkBoxBorder.Height = BoxHeight;
            _checkBoxBorder.CornerRadius = CornerRadius;

            if (_checkMarkPath != null)
            {
                if (_checkMarkPath.Parent is Viewbox vb)
                {
                    vb.Width = CheckMarkSize;
                    vb.Height = CheckMarkSize;
                }
                _checkMarkPath.Stretch = Stretch.Uniform;
                UpdateCheckMarkGeometry();
            }

            if (_textBlock != null)
            {
                _textBlock.Text = !string.IsNullOrEmpty(Text) ? Text : (Content?.ToString() ?? "");
            }

            SetupLayout();
            UpdateShadow();
        }


        /// <summary>
        /// Handles changes to the CornerRadius property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnCornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedCheckBox checkBox && checkBox._checkBoxBorder != null)
            {
                checkBox._checkBoxBorder.CornerRadius = (CornerRadius)e.NewValue;
            }
        }

        /// <summary>
        /// Handles changes to the CheckMarkStyle property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnCheckMarkStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedCheckBox checkBox)
            {
                checkBox.UpdateCheckMarkGeometry();
                checkBox.UpdateCheckMarkVisibility(false);
            }
        }

        /// <summary>
        /// Handles changes to the CheckMarkGeometry property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnCheckMarkGeometryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedCheckBox checkBox && checkBox._checkMarkPath != null)
            {
                // Directly update the Path.Data when geometry changes
                checkBox._checkMarkPath.Data = (Geometry)e.NewValue;
                checkBox.UpdateCheckMarkGeometry();
            }
        }

        /// <summary>
        /// Handles changes to the CheckMarkBrush property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnCheckMarkBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedCheckBox checkBox && checkBox._checkMarkPath != null)
            {
                checkBox._checkMarkPath.Fill = (Brush)e.NewValue;
            }
        }

        /// <summary>
        /// Handles changes to the CheckMarkSize property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnCheckMarkSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedCheckBox checkBox && checkBox._checkMarkPath != null)
            {
                var size = (double)e.NewValue;
                checkBox._checkMarkPath.Width = size;
                checkBox._checkMarkPath.Height = size;
            }
        }

        /// <summary>
        /// Handles changes to box brush properties.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnBoxBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedCheckBox checkBox)
            {
                checkBox.UpdateVisualState();
            }
        }

        /// <summary>
        /// Handles changes to box border brush properties.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnBoxBorderBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedCheckBox checkBox)
            {
                checkBox.UpdateVisualState();
            }
        }

        /// <summary>
        /// Handles changes to the BoxBorderThickness property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnBoxBorderThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedCheckBox checkBox && checkBox._checkBoxBorder != null)
            {
                checkBox._checkBoxBorder.BorderThickness = (Thickness)e.NewValue;
            }
        }

        /// <summary>
        /// Handles changes to the HasShadow property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnShadowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedCheckBox checkBox)
            {
                checkBox.UpdateShadow();
            }
        }

        /// <summary>
        /// Handles changes to the Text property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedCheckBox checkBox && checkBox._templateApplied)
            {
                checkBox.UpdateLayout();
            }
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Called when the content property changes.
        /// </summary>
        /// <param name="oldContent">The old content.</param>
        /// <param name="newContent">The new content.</param>
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            // Don't call base to prevent default content handling
            // We handle content ourselves in the template

            if (_templateApplied)
            {
                UpdateLayout();
            }
        }

        #endregion
    }
}