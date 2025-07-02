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
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OpenSilver.ControlsKit.Controls
{
    /// <summary>
    /// Specifies the size of the switch control.
    /// </summary>
    public enum SwitchSize
    {
        /// <summary>
        /// Small switch size.
        /// </summary>
        Small,
        /// <summary>
        /// Medium switch size.
        /// </summary>
        Medium,
        /// <summary>
        /// Large switch size.
        /// </summary>
        Large,
        /// <summary>
        /// Custom switch size.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Specifies the animation style for the switch transition.
    /// </summary>
    public enum SwitchAnimationStyle
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
    /// A customizable switch control with smooth animations, hover effects, and flexible styling options.
    /// Provides advanced visual capabilities including shadows, custom colors for different states, and smooth transitions.
    /// </summary>
    public class ExtendedSwitch : ToggleButton
    {
        #region Private Fields
        private Border _rootBorder;
        private Border _trackBorder;
        private Border _thumbBorder; // Changed from Ellipse to Border
        private Grid _contentGrid;
        private Canvas _thumbCanvas;
        private DropShadowEffect _shadowEffect;
        private TranslateTransform _thumbTransform;
        private bool _isHovered;
        private bool _templateApplied;
        private double _thumbPosition;
        private DispatcherTimer _animationTimer;
        private double _animationStartPosition;
        private double _animationTargetPosition;
        private DateTime _animationStartTime;
        private IEasingFunction _currentEasingFunction;
        private Color _animationStartColor;
        private Color _animationTargetColor;
        private bool _animateToChecked;
        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="SwitchSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SwitchSizeProperty =
            DependencyProperty.Register(nameof(SwitchSize), typeof(SwitchSize), typeof(ExtendedSwitch),
                new PropertyMetadata(SwitchSize.Medium, OnSwitchSizeChanged));

        /// <summary>
        /// Identifies the <see cref="TrackWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TrackWidthProperty =
            DependencyProperty.Register(nameof(TrackWidth), typeof(double), typeof(ExtendedSwitch),
                new PropertyMetadata(50.0, OnLayoutPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="TrackHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TrackHeightProperty =
            DependencyProperty.Register(nameof(TrackHeight), typeof(double), typeof(ExtendedSwitch),
                new PropertyMetadata(25.0, OnLayoutPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ThumbSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ThumbSizeProperty =
            DependencyProperty.Register(nameof(ThumbSize), typeof(double), typeof(ExtendedSwitch),
                new PropertyMetadata(21.0, OnLayoutPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius?), typeof(ExtendedSwitch),
                new PropertyMetadata(null, OnCornerRadiusChanged));

        /// <summary>
        /// Identifies the <see cref="ThumbCornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ThumbCornerRadiusProperty =
            DependencyProperty.Register(nameof(ThumbCornerRadius), typeof(CornerRadius?), typeof(ExtendedSwitch),
                new PropertyMetadata(null, OnThumbCornerRadiusChanged));

        /// <summary>
        /// Identifies the <see cref="TrackBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TrackBrushProperty =
            DependencyProperty.Register(nameof(TrackBrush), typeof(Brush), typeof(ExtendedSwitch),
                new PropertyMetadata(new SolidColorBrush(Colors.LightGray), OnTrackBrushChanged));

        /// <summary>
        /// Identifies the <see cref="CheckedTrackBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CheckedTrackBrushProperty =
            DependencyProperty.Register(nameof(CheckedTrackBrush), typeof(Brush), typeof(ExtendedSwitch),
                new PropertyMetadata(new SolidColorBrush(Colors.DodgerBlue), OnTrackBrushChanged));

        /// <summary>
        /// Identifies the <see cref="ThumbBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ThumbBrushProperty =
            DependencyProperty.Register(nameof(ThumbBrush), typeof(Brush), typeof(ExtendedSwitch),
                new PropertyMetadata(new SolidColorBrush(Colors.White), OnThumbBrushChanged));

        /// <summary>
        /// Identifies the <see cref="HoverTrackBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HoverTrackBrushProperty =
            DependencyProperty.Register(nameof(HoverTrackBrush), typeof(Brush), typeof(ExtendedSwitch),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="HoverCheckedTrackBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HoverCheckedTrackBrushProperty =
            DependencyProperty.Register(nameof(HoverCheckedTrackBrush), typeof(Brush), typeof(ExtendedSwitch),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="HoverThumbBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HoverThumbBrushProperty =
            DependencyProperty.Register(nameof(HoverThumbBrush), typeof(Brush), typeof(ExtendedSwitch),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="HasShadow"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HasShadowProperty =
            DependencyProperty.Register(nameof(HasShadow), typeof(bool), typeof(ExtendedSwitch),
                new PropertyMetadata(false, OnShadowChanged));

        /// <summary>
        /// Identifies the <see cref="AnimationDuration"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AnimationDurationProperty =
            DependencyProperty.Register(nameof(AnimationDuration), typeof(TimeSpan), typeof(ExtendedSwitch),
                new PropertyMetadata(TimeSpan.FromMilliseconds(200)));

        /// <summary>
        /// Identifies the <see cref="AnimationStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AnimationStyleProperty =
            DependencyProperty.Register(nameof(AnimationStyle), typeof(SwitchAnimationStyle), typeof(ExtendedSwitch),
                new PropertyMetadata(SwitchAnimationStyle.EaseInOut));

        /// <summary>
        /// Identifies the <see cref="ThumbMargin"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ThumbMarginProperty =
            DependencyProperty.Register(nameof(ThumbMargin), typeof(double), typeof(ExtendedSwitch),
                new PropertyMetadata(2.0, OnLayoutPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="TrackBorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TrackBorderBrushProperty =
            DependencyProperty.Register(nameof(TrackBorderBrush), typeof(Brush), typeof(ExtendedSwitch),
                new PropertyMetadata(null, OnTrackBorderBrushChanged));

        /// <summary>
        /// Identifies the <see cref="TrackBorderThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TrackBorderThicknessProperty =
            DependencyProperty.Register(nameof(TrackBorderThickness), typeof(Thickness), typeof(ExtendedSwitch),
                new PropertyMetadata(new Thickness(0), OnTrackBorderThicknessChanged));

        /// <summary>
        /// Identifies the <see cref="ThumbBorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ThumbBorderBrushProperty =
            DependencyProperty.Register(nameof(ThumbBorderBrush), typeof(Brush), typeof(ExtendedSwitch),
                new PropertyMetadata(null, OnThumbBorderBrushChanged));

        /// <summary>
        /// Identifies the <see cref="ThumbBorderThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ThumbBorderThicknessProperty =
            DependencyProperty.Register(nameof(ThumbBorderThickness), typeof(Thickness), typeof(ExtendedSwitch),
                new PropertyMetadata(new Thickness(0), OnThumbBorderThicknessChanged));

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the predefined size of the switch.
        /// </summary>
        /// <value>The switch size. Default is Medium.</value>
        public SwitchSize SwitchSize
        {
            get => (SwitchSize)GetValue(SwitchSizeProperty);
            set => SetValue(SwitchSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the width of the track.
        /// </summary>
        /// <value>The track width in device-independent pixels. Default is 50.</value>
        public double TrackWidth
        {
            get => (double)GetValue(TrackWidthProperty);
            set => SetValue(TrackWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the height of the track.
        /// </summary>
        /// <value>The track height in device-independent pixels. Default is 25.</value>
        public double TrackHeight
        {
            get => (double)GetValue(TrackHeightProperty);
            set => SetValue(TrackHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the size of the thumb.
        /// </summary>
        /// <value>The thumb size in device-independent pixels. Default is 21.</value>
        public double ThumbSize
        {
            get => (double)GetValue(ThumbSizeProperty);
            set => SetValue(ThumbSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the corner radius of the switch track.
        /// If null, automatically calculates based on track height for circular appearance.
        /// </summary>
        /// <value>The corner radius. If null, defaults to TrackHeight/2 for circular appearance.</value>
        public CornerRadius? CornerRadius
        {
            get => (CornerRadius?)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Gets or sets the corner radius of the thumb.
        /// If null, automatically calculates based on thumb size and track corner radius.
        /// </summary>
        /// <value>The thumb corner radius. If null, automatically calculated for best appearance.</value>
        public CornerRadius? ThumbCornerRadius
        {
            get => (CornerRadius?)GetValue(ThumbCornerRadiusProperty);
            set => SetValue(ThumbCornerRadiusProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for the track background when unchecked.
        /// </summary>
        /// <value>The track brush. Default is light gray.</value>
        public Brush TrackBrush
        {
            get => (Brush)GetValue(TrackBrushProperty);
            set => SetValue(TrackBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for the track background when checked.
        /// </summary>
        /// <value>The checked track brush. Default is dodger blue.</value>
        public Brush CheckedTrackBrush
        {
            get => (Brush)GetValue(CheckedTrackBrushProperty);
            set => SetValue(CheckedTrackBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for the thumb.
        /// </summary>
        /// <value>The thumb brush. Default is white.</value>
        public Brush ThumbBrush
        {
            get => (Brush)GetValue(ThumbBrushProperty);
            set => SetValue(ThumbBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for the track when hovered and unchecked.
        /// </summary>
        /// <value>The hover track brush. If null, a calculated lighter color is used.</value>
        public Brush HoverTrackBrush
        {
            get => (Brush)GetValue(HoverTrackBrushProperty);
            set => SetValue(HoverTrackBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for the track when hovered and checked.
        /// </summary>
        /// <value>The hover checked track brush. If null, a calculated lighter color is used.</value>
        public Brush HoverCheckedTrackBrush
        {
            get => (Brush)GetValue(HoverCheckedTrackBrushProperty);
            set => SetValue(HoverCheckedTrackBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for the thumb when hovered.
        /// </summary>
        /// <value>The hover thumb brush. If null, a calculated lighter color is used.</value>
        public Brush HoverThumbBrush
        {
            get => (Brush)GetValue(HoverThumbBrushProperty);
            set => SetValue(HoverThumbBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the switch track has a drop shadow effect.
        /// </summary>
        /// <value>true if the switch track has a shadow; otherwise, false. Default is false.</value>
        public bool HasShadow
        {
            get => (bool)GetValue(HasShadowProperty);
            set => SetValue(HasShadowProperty, value);
        }

        /// <summary>
        /// Gets or sets the duration of the thumb animation.
        /// </summary>
        /// <value>The animation duration. Default is 200 milliseconds.</value>
        public TimeSpan AnimationDuration
        {
            get => (TimeSpan)GetValue(AnimationDurationProperty);
            set => SetValue(AnimationDurationProperty, value);
        }

        /// <summary>
        /// Gets or sets the animation style for the thumb transition.
        /// </summary>
        /// <value>The animation style. Default is EaseInOut.</value>
        public SwitchAnimationStyle AnimationStyle
        {
            get => (SwitchAnimationStyle)GetValue(AnimationStyleProperty);
            set => SetValue(AnimationStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the margin around the thumb.
        /// </summary>
        /// <value>The thumb margin in device-independent pixels. Default is 2.</value>
        public double ThumbMargin
        {
            get => (double)GetValue(ThumbMarginProperty);
            set => SetValue(ThumbMarginProperty, value);
        }

        /// <summary>
        /// Gets or sets the border brush of the track.
        /// </summary>
        /// <value>The track border brush.</value>
        public Brush TrackBorderBrush
        {
            get => (Brush)GetValue(TrackBorderBrushProperty);
            set => SetValue(TrackBorderBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the border thickness of the track.
        /// </summary>
        /// <value>The track border thickness.</value>
        public Thickness TrackBorderThickness
        {
            get => (Thickness)GetValue(TrackBorderThicknessProperty);
            set => SetValue(TrackBorderThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the border brush of the thumb.
        /// </summary>
        /// <value>The thumb border brush.</value>
        public Brush ThumbBorderBrush
        {
            get => (Brush)GetValue(ThumbBorderBrushProperty);
            set => SetValue(ThumbBorderBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the border thickness of the thumb.
        /// </summary>
        /// <value>The thumb border thickness.</value>
        public Thickness ThumbBorderThickness
        {
            get => (Thickness)GetValue(ThumbBorderThicknessProperty);
            set => SetValue(ThumbBorderThicknessProperty, value);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Static constructor to override metadata for IsChecked property.
        /// </summary>
        static ExtendedSwitch()
        {
            // Override IsChecked property to handle programmatic changes
            IsCheckedProperty.OverrideMetadata(typeof(ExtendedSwitch),
                new FrameworkPropertyMetadata(OnIsCheckedChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedSwitch"/> class.
        /// </summary>
        public ExtendedSwitch()
        {
            DefaultStyleKey = typeof(ExtendedSwitch);
            SetupEventHandlers();
            Loaded += ExtendedSwitch_Loaded;
            Cursor = Cursors.Hand;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles the Loaded event to create the switch template if not already applied.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ExtendedSwitch_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_templateApplied)
            {
                CreateTemplate();
            }
        }

        /// <summary>
        /// Creates the visual template for the switch programmatically.
        /// </summary>
        private void CreateTemplate()
        {
            ApplySizePreset();

            _rootBorder = new Border
            {
                Width = TrackWidth,
                Height = TrackHeight,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _trackBorder = new Border
            {
                Width = TrackWidth,
                Height = TrackHeight,
                CornerRadius = GetEffectiveTrackCornerRadius(),
                Background = TrackBrush,
                BorderBrush = TrackBorderBrush,
                BorderThickness = TrackBorderThickness
            };

            _thumbCanvas = new Canvas
            {
                Width = TrackWidth,
                Height = TrackHeight
            };

            // Create thumb as Border instead of Ellipse for better corner radius control
            _thumbBorder = new Border
            {
                Width = ThumbSize,
                Height = ThumbSize,
                Background = ThumbBrush,
                BorderBrush = ThumbBorderBrush,
                BorderThickness = ThumbBorderThickness,
                CornerRadius = GetEffectiveThumbCornerRadius()
            };

            _thumbTransform = new TranslateTransform();
            _thumbBorder.RenderTransform = _thumbTransform;

            _shadowEffect = new DropShadowEffect
            {
                BlurRadius = 6,
                Direction = 270,
                ShadowDepth = 2,
                Opacity = 0.25,
                Color = Colors.Black
            };

            _contentGrid = new Grid();
            _contentGrid.Children.Add(_trackBorder);
            _contentGrid.Children.Add(_thumbCanvas);
            _thumbCanvas.Children.Add(_thumbBorder);

            _rootBorder.Child = _contentGrid;
            Content = _rootBorder;

            UpdateLayout();

            // Set initial thumb position based on current IsChecked state
            double initialX = IsChecked == true
                ? TrackWidth - ThumbSize - ThumbMargin
                : ThumbMargin;
            _thumbTransform.X = initialX;
            _thumbPosition = initialX;

            _templateApplied = true;
        }

        /// <summary>
        /// Gets the effective corner radius for the track.
        /// If CornerRadius is null, calculates based on track height for circular appearance.
        /// </summary>
        /// <returns>The effective track corner radius.</returns>
        private CornerRadius GetEffectiveTrackCornerRadius()
        {
            if (CornerRadius.HasValue)
            {
                return CornerRadius.Value;
            }

            // Default to circular (half of track height)
            var radius = TrackHeight / 2;
            return new CornerRadius(radius);
        }

        /// <summary>
        /// Gets the effective corner radius for the thumb.
        /// If ThumbCornerRadius is null, calculates based on thumb size and track corner radius.
        /// </summary>
        /// <returns>The effective thumb corner radius.</returns>
        private CornerRadius GetEffectiveThumbCornerRadius()
        {
            if (ThumbCornerRadius.HasValue)
            {
                return ThumbCornerRadius.Value;
            }

            // Auto-calculate based on track corner radius
            var trackCornerRadius = GetEffectiveTrackCornerRadius();
            var trackRadius = trackCornerRadius.TopLeft;

            // Calculate ratio of track radius to track height
            var trackRadiusRatio = trackRadius / (TrackHeight / 2);

            // Apply same ratio to thumb
            var thumbRadius = (ThumbSize / 2) * trackRadiusRatio;

            // Ensure thumb radius doesn't exceed half of thumb size
            thumbRadius = Math.Min(thumbRadius, ThumbSize / 2);

            // For very small track corner radius, ensure minimum thumb radius for good appearance
            if (trackRadius <= 3)
            {
                thumbRadius = Math.Max(thumbRadius, 2);
            }

            return new CornerRadius(thumbRadius);
        }

        /// <summary>
        /// Applies predefined size settings based on SwitchSize property.
        /// </summary>
        private void ApplySizePreset()
        {
            switch (SwitchSize)
            {
                case SwitchSize.Small:
                    SetValue(TrackWidthProperty, 36.0);
                    SetValue(TrackHeightProperty, 20.0);
                    SetValue(ThumbSizeProperty, 16.0);
                    // Don't set CornerRadius - let it auto-calculate
                    break;
                case SwitchSize.Medium:
                    SetValue(TrackWidthProperty, 50.0);
                    SetValue(TrackHeightProperty, 25.0);
                    SetValue(ThumbSizeProperty, 21.0);
                    // Don't set CornerRadius - let it auto-calculate
                    break;
                case SwitchSize.Large:
                    SetValue(TrackWidthProperty, 64.0);
                    SetValue(TrackHeightProperty, 32.0);
                    SetValue(ThumbSizeProperty, 28.0);
                    // Don't set CornerRadius - let it auto-calculate
                    break;
                case SwitchSize.Custom:
                    // Keep current values
                    break;
            }
        }

        /// <summary>
        /// Updates the layout of the switch components.
        /// </summary>
        private void UpdateLayout()
        {
            if (_rootBorder == null) return;

            _rootBorder.Width = TrackWidth;
            _rootBorder.Height = TrackHeight;

            if (_trackBorder != null)
            {
                _trackBorder.Width = TrackWidth;
                _trackBorder.Height = TrackHeight;
                _trackBorder.CornerRadius = GetEffectiveTrackCornerRadius();
            }

            if (_thumbCanvas != null)
            {
                _thumbCanvas.Width = TrackWidth;
                _thumbCanvas.Height = TrackHeight;
            }

            if (_thumbBorder != null)
            {
                _thumbBorder.Width = ThumbSize;
                _thumbBorder.Height = ThumbSize;
                _thumbBorder.CornerRadius = GetEffectiveThumbCornerRadius();

                // Position thumb vertically centered
                Canvas.SetTop(_thumbBorder, (TrackHeight - ThumbSize) / 2);
            }

            UpdateShadow();
        }

        /// <summary>
        /// Updates the position of the thumb based on the checked state.
        /// </summary>
        /// <param name="animate">Whether to animate the transition.</param>
        private void UpdateThumbPosition(bool animate)
        {
            if (_thumbBorder == null || _thumbTransform == null) return;

            double targetX = IsChecked == true
                ? TrackWidth - ThumbSize - ThumbMargin
                : ThumbMargin;

            if (animate && AnimationStyle != SwitchAnimationStyle.None)
            {
                AnimateThumbPosition(targetX);
            }
            else
            {
                _thumbTransform.X = targetX;
                _thumbPosition = targetX;
            }
        }

        /// <summary>
        /// Animates the thumb to the target position using manual timer-based animation.
        /// </summary>
        /// <param name="targetX">The target X position.</param>
        private void AnimateThumbPosition(double targetX)
        {
            // Stop any existing animation
            if (_animationTimer != null)
            {
                _animationTimer.Stop();
                _animationTimer = null;
            }

            // Get current position
            double currentX = _thumbTransform.X;
            _thumbPosition = currentX;

            // Skip animation if already at target or very close
            if (Math.Abs(currentX - targetX) < 1.0)
            {
                _thumbTransform.X = targetX;
                _thumbPosition = targetX;
                UpdateTrackColorInstant();
                return;
            }

            // Skip animation if AnimationStyle is None
            if (AnimationStyle == SwitchAnimationStyle.None)
            {
                _thumbTransform.X = targetX;
                _thumbPosition = targetX;
                UpdateTrackColorInstant();
                return;
            }

            // Setup position animation parameters
            _animationStartPosition = currentX;
            _animationTargetPosition = targetX;
            _animationStartTime = DateTime.Now;
            _animateToChecked = IsChecked == true;

            // Setup color animation parameters
            _animationStartColor = GetCurrentTrackColor();
            _animationTargetColor = GetTargetTrackColor();

            // Set easing function
            switch (AnimationStyle)
            {
                case SwitchAnimationStyle.EaseInOut:
                    _currentEasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut };
                    break;
                case SwitchAnimationStyle.Bounce:
                    _currentEasingFunction = new BounceEase { EasingMode = EasingMode.EaseOut, Bounces = 2, Bounciness = 2 };
                    break;
                case SwitchAnimationStyle.Linear:
                default:
                    _currentEasingFunction = null;
                    break;
            }

            // Create and start timer
            _animationTimer = new DispatcherTimer();
            _animationTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60fps
            _animationTimer.Tick += OnAnimationTick;
            _animationTimer.Start();
        }

        /// <summary>
        /// Handles animation timer tick for manual thumb position and color animation.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnAnimationTick(object sender, EventArgs e)
        {
            if (_animationTimer == null) return;

            var elapsed = DateTime.Now - _animationStartTime;

            // Use different durations for position vs color-only animations
            var isPositionAnimation = Math.Abs(_animationTargetPosition - _animationStartPosition) > 1.0;
            var duration = isPositionAnimation ? AnimationDuration : TimeSpan.FromMilliseconds(150);

            var progress = Math.Min(1.0, elapsed.TotalMilliseconds / duration.TotalMilliseconds);

            // Apply easing function if available
            var easedProgress = progress;
            if (_currentEasingFunction != null)
            {
                easedProgress = _currentEasingFunction.Ease(progress);
            }

            // Update thumb position (only if it's a position animation)
            if (isPositionAnimation)
            {
                var distance = _animationTargetPosition - _animationStartPosition;
                var currentPosition = _animationStartPosition + (distance * easedProgress);
                _thumbTransform.X = currentPosition;
                _thumbPosition = currentPosition;
            }

            // Always update track color
            var currentColor = InterpolateColor(_animationStartColor, _animationTargetColor, easedProgress);
            _trackBorder.Background = new SolidColorBrush(currentColor);

            // Check if animation is complete
            if (progress >= 1.0)
            {
                _animationTimer.Stop();
                _animationTimer = null;

                // Ensure final values are set
                if (isPositionAnimation)
                {
                    _thumbTransform.X = _animationTargetPosition;
                    _thumbPosition = _animationTargetPosition;
                }
                _trackBorder.Background = new SolidColorBrush(_animationTargetColor);
            }
        }

        /// <summary>
        /// Gets the current track color based on state and hover.
        /// </summary>
        /// <returns>The current track color.</returns>
        private Color GetCurrentTrackColor()
        {
            var currentBrush = _trackBorder?.Background as SolidColorBrush;
            if (currentBrush != null)
            {
                return currentBrush.Color;
            }

            // Fallback to default colors
            if (_isHovered)
            {
                var hoverBrush = IsChecked == true
                    ? (HoverCheckedTrackBrush ?? GetCalculatedHoverColor(CheckedTrackBrush))
                    : (HoverTrackBrush ?? GetCalculatedHoverColor(TrackBrush));
                return ((SolidColorBrush)hoverBrush).Color;
            }
            else
            {
                var normalBrush = IsChecked == true ? CheckedTrackBrush : TrackBrush;
                return ((SolidColorBrush)normalBrush).Color;
            }
        }

        /// <summary>
        /// Gets the target track color based on the new state and hover.
        /// </summary>
        /// <returns>The target track color.</returns>
        private Color GetTargetTrackColor()
        {
            Brush targetBrush;
            if (_isHovered)
            {
                // For position animations, use the final checked state
                // For hover animations, use the current checked state
                bool targetChecked = _animationTargetPosition == _animationStartPosition ?
                    (IsChecked == true) : _animateToChecked;

                targetBrush = targetChecked
                    ? (HoverCheckedTrackBrush ?? GetCalculatedHoverColor(CheckedTrackBrush))
                    : (HoverTrackBrush ?? GetCalculatedHoverColor(TrackBrush));
            }
            else
            {
                // For position animations, use the final checked state
                // For hover animations, use the current checked state
                bool targetChecked = _animationTargetPosition == _animationStartPosition ?
                    (IsChecked == true) : _animateToChecked;

                targetBrush = targetChecked ? CheckedTrackBrush : TrackBrush;
            }

            return ((SolidColorBrush)targetBrush).Color;
        }

        /// <summary>
        /// Updates track color instantly without animation.
        /// </summary>
        private void UpdateTrackColorInstant()
        {
            if (_trackBorder == null) return;

            Brush trackBrush;
            if (_isHovered)
            {
                trackBrush = IsChecked == true
                    ? (HoverCheckedTrackBrush ?? GetCalculatedHoverColor(CheckedTrackBrush))
                    : (HoverTrackBrush ?? GetCalculatedHoverColor(TrackBrush));
            }
            else
            {
                trackBrush = IsChecked == true ? CheckedTrackBrush : TrackBrush;
            }

            _trackBorder.Background = trackBrush;
        }

        /// <summary>
        /// Interpolates between two colors based on progress.
        /// </summary>
        /// <param name="startColor">The starting color.</param>
        /// <param name="endColor">The ending color.</param>
        /// <param name="progress">The interpolation progress (0.0 to 1.0).</param>
        /// <returns>The interpolated color.</returns>
        private Color InterpolateColor(Color startColor, Color endColor, double progress)
        {
            var r = (byte)(startColor.R + (endColor.R - startColor.R) * progress);
            var g = (byte)(startColor.G + (endColor.G - startColor.G) * progress);
            var b = (byte)(startColor.B + (endColor.B - startColor.B) * progress);
            var a = (byte)(startColor.A + (endColor.A - startColor.A) * progress);

            return Color.FromArgb(a, r, g, b);
        }

        /// <summary>
        /// Updates the shadow effect based on the HasShadow property.
        /// </summary>
        private void UpdateShadow()
        {
            if (_trackBorder == null) return;
            _trackBorder.Effect = HasShadow ? _shadowEffect : null;
        }

        /// <summary>
        /// Updates the visual state of the switch.
        /// </summary>
        private void UpdateVisualState()
        {
            if (_trackBorder == null) return;

            // Only update colors instantly if no animation is running
            // (Animation will handle color changes during thumb movement)
            if (_animationTimer == null)
            {
                UpdateTrackColorInstant();
            }

            // Always update thumb color for hover effects
            if (_thumbBorder != null)
            {
                _thumbBorder.Background = _isHovered && HoverThumbBrush != null
                    ? HoverThumbBrush
                    : ThumbBrush;
            }
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

        /// <summary>
        /// Sets up event handlers for switch interactions.
        /// </summary>
        private void SetupEventHandlers()
        {
            Checked += OnChecked;
            Unchecked += OnUnchecked;

            AddHandler(UIElement.MouseEnterEvent, new MouseEventHandler(OnMouseEnter), true);
            AddHandler(UIElement.MouseLeaveEvent, new MouseEventHandler(OnMouseLeave), true);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the checked event to update thumb position.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The routed event arguments.</param>
        private void OnChecked(object sender, RoutedEventArgs e)
        {
            UpdateThumbPosition(true);
            UpdateVisualState();
        }

        /// <summary>
        /// Handles the unchecked event to update thumb position.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The routed event arguments.</param>
        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            UpdateThumbPosition(true);
            UpdateVisualState();
        }

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
                // Animate to hover colors if no position animation is running
                if (_animationTimer == null)
                {
                    AnimateColorOnly();
                }
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
                // Animate to normal colors if no position animation is running
                if (_animationTimer == null)
                {
                    AnimateColorOnly();
                }
                UpdateVisualState();
            }
        }

        /// <summary>
        /// Animates only the color without position change (for hover effects).
        /// </summary>
        private void AnimateColorOnly()
        {
            // Skip color animation if AnimationStyle is None
            if (AnimationStyle == SwitchAnimationStyle.None)
            {
                UpdateTrackColorInstant();
                return;
            }

            // Setup color animation parameters
            _animationStartColor = GetCurrentTrackColor();
            _animationTargetColor = GetTargetTrackColor();
            _animationStartTime = DateTime.Now;

            // Don't change thumb position during color-only animation
            _animationStartPosition = _thumbPosition;
            _animationTargetPosition = _thumbPosition;

            // Set easing function (use faster animation for hover)
            _currentEasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut };

            // Create and start timer with shorter duration for hover
            _animationTimer = new DispatcherTimer();
            _animationTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60fps
            _animationTimer.Tick += OnAnimationTick; // Use same tick handler
            _animationTimer.Start();
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
            if (d is ExtendedSwitch switchControl && switchControl._templateApplied)
            {
                switchControl.UpdateThumbPosition(true);
                switchControl.UpdateVisualState();
            }
        }

        /// <summary>
        /// Handles changes to the SwitchSize property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnSwitchSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedSwitch switchControl)
            {
                switchControl.ApplySizePreset();
                switchControl.UpdateLayout();
            }
        }

        /// <summary>
        /// Handles changes to layout-affecting properties.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedSwitch switchControl)
            {
                switchControl.UpdateLayout();
                switchControl.UpdateThumbPosition(false);
            }
        }

        /// <summary>
        /// Handles changes to the CornerRadius property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnCornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedSwitch switchControl && switchControl._trackBorder != null)
            {
                switchControl._trackBorder.CornerRadius = switchControl.GetEffectiveTrackCornerRadius();
                // Also update thumb corner radius when track corner radius changes (if thumb corner radius is auto)
                if (switchControl._thumbBorder != null && !switchControl.ThumbCornerRadius.HasValue)
                {
                    switchControl._thumbBorder.CornerRadius = switchControl.GetEffectiveThumbCornerRadius();
                }
            }
        }

        /// <summary>
        /// Handles changes to the ThumbCornerRadius property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnThumbCornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedSwitch switchControl && switchControl._thumbBorder != null)
            {
                switchControl._thumbBorder.CornerRadius = switchControl.GetEffectiveThumbCornerRadius();
            }
        }

        /// <summary>
        /// Handles changes to track brush properties.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnTrackBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedSwitch switchControl)
            {
                switchControl.UpdateVisualState();
            }
        }

        /// <summary>
        /// Handles changes to the ThumbBrush property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnThumbBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedSwitch switchControl && switchControl._thumbBorder != null)
            {
                switchControl._thumbBorder.Background = (Brush)e.NewValue;
            }
        }

        /// <summary>
        /// Handles changes to the HasShadow property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnShadowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedSwitch switchControl)
            {
                switchControl.UpdateShadow();
            }
        }

        /// <summary>
        /// Handles changes to the TrackBorderBrush property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnTrackBorderBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedSwitch switchControl && switchControl._trackBorder != null)
            {
                switchControl._trackBorder.BorderBrush = (Brush)e.NewValue;
            }
        }

        /// <summary>
        /// Handles changes to the TrackBorderThickness property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnTrackBorderThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedSwitch switchControl && switchControl._trackBorder != null)
            {
                switchControl._trackBorder.BorderThickness = (Thickness)e.NewValue;
            }
        }

        /// <summary>
        /// Handles changes to the ThumbBorderBrush property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnThumbBorderBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedSwitch switchControl && switchControl._thumbBorder != null)
            {
                switchControl._thumbBorder.BorderBrush = (Brush)e.NewValue;
            }
        }

        /// <summary>
        /// Handles changes to the ThumbBorderThickness property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnThumbBorderThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedSwitch switchControl && switchControl._thumbBorder != null)
            {
                switchControl._thumbBorder.BorderThickness = (Thickness)e.NewValue;
            }
        }

        #endregion
    }
}