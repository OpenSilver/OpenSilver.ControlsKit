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

namespace OpenSilver.ControlsKit.Controls
{
    /// <summary>
    /// Specifies the size of the slider control.
    /// </summary>
    public enum SliderSize
    {
        /// <summary>
        /// Small slider size.
        /// </summary>
        Small,
        /// <summary>
        /// Medium slider size.
        /// </summary>
        Medium,
        /// <summary>
        /// Large slider size.
        /// </summary>
        Large,
        /// <summary>
        /// Custom slider size.
        /// </summary>
        Custom
    }

    /// <summary>
    /// A customizable slider control with smooth animations, hover effects, and flexible styling options.
    /// Provides advanced visual capabilities including shadows, custom colors for different states, and smooth transitions.
    /// </summary>
    public class ExtendedSlider : ContentControl
    {
        #region Events

        /// <summary>
        /// Occurs when the Value property changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<double> ValueChanged;

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the Value dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(ExtendedSlider),
                new PropertyMetadata(0.0, OnValueChanged, CoerceValue));

        /// <summary>
        /// Identifies the Minimum dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(ExtendedSlider),
                new PropertyMetadata(0.0, OnRangeChanged));

        /// <summary>
        /// Identifies the Maximum dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(ExtendedSlider),
                new PropertyMetadata(100.0, OnRangeChanged));

        /// <summary>
        /// Identifies the SliderSize dependency property.
        /// </summary>
        public static readonly DependencyProperty SliderSizeProperty =
            DependencyProperty.Register(nameof(SliderSize), typeof(SliderSize), typeof(ExtendedSlider),
                new PropertyMetadata(SliderSize.Medium, OnSliderSizeChanged));

        /// <summary>
        /// Identifies the TrackWidth dependency property.
        /// </summary>
        public static readonly DependencyProperty TrackWidthProperty =
            DependencyProperty.Register(nameof(TrackWidth), typeof(double), typeof(ExtendedSlider),
                new PropertyMetadata(200.0, OnLayoutChanged));

        /// <summary>
        /// Identifies the TrackHeight dependency property.
        /// </summary>
        public static readonly DependencyProperty TrackHeightProperty =
            DependencyProperty.Register(nameof(TrackHeight), typeof(double), typeof(ExtendedSlider),
                new PropertyMetadata(6.0, OnLayoutChanged));

        /// <summary>
        /// Identifies the ThumbSize dependency property.
        /// </summary>
        public static readonly DependencyProperty ThumbSizeProperty =
            DependencyProperty.Register(nameof(ThumbSize), typeof(double), typeof(ExtendedSlider),
                new PropertyMetadata(20.0, OnLayoutChanged));

        /// <summary>
        /// Identifies the CornerRadius dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(ExtendedSlider),
                new PropertyMetadata(new CornerRadius(3), OnStyleChanged));

        /// <summary>
        /// Identifies the TrackBrush dependency property.
        /// </summary>
        public static readonly DependencyProperty TrackBrushProperty =
            DependencyProperty.Register(nameof(TrackBrush), typeof(Brush), typeof(ExtendedSlider),
                new PropertyMetadata(new SolidColorBrush(Colors.LightGray), OnStyleChanged));

        /// <summary>
        /// Identifies the TrackBorderBrush dependency property.
        /// </summary>
        public static readonly DependencyProperty TrackBorderBrushProperty =
            DependencyProperty.Register(nameof(TrackBorderBrush), typeof(Brush), typeof(ExtendedSlider),
                new PropertyMetadata(null, OnStyleChanged));

        /// <summary>
        /// Identifies the TrackBorderThickness dependency property.
        /// </summary>
        public static readonly DependencyProperty TrackBorderThicknessProperty =
            DependencyProperty.Register(nameof(TrackBorderThickness), typeof(Thickness), typeof(ExtendedSlider),
                new PropertyMetadata(new Thickness(0), OnStyleChanged));

        /// <summary>
        /// Identifies the FillBrush dependency property.
        /// </summary>
        public static readonly DependencyProperty FillBrushProperty =
            DependencyProperty.Register(nameof(FillBrush), typeof(Brush), typeof(ExtendedSlider),
                new PropertyMetadata(new SolidColorBrush(Colors.DodgerBlue), OnStyleChanged));

        /// <summary>
        /// Identifies the ThumbBrush dependency property.
        /// </summary>
        public static readonly DependencyProperty ThumbBrushProperty =
            DependencyProperty.Register(nameof(ThumbBrush), typeof(Brush), typeof(ExtendedSlider),
                new PropertyMetadata(new SolidColorBrush(Colors.White), OnStyleChanged));

        /// <summary>
        /// Identifies the ThumbBorderBrush dependency property.
        /// </summary>
        public static readonly DependencyProperty ThumbBorderBrushProperty =
            DependencyProperty.Register(nameof(ThumbBorderBrush), typeof(Brush), typeof(ExtendedSlider),
                new PropertyMetadata(new SolidColorBrush(Colors.Gray), OnStyleChanged));

        /// <summary>
        /// Identifies the ThumbBorderThickness dependency property.
        /// </summary>
        public static readonly DependencyProperty ThumbBorderThicknessProperty =
            DependencyProperty.Register(nameof(ThumbBorderThickness), typeof(Thickness), typeof(ExtendedSlider),
                new PropertyMetadata(new Thickness(1), OnStyleChanged));

        /// <summary>
        /// Identifies the HasShadow dependency property.
        /// </summary>
        public static readonly DependencyProperty HasShadowProperty =
            DependencyProperty.Register(nameof(HasShadow), typeof(bool), typeof(ExtendedSlider),
                new PropertyMetadata(true, OnStyleChanged));

        /// <summary>
        /// Identifies the IsAnimated dependency property.
        /// </summary>
        public static readonly DependencyProperty IsAnimatedProperty =
            DependencyProperty.Register(nameof(IsAnimated), typeof(bool), typeof(ExtendedSlider),
                new PropertyMetadata(true));

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the current value of the slider.
        /// </summary>
        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum value of the slider.
        /// </summary>
        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum value of the slider.
        /// </summary>
        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        /// <summary>
        /// Gets or sets the predefined size of the slider.
        /// </summary>
        public SliderSize SliderSize
        {
            get => (SliderSize)GetValue(SliderSizeProperty);
            set => SetValue(SliderSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the width of the track.
        /// </summary>
        public double TrackWidth
        {
            get => (double)GetValue(TrackWidthProperty);
            set => SetValue(TrackWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the height of the track.
        /// </summary>
        public double TrackHeight
        {
            get => (double)GetValue(TrackHeightProperty);
            set => SetValue(TrackHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the size of the thumb.
        /// </summary>
        public double ThumbSize
        {
            get => (double)GetValue(ThumbSizeProperty);
            set => SetValue(ThumbSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the corner radius of the slider track.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for the track background.
        /// </summary>
        public Brush TrackBrush
        {
            get => (Brush)GetValue(TrackBrushProperty);
            set => SetValue(TrackBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the border brush of the track.
        /// </summary>
        public Brush TrackBorderBrush
        {
            get => (Brush)GetValue(TrackBorderBrushProperty);
            set => SetValue(TrackBorderBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the border thickness of the track.
        /// </summary>
        public Thickness TrackBorderThickness
        {
            get => (Thickness)GetValue(TrackBorderThicknessProperty);
            set => SetValue(TrackBorderThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for the filled portion of the track.
        /// </summary>
        public Brush FillBrush
        {
            get => (Brush)GetValue(FillBrushProperty);
            set => SetValue(FillBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for the thumb.
        /// </summary>
        public Brush ThumbBrush
        {
            get => (Brush)GetValue(ThumbBrushProperty);
            set => SetValue(ThumbBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the border brush of the thumb.
        /// </summary>
        public Brush ThumbBorderBrush
        {
            get => (Brush)GetValue(ThumbBorderBrushProperty);
            set => SetValue(ThumbBorderBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the border thickness of the thumb.
        /// </summary>
        public Thickness ThumbBorderThickness
        {
            get => (Thickness)GetValue(ThumbBorderThicknessProperty);
            set => SetValue(ThumbBorderThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the slider has a drop shadow effect.
        /// </summary>
        public bool HasShadow
        {
            get => (bool)GetValue(HasShadowProperty);
            set => SetValue(HasShadowProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether value changes are animated.
        /// </summary>
        public bool IsAnimated
        {
            get => (bool)GetValue(IsAnimatedProperty);
            set => SetValue(IsAnimatedProperty, value);
        }

        #endregion

        #region Private Fields

        private Canvas _rootCanvas;
        private Border _trackBorder;
        private Border _fillBorder;
        private Border _thumbBorder;
        private bool _isDragging;
        private bool _isHovered;
        private Point _lastMousePosition;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the ExtendedSlider class.
        /// </summary>
        public ExtendedSlider()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        #endregion

        #region Initialization

        private void InitializeComponent()
        {
            _rootCanvas = new Canvas
            {
                Background = new SolidColorBrush(Colors.Transparent)
            };

            _trackBorder = new Border
            {
                Background = TrackBrush,
                BorderBrush = TrackBorderBrush,
                BorderThickness = TrackBorderThickness,
                CornerRadius = CornerRadius
            };

            _fillBorder = new Border
            {
                Background = FillBrush,
                CornerRadius = CornerRadius
            };

            _thumbBorder = new Border
            {
                Background = ThumbBrush,
                BorderBrush = ThumbBorderBrush,
                BorderThickness = ThumbBorderThickness,
                CornerRadius = new CornerRadius(10),
                Cursor = Cursors.Hand
            };

            _rootCanvas.Children.Add(_trackBorder);
            _rootCanvas.Children.Add(_fillBorder);
            _rootCanvas.Children.Add(_thumbBorder);

            Content = _rootCanvas;

            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            _rootCanvas.MouseLeftButtonDown += OnCanvasMouseDown;
            _rootCanvas.MouseMove += OnCanvasMouseMove;
            _rootCanvas.MouseLeftButtonUp += OnCanvasMouseUp;

            _thumbBorder.MouseLeftButtonDown += OnThumbMouseDown;
            _thumbBorder.MouseEnter += OnThumbMouseEnter;
            _thumbBorder.MouseLeave += OnThumbMouseLeave;

            KeyDown += OnKeyDown;
            Focusable = true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ApplySizePreset();
            UpdateLayout();
            UpdateVisualElements();
            UpdateValueDisplay();
        }

        #endregion

        #region Layout and Visual Updates

        private void ApplySizePreset()
        {
            switch (SliderSize)
            {
                case SliderSize.Small:
                    TrackWidth = 150;
                    TrackHeight = 4;
                    ThumbSize = 16;
                    break;
                case SliderSize.Medium:
                    TrackWidth = 200;
                    TrackHeight = 6;
                    ThumbSize = 20;
                    break;
                case SliderSize.Large:
                    TrackWidth = 250;
                    TrackHeight = 8;
                    ThumbSize = 24;
                    break;
                case SliderSize.Custom:
                    break;
            }
        }

        private void UpdateLayout()
        {
            if (_rootCanvas == null) return;

            var canvasHeight = Math.Max(TrackHeight, ThumbSize) + 4;
            _rootCanvas.Width = TrackWidth;
            _rootCanvas.Height = canvasHeight;

            _trackBorder.Width = TrackWidth;
            _trackBorder.Height = TrackHeight;
            Canvas.SetLeft(_trackBorder, 0);
            Canvas.SetTop(_trackBorder, (canvasHeight - TrackHeight) / 2);

            UpdateValueDisplay();

            _thumbBorder.Width = ThumbSize;
            _thumbBorder.Height = ThumbSize;
            _thumbBorder.CornerRadius = new CornerRadius(ThumbSize / 2);

            UpdateShadowEffect();
        }

        private void UpdateValueDisplay()
        {
            if (_fillBorder == null || _thumbBorder == null) return;

            var range = Maximum - Minimum;
            if (range <= 0) return;

            var normalizedValue = Math.Max(0, Math.Min(1, (Value - Minimum) / range));
            var canvasHeight = _rootCanvas.Height;

            var fillWidth = normalizedValue * TrackWidth;
            _fillBorder.Width = fillWidth;
            _fillBorder.Height = TrackHeight;
            Canvas.SetLeft(_fillBorder, 0);
            Canvas.SetTop(_fillBorder, (canvasHeight - TrackHeight) / 2);

            var thumbPosition = normalizedValue * (TrackWidth - ThumbSize);

            if (IsAnimated && !_isDragging)
            {
                AnimateThumbToPosition(thumbPosition);
            }
            else
            {
                Canvas.SetLeft(_thumbBorder, thumbPosition);
            }

            Canvas.SetTop(_thumbBorder, (canvasHeight - ThumbSize) / 2);
        }

        private void AnimateThumbToPosition(double targetPosition)
        {
            var currentPosition = Canvas.GetLeft(_thumbBorder);

            if (double.IsNaN(currentPosition))
            {
                currentPosition = 0;
            }

            var animation = new DoubleAnimation
            {
                From = currentPosition,
                To = targetPosition,
                Duration = TimeSpan.FromMilliseconds(150),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            Storyboard.SetTarget(animation, _thumbBorder);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(Canvas.Left)"));
            storyboard.Begin();
        }

        private void UpdateVisualElements()
        {
            if (_trackBorder == null) return;

            _trackBorder.Background = TrackBrush;
            _trackBorder.BorderBrush = TrackBorderBrush;
            _trackBorder.BorderThickness = TrackBorderThickness;
            _trackBorder.CornerRadius = CornerRadius;

            _fillBorder.Background = FillBrush;
            _fillBorder.CornerRadius = CornerRadius;

            _thumbBorder.Background = ThumbBrush;
            _thumbBorder.BorderBrush = ThumbBorderBrush;
            _thumbBorder.BorderThickness = ThumbBorderThickness;

            ApplyHoverEffect();
        }

        private void ApplyHoverEffect()
        {
            if (_thumbBorder == null) return;

            if (_isHovered)
            {
                var scaleTransform = new ScaleTransform { ScaleX = 1.1, ScaleY = 1.1 };
                _thumbBorder.RenderTransform = scaleTransform;
                _thumbBorder.RenderTransformOrigin = new Point(0.5, 0.5);
            }
            else
            {
                _thumbBorder.RenderTransform = null;
            }
        }

        private void UpdateShadowEffect()
        {
            if (_thumbBorder == null) return;

            if (HasShadow)
            {
                _thumbBorder.Effect = new DropShadowEffect
                {
                    BlurRadius = 6,
                    Direction = 270,
                    ShadowDepth = 2,
                    Opacity = 0.25,
                    Color = Colors.Black
                };
            }
            else
            {
                _thumbBorder.Effect = null;
            }
        }

        #endregion

        #region Event Handlers

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsEnabled) return;

            var position = e.GetPosition(_rootCanvas);
            UpdateValueFromPosition(position.X);

            _isDragging = true;
            _rootCanvas.CaptureMouse();
            Focus();

            e.Handled = true;
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (!IsEnabled || !_isDragging) return;

            var position = e.GetPosition(_rootCanvas);
            UpdateValueFromPosition(position.X);

            e.Handled = true;
        }

        private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _rootCanvas.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void OnThumbMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsEnabled) return;

            _isDragging = true;
            _rootCanvas.CaptureMouse();
            Focus();

            e.Handled = true;
        }

        private void OnThumbMouseEnter(object sender, MouseEventArgs e)
        {
            _isHovered = true;
            ApplyHoverEffect();
        }

        private void OnThumbMouseLeave(object sender, MouseEventArgs e)
        {
            _isHovered = false;
            ApplyHoverEffect();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsEnabled) return;

            var step = (Maximum - Minimum) / 100;
            var newValue = Value;

            switch (e.Key)
            {
                case Key.Left:
                case Key.Down:
                    newValue = Math.Max(Minimum, Value - step);
                    break;
                case Key.Right:
                case Key.Up:
                    newValue = Math.Min(Maximum, Value + step);
                    break;
                case Key.Home:
                    newValue = Minimum;
                    break;
                case Key.End:
                    newValue = Maximum;
                    break;
                case Key.PageDown:
                    newValue = Math.Max(Minimum, Value - step * 10);
                    break;
                case Key.PageUp:
                    newValue = Math.Min(Maximum, Value + step * 10);
                    break;
                default:
                    return;
            }

            Value = newValue;
            e.Handled = true;
        }

        private void UpdateValueFromPosition(double mouseX)
        {
            var normalizedPosition = Math.Max(0, Math.Min(1, mouseX / TrackWidth));
            var newValue = Minimum + normalizedPosition * (Maximum - Minimum);
            Value = newValue;
        }

        #endregion

        #region Property Change Handlers

        private static object CoerceValue(DependencyObject d, object value)
        {
            var slider = (ExtendedSlider)d;
            var doubleValue = (double)value;
            return Math.Max(slider.Minimum, Math.Min(slider.Maximum, doubleValue));
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (ExtendedSlider)d;

            slider.ValueChanged?.Invoke(slider, new RoutedPropertyChangedEventArgs<double>(
                (double)e.OldValue, (double)e.NewValue));

            slider.UpdateValueDisplay();
        }

        private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (ExtendedSlider)d;

            slider.CoerceValue(ValueProperty);
            slider.UpdateValueDisplay();
        }

        private static void OnSliderSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (ExtendedSlider)d;
            slider.ApplySizePreset();
            slider.UpdateLayout();
        }

        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (ExtendedSlider)d;
            slider.UpdateLayout();
        }

        private static void OnStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (ExtendedSlider)d;
            slider.UpdateVisualElements();
        }

        #endregion
    }
}