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
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace OpenSilver.ControlsKit.Controls
{
    /// <summary>
    /// Specifies the position of the icon relative to the text content.
    /// </summary>
    public enum IconPosition
    {
        /// <summary>
        /// Icon is positioned to the left of the text.
        /// </summary>
        Left,
        /// <summary>
        /// Icon is positioned above the text.
        /// </summary>
        Top,
        /// <summary>
        /// Icon is positioned to the right of the text.
        /// </summary>
        Right,
        /// <summary>
        /// Icon is positioned below the text.
        /// </summary>
        Bottom
    }

    /// <summary>
    /// Specifies the alignment of content within its container.
    /// </summary>
    public enum ContentAlignment
    {
        /// <summary>
        /// Content is aligned to the left.
        /// </summary>
        Left,
        /// <summary>
        /// Content is aligned to the top.
        /// </summary>
        Top,
        /// <summary>
        /// Content is aligned to the right.
        /// </summary>
        Right,
        /// <summary>
        /// Content is aligned to the bottom.
        /// </summary>
        Bottom,
        /// <summary>
        /// Content is centered.
        /// </summary>
        Center,
        /// <summary>
        /// Content is stretched to fill the available space.
        /// </summary>
        Stretch
    }

    /// <summary>
    /// Specifies what content is displayed in the button.
    /// </summary>
    public enum DisplayMode
    {
        /// <summary>
        /// Both icon and text are displayed.
        /// </summary>
        IconAndText,
        /// <summary>
        /// Only the icon is displayed.
        /// </summary>
        IconOnly,
        /// <summary>
        /// Only the text is displayed.
        /// </summary>
        TextOnly
    }

    /// <summary>
    /// A customizable button control with icon support, hover effects, press animations, and flexible layout options.
    /// Provides advanced styling capabilities including shadows, custom colors for different states, and smooth visual transitions.
    /// </summary>
    public class ExtendedButton : Button
    {
        #region Private Fields
        private Border _rootBorder;
        private Grid _contentGrid;
        private Path _iconPath;
        private TextBlock _textBlock;
        private DropShadowEffect _shadowEffect;
        private bool _isHovered;
        private bool _isPressed;
        private bool _templateApplied;
        private Thickness _originalBorderThickness;
        private TransformGroup _transformGroup;
        private ScaleTransform _scaleTransform;
        private TranslateTransform _translateTransform;
        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(ExtendedButton),
                new PropertyMetadata(new CornerRadius(0), OnCornerRadiusChanged));

        /// <summary>
        /// Identifies the <see cref="IconGeometry"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconGeometryProperty =
            DependencyProperty.Register(nameof(IconGeometry), typeof(Geometry), typeof(ExtendedButton),
                new PropertyMetadata(null, OnIconGeometryChanged));

        /// <summary>
        /// Identifies the <see cref="IconPosition"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconPositionProperty =
            DependencyProperty.Register(nameof(IconPosition), typeof(IconPosition), typeof(ExtendedButton),
                new PropertyMetadata(IconPosition.Left, OnLayoutPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="IconAlignment"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconAlignmentProperty =
            DependencyProperty.Register(nameof(IconAlignment), typeof(ContentAlignment), typeof(ExtendedButton),
                new PropertyMetadata(ContentAlignment.Center, OnLayoutPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="TextAlignment"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextAlignmentProperty =
            DependencyProperty.Register(nameof(TextAlignment), typeof(ContentAlignment), typeof(ExtendedButton),
                new PropertyMetadata(ContentAlignment.Center, OnLayoutPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="DisplayMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DisplayModeProperty =
            DependencyProperty.Register(nameof(DisplayMode), typeof(DisplayMode), typeof(ExtendedButton),
                new PropertyMetadata(DisplayMode.IconAndText, OnDisplayModeChanged));

        /// <summary>
        /// Identifies the <see cref="HasShadow"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HasShadowProperty =
            DependencyProperty.Register(nameof(HasShadow), typeof(bool), typeof(ExtendedButton),
                new PropertyMetadata(false, OnShadowChanged));

        /// <summary>
        /// Identifies the <see cref="IconSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(ExtendedButton),
                new PropertyMetadata(16.0, OnIconSizeChanged));

        /// <summary>
        /// Identifies the <see cref="IconBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconBrushProperty =
            DependencyProperty.Register(nameof(IconBrush), typeof(Brush), typeof(ExtendedButton),
                new PropertyMetadata(new SolidColorBrush(Colors.Black), OnIconBrushChanged));

        /// <summary>
        /// Identifies the <see cref="HoverBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HoverBackgroundProperty =
            DependencyProperty.Register(nameof(HoverBackground), typeof(Brush), typeof(ExtendedButton),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="HoverBorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HoverBorderBrushProperty =
            DependencyProperty.Register(nameof(HoverBorderBrush), typeof(Brush), typeof(ExtendedButton),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="HoverForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HoverForegroundProperty =
            DependencyProperty.Register(nameof(HoverForeground), typeof(Brush), typeof(ExtendedButton),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="HoverIconBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HoverIconBrushProperty =
            DependencyProperty.Register(nameof(HoverIconBrush), typeof(Brush), typeof(ExtendedButton),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="PressedBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PressedBackgroundProperty =
            DependencyProperty.Register(nameof(PressedBackground), typeof(Brush), typeof(ExtendedButton),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="PressedBorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PressedBorderBrushProperty =
            DependencyProperty.Register(nameof(PressedBorderBrush), typeof(Brush), typeof(ExtendedButton),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="PressedForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PressedForegroundProperty =
            DependencyProperty.Register(nameof(PressedForeground), typeof(Brush), typeof(ExtendedButton),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="PressedIconBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PressedIconBrushProperty =
            DependencyProperty.Register(nameof(PressedIconBrush), typeof(Brush), typeof(ExtendedButton),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="AnimationDuration"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AnimationDurationProperty =
            DependencyProperty.Register(nameof(AnimationDuration), typeof(TimeSpan), typeof(ExtendedButton),
                new PropertyMetadata(TimeSpan.FromMilliseconds(150)));

        /// <summary>
        /// Identifies the <see cref="IconMargin"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconMarginProperty =
            DependencyProperty.Register(nameof(IconMargin), typeof(Thickness), typeof(ExtendedButton),
                new PropertyMetadata(new Thickness(0), OnLayoutPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="TextMargin"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextMarginProperty =
            DependencyProperty.Register(nameof(TextMargin), typeof(Thickness), typeof(ExtendedButton),
                new PropertyMetadata(new Thickness(0), OnLayoutPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ButtonText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ButtonTextProperty =
            DependencyProperty.Register(nameof(ButtonText), typeof(string), typeof(ExtendedButton),
                new PropertyMetadata("", OnButtonTextChanged));

        /// <summary>
        /// Identifies the <see cref="IconTextSpacing"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconTextSpacingProperty =
            DependencyProperty.Register(nameof(IconTextSpacing), typeof(double), typeof(ExtendedButton),
                new PropertyMetadata(8.0, OnLayoutPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ContentPadding"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ContentPaddingProperty =
            DependencyProperty.Register(nameof(ContentPadding), typeof(Thickness), typeof(ExtendedButton),
                new PropertyMetadata(new Thickness(12, 6, 12, 6), OnLayoutPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="PressedScale"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PressedScaleProperty =
            DependencyProperty.Register(nameof(PressedScale), typeof(double), typeof(ExtendedButton),
                new PropertyMetadata(1.0));

        /// <summary>
        /// Identifies the <see cref="PressedOffset"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PressedOffsetProperty =
            DependencyProperty.Register(nameof(PressedOffset), typeof(Point), typeof(ExtendedButton),
                new PropertyMetadata(new Point(0, 0)));

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the corner radius of the button.
        /// </summary>
        /// <value>The corner radius. Default is 0.</value>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Gets or sets the geometry used for the icon.
        /// </summary>
        /// <value>The icon geometry. Can be any valid Path geometry.</value>
        public Geometry IconGeometry
        {
            get => (Geometry)GetValue(IconGeometryProperty);
            set => SetValue(IconGeometryProperty, value);
        }

        /// <summary>
        /// Gets or sets the position of the icon relative to the text.
        /// </summary>
        /// <value>The icon position. Default is Left.</value>
        public IconPosition IconPosition
        {
            get => (IconPosition)GetValue(IconPositionProperty);
            set => SetValue(IconPositionProperty, value);
        }

        /// <summary>
        /// Gets or sets the alignment of the icon within its allocated space.
        /// </summary>
        /// <value>The icon alignment. Default is Center.</value>
        public ContentAlignment IconAlignment
        {
            get => (ContentAlignment)GetValue(IconAlignmentProperty);
            set => SetValue(IconAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the alignment of the text within its allocated space.
        /// </summary>
        /// <value>The text alignment. Default is Center.</value>
        public ContentAlignment TextAlignment
        {
            get => (ContentAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets what content is displayed in the button.
        /// </summary>
        /// <value>The display mode. Default is IconAndText.</value>
        public DisplayMode DisplayMode
        {
            get => (DisplayMode)GetValue(DisplayModeProperty);
            set => SetValue(DisplayModeProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the button has a drop shadow effect.
        /// </summary>
        /// <value>true if the button has a shadow; otherwise, false. Default is false.</value>
        public bool HasShadow
        {
            get => (bool)GetValue(HasShadowProperty);
            set => SetValue(HasShadowProperty, value);
        }

        /// <summary>
        /// Gets or sets the size of the icon.
        /// </summary>
        /// <value>The icon size in device-independent pixels. Default is 16.</value>
        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used to paint the icon.
        /// </summary>
        /// <value>The icon brush. Default is black.</value>
        public Brush IconBrush
        {
            get => (Brush)GetValue(IconBrushProperty);
            set => SetValue(IconBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the background brush used when the button is hovered.
        /// </summary>
        /// <value>The hover background brush. If null, a calculated lighter color is used.</value>
        public Brush HoverBackground
        {
            get => (Brush)GetValue(HoverBackgroundProperty);
            set => SetValue(HoverBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the border brush used when the button is hovered.
        /// </summary>
        /// <value>The hover border brush. If null, a calculated lighter color is used.</value>
        public Brush HoverBorderBrush
        {
            get => (Brush)GetValue(HoverBorderBrushProperty);
            set => SetValue(HoverBorderBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the foreground brush used when the button is hovered.
        /// </summary>
        /// <value>The hover foreground brush. If null, a calculated lighter color is used.</value>
        public Brush HoverForeground
        {
            get => (Brush)GetValue(HoverForegroundProperty);
            set => SetValue(HoverForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the icon brush used when the button is hovered.
        /// </summary>
        /// <value>The hover icon brush. If null, a calculated lighter color is used.</value>
        public Brush HoverIconBrush
        {
            get => (Brush)GetValue(HoverIconBrushProperty);
            set => SetValue(HoverIconBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the background brush used when the button is pressed.
        /// </summary>
        /// <value>The pressed background brush. If null, a calculated darker color is used.</value>
        public Brush PressedBackground
        {
            get => (Brush)GetValue(PressedBackgroundProperty);
            set => SetValue(PressedBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the border brush used when the button is pressed.
        /// </summary>
        /// <value>The pressed border brush. If null, a calculated darker color is used.</value>
        public Brush PressedBorderBrush
        {
            get => (Brush)GetValue(PressedBorderBrushProperty);
            set => SetValue(PressedBorderBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the foreground brush used when the button is pressed.
        /// </summary>
        /// <value>The pressed foreground brush. If null, a calculated darker color is used.</value>
        public Brush PressedForeground
        {
            get => (Brush)GetValue(PressedForegroundProperty);
            set => SetValue(PressedForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the icon brush used when the button is pressed.
        /// </summary>
        /// <value>The pressed icon brush. If null, a calculated darker color is used.</value>
        public Brush PressedIconBrush
        {
            get => (Brush)GetValue(PressedIconBrushProperty);
            set => SetValue(PressedIconBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the duration of visual state transition animations.
        /// </summary>
        /// <value>The animation duration. Default is 150 milliseconds.</value>
        public TimeSpan AnimationDuration
        {
            get => (TimeSpan)GetValue(AnimationDurationProperty);
            set => SetValue(AnimationDurationProperty, value);
        }

        /// <summary>
        /// Gets or sets the margin around the icon.
        /// </summary>
        /// <value>The icon margin. Default is 0.</value>
        public Thickness IconMargin
        {
            get => (Thickness)GetValue(IconMarginProperty);
            set => SetValue(IconMarginProperty, value);
        }

        /// <summary>
        /// Gets or sets the margin around the text.
        /// </summary>
        /// <value>The text margin. Default is 0.</value>
        public Thickness TextMargin
        {
            get => (Thickness)GetValue(TextMarginProperty);
            set => SetValue(TextMarginProperty, value);
        }

        /// <summary>
        /// Gets or sets the text content of the button.
        /// </summary>
        /// <value>The button text. Takes precedence over the Content property for text display.</value>
        public string ButtonText
        {
            get => (string)GetValue(ButtonTextProperty);
            set => SetValue(ButtonTextProperty, value);
        }

        /// <summary>
        /// Gets or sets the spacing between the icon and text.
        /// </summary>
        /// <value>The spacing in device-independent pixels. Default is 8.</value>
        public double IconTextSpacing
        {
            get => (double)GetValue(IconTextSpacingProperty);
            set => SetValue(IconTextSpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets the padding inside the button content area.
        /// </summary>
        /// <value>The content padding. Default is 12,6,12,6.</value>
        public Thickness ContentPadding
        {
            get => (Thickness)GetValue(ContentPaddingProperty);
            set => SetValue(ContentPaddingProperty, value);
        }

        /// <summary>
        /// Gets or sets the scale factor applied when the button is pressed.
        /// </summary>
        /// <value>The pressed scale factor. Default is 1.0 (no scaling).</value>
        public double PressedScale
        {
            get => (double)GetValue(PressedScaleProperty);
            set => SetValue(PressedScaleProperty, value);
        }

        /// <summary>
        /// Gets or sets the offset applied when the button is pressed.
        /// </summary>
        /// <value>The pressed offset point. Default is 0,0 (no offset).</value>
        public Point PressedOffset
        {
            get => (Point)GetValue(PressedOffsetProperty);
            set => SetValue(PressedOffsetProperty, value);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedButton"/> class.
        /// </summary>
        public ExtendedButton()
        {
            DefaultStyleKey = typeof(ExtendedButton);
            InitializeTransforms();
            SetupEventHandlers();
            Loaded += ExtendedButton_Loaded;

            Cursor = Cursors.Hand;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes the transform objects used for press animations.
        /// </summary>
        private void InitializeTransforms()
        {
            _scaleTransform = new ScaleTransform { ScaleX = 1.0, ScaleY = 1.0 };
            _translateTransform = new TranslateTransform { X = 0, Y = 0 };
            _transformGroup = new TransformGroup();
            _transformGroup.Children.Add(_scaleTransform);
            _transformGroup.Children.Add(_translateTransform);
        }

        /// <summary>
        /// Handles the Loaded event to create the button template if not already applied.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ExtendedButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_templateApplied)
            {
                CreateTemplate();
            }
        }

        /// <summary>
        /// Creates the visual template for the button programmatically.
        /// </summary>
        private void CreateTemplate()
        {
            _originalBorderThickness = BorderThickness.Left > 0 ? BorderThickness : new Thickness(1);

            _rootBorder = new Border
            {
                CornerRadius = CornerRadius,
                Background = Background ?? new SolidColorBrush(Colors.LightGray),
                BorderBrush = BorderBrush ?? new SolidColorBrush(Colors.Gray),
                BorderThickness = _originalBorderThickness,
                Padding = ContentPadding,
                RenderTransform = _transformGroup,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };

            _contentGrid = new Grid();

            _iconPath = new Path
            {
                Data = IconGeometry,
                Fill = IconBrush,
                Width = IconSize,
                Height = IconSize,
                Stretch = Stretch.Uniform,
                Margin = IconMargin
            };

            var textContent = !string.IsNullOrEmpty(ButtonText) ? ButtonText : (Content?.ToString() ?? "");
            _textBlock = new TextBlock
            {
                Text = textContent,
                Foreground = Foreground ?? new SolidColorBrush(Colors.Black),
                FontFamily = FontFamily,
                FontSize = FontSize,
                FontWeight = FontWeight,
                Margin = TextMargin
            };

            _shadowEffect = new DropShadowEffect
            {
                BlurRadius = 8,
                Direction = 315,
                ShadowDepth = 2,
                Opacity = 0.3,
                Color = Colors.Black
            };

            _rootBorder.Child = _contentGrid;
            Content = _rootBorder;

            UpdateLayout();
            _templateApplied = true;
        }

        /// <summary>
        /// Updates the layout of the button content based on current property values.
        /// </summary>
        private void UpdateLayout()
        {
            if (_contentGrid == null) return;

            _contentGrid.Children.Clear();
            _contentGrid.RowDefinitions.Clear();
            _contentGrid.ColumnDefinitions.Clear();

            switch (DisplayMode)
            {
                case DisplayMode.IconOnly:
                    SetupIconOnlyLayout();
                    break;
                case DisplayMode.TextOnly:
                    SetupTextOnlyLayout();
                    break;
                case DisplayMode.IconAndText:
                    SetupIconAndTextLayout();
                    break;
            }

            UpdateShadow();
        }

        /// <summary>
        /// Sets up the layout for icon-only display mode.
        /// </summary>
        private void SetupIconOnlyLayout()
        {
            if (_iconPath?.Data != null)
            {
                SetAlignment(_iconPath, IconAlignment);
                _contentGrid.Children.Add(_iconPath);
            }
        }

        /// <summary>
        /// Sets up the layout for text-only display mode.
        /// </summary>
        private void SetupTextOnlyLayout()
        {
            if (_textBlock != null)
            {
                SetAlignment(_textBlock, TextAlignment);
                _contentGrid.Children.Add(_textBlock);
            }
        }

        /// <summary>
        /// Sets up the layout for icon and text display mode.
        /// </summary>
        private void SetupIconAndTextLayout()
        {
            if (_iconPath?.Data == null)
            {
                SetupTextOnlyLayout();
                return;
            }

            switch (IconPosition)
            {
                case IconPosition.Left:
                    SetupHorizontalLayout(iconFirst: true);
                    break;
                case IconPosition.Right:
                    SetupHorizontalLayout(iconFirst: false);
                    break;
                case IconPosition.Top:
                    SetupVerticalLayout(iconFirst: true);
                    break;
                case IconPosition.Bottom:
                    SetupVerticalLayout(iconFirst: false);
                    break;
            }

            if (_iconPath != null) _contentGrid.Children.Add(_iconPath);
            if (_textBlock != null) _contentGrid.Children.Add(_textBlock);
        }

        /// <summary>
        /// Sets up horizontal layout (Left/Right icon positions).
        /// </summary>
        /// <param name="iconFirst">Whether the icon should be placed first.</param>
        private void SetupHorizontalLayout(bool iconFirst)
        {
            _contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            if (iconFirst)
            {
                Grid.SetColumn(_iconPath, 0);
                Grid.SetColumn(_textBlock, 1);
                _iconPath.Margin = new Thickness(IconMargin.Left, IconMargin.Top, IconMargin.Right + IconTextSpacing, IconMargin.Bottom);
                _textBlock.Margin = TextMargin;
            }
            else
            {
                Grid.SetColumn(_textBlock, 0);
                Grid.SetColumn(_iconPath, 1);
                _textBlock.Margin = new Thickness(TextMargin.Left, TextMargin.Top, TextMargin.Right + IconTextSpacing, TextMargin.Bottom);
                _iconPath.Margin = IconMargin;
            }

            SetAlignment(_iconPath, IconAlignment);
            SetAlignment(_textBlock, TextAlignment);
        }

        /// <summary>
        /// Sets up vertical layout (Top/Bottom icon positions).
        /// </summary>
        /// <param name="iconFirst">Whether the icon should be placed first.</param>
        private void SetupVerticalLayout(bool iconFirst)
        {
            _contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            if (iconFirst)
            {
                Grid.SetRow(_iconPath, 0);
                Grid.SetRow(_textBlock, 1);
                _iconPath.Margin = new Thickness(IconMargin.Left, IconMargin.Top, IconMargin.Right, IconMargin.Bottom + IconTextSpacing);
                _textBlock.Margin = TextMargin;
            }
            else
            {
                Grid.SetRow(_textBlock, 0);
                Grid.SetRow(_iconPath, 1);
                _textBlock.Margin = new Thickness(TextMargin.Left, TextMargin.Top, TextMargin.Right, TextMargin.Bottom + IconTextSpacing);
                _iconPath.Margin = IconMargin;
            }

            SetAlignment(_iconPath, IconAlignment);
            SetAlignment(_textBlock, TextAlignment);
        }

        /// <summary>
        /// Sets the alignment properties for a framework element.
        /// </summary>
        /// <param name="element">The element to align.</param>
        /// <param name="alignment">The desired alignment.</param>
        private void SetAlignment(FrameworkElement element, ContentAlignment alignment)
        {
            if (element == null) return;

            switch (alignment)
            {
                case ContentAlignment.Left:
                    element.HorizontalAlignment = HorizontalAlignment.Left;
                    element.VerticalAlignment = VerticalAlignment.Center;
                    break;
                case ContentAlignment.Top:
                    element.HorizontalAlignment = HorizontalAlignment.Center;
                    element.VerticalAlignment = VerticalAlignment.Top;
                    break;
                case ContentAlignment.Right:
                    element.HorizontalAlignment = HorizontalAlignment.Right;
                    element.VerticalAlignment = VerticalAlignment.Center;
                    break;
                case ContentAlignment.Bottom:
                    element.HorizontalAlignment = HorizontalAlignment.Center;
                    element.VerticalAlignment = VerticalAlignment.Bottom;
                    break;
                case ContentAlignment.Center:
                    element.HorizontalAlignment = HorizontalAlignment.Center;
                    element.VerticalAlignment = VerticalAlignment.Center;
                    break;
                case ContentAlignment.Stretch:
                    element.HorizontalAlignment = HorizontalAlignment.Stretch;
                    element.VerticalAlignment = VerticalAlignment.Stretch;
                    break;
            }
        }

        /// <summary>
        /// Updates the shadow effect based on the HasShadow property.
        /// </summary>
        private void UpdateShadow()
        {
            if (_rootBorder == null) return;
            _rootBorder.Effect = HasShadow ? _shadowEffect : null;
        }

        /// <summary>
        /// Transitions the button to its normal visual state.
        /// </summary>
        private void GoToNormalState()
        {
            if (_rootBorder == null) return;

            _rootBorder.Background = Background ?? new SolidColorBrush(Colors.LightGray);
            _rootBorder.BorderBrush = BorderBrush ?? new SolidColorBrush(Colors.Gray);
            _rootBorder.BorderThickness = _originalBorderThickness;
            _rootBorder.Margin = new Thickness(0);
            _rootBorder.Opacity = 1.0;

            ResetTransforms();
            ApplyForegroundColors(Foreground, IconBrush);
            ResetShadowToNormal();
        }

        /// <summary>
        /// Transitions the button to its hover visual state.
        /// </summary>
        private void GoToHoverState()
        {
            if (_rootBorder == null) return;

            var hoverBg = HoverBackground ?? GetCalculatedHoverColor(Background ?? new SolidColorBrush(Colors.LightGray));
            var hoverBorder = HoverBorderBrush ?? GetCalculatedHoverColor(BorderBrush ?? new SolidColorBrush(Colors.Gray));
            var hoverFg = HoverForeground ?? GetCalculatedHoverColor(Foreground ?? new SolidColorBrush(Colors.Black));
            var hoverIcon = HoverIconBrush ?? GetCalculatedHoverColor(IconBrush ?? new SolidColorBrush(Colors.Black));

            _rootBorder.Background = hoverBg;
            _rootBorder.BorderBrush = hoverBorder;
            _rootBorder.BorderThickness = _originalBorderThickness;
            _rootBorder.Margin = new Thickness(0);
            _rootBorder.Opacity = 1.0;

            ResetTransforms();
            ApplyForegroundColors(hoverFg, hoverIcon);
            ResetShadowToNormal();
        }

        /// <summary>
        /// Transitions the button to its pressed visual state.
        /// </summary>
        private void GoToPressedState()
        {
            if (_rootBorder == null) return;

            var pressedBg = PressedBackground ?? GetCalculatedPressedColor(Background ?? new SolidColorBrush(Colors.LightGray));
            var pressedBorder = PressedBorderBrush ?? GetCalculatedPressedColor(BorderBrush ?? new SolidColorBrush(Colors.Gray));
            var pressedFg = PressedForeground ?? GetCalculatedPressedColor(Foreground ?? new SolidColorBrush(Colors.Black));
            var pressedIcon = PressedIconBrush ?? GetCalculatedPressedColor(IconBrush ?? new SolidColorBrush(Colors.Black));

            _rootBorder.Background = pressedBg;
            _rootBorder.BorderBrush = pressedBorder;

            ApplyPressedTransforms();
            ApplyForegroundColors(pressedFg, pressedIcon);
            ApplyPressedShadow();
        }

        /// <summary>
        /// Resets all transforms to their default values.
        /// </summary>
        private void ResetTransforms()
        {
            if (_scaleTransform != null)
            {
                _scaleTransform.ScaleX = 1.0;
                _scaleTransform.ScaleY = 1.0;
            }
            if (_translateTransform != null)
            {
                _translateTransform.X = 0;
                _translateTransform.Y = 0;
            }
        }

        /// <summary>
        /// Applies transforms for the pressed state.
        /// </summary>
        private void ApplyPressedTransforms()
        {
            if (_scaleTransform != null)
            {
                _scaleTransform.ScaleX = PressedScale;
                _scaleTransform.ScaleY = PressedScale;
            }
            if (_translateTransform != null)
            {
                _translateTransform.X = PressedOffset.X;
                _translateTransform.Y = PressedOffset.Y;
            }
        }

        /// <summary>
        /// Applies foreground colors to text and icon elements.
        /// </summary>
        /// <param name="textBrush">The brush for the text.</param>
        /// <param name="iconBrush">The brush for the icon.</param>
        private void ApplyForegroundColors(Brush textBrush, Brush iconBrush)
        {
            if (_textBlock != null)
            {
                _textBlock.Foreground = textBrush ?? new SolidColorBrush(Colors.Black);
            }

            if (_iconPath != null)
            {
                _iconPath.Fill = iconBrush ?? new SolidColorBrush(Colors.Black);
            }
        }

        /// <summary>
        /// Resets the shadow effect to normal state values.
        /// </summary>
        private void ResetShadowToNormal()
        {
            if (HasShadow && _shadowEffect != null)
            {
                _shadowEffect.ShadowDepth = 2;
                _shadowEffect.Opacity = 0.3;
            }
        }

        /// <summary>
        /// Applies shadow effect values for the pressed state.
        /// </summary>
        private void ApplyPressedShadow()
        {
            if (HasShadow && _shadowEffect != null)
            {
                _shadowEffect.ShadowDepth = 1;
                _shadowEffect.Opacity = 0.2;
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
                    (byte)Math.Min(255, color.R + 30),
                    (byte)Math.Min(255, color.G + 30),
                    (byte)Math.Min(255, color.B + 30)
                ));
            }
            return originalBrush;
        }

        /// <summary>
        /// Calculates a darker color variant for pressed states.
        /// </summary>
        /// <param name="originalBrush">The original brush.</param>
        /// <returns>A brush with a darker color, or the original brush if not a solid color.</returns>
        private Brush GetCalculatedPressedColor(Brush originalBrush)
        {
            if (originalBrush is SolidColorBrush solidBrush)
            {
                var color = solidBrush.Color;
                return new SolidColorBrush(Color.FromArgb(
                    color.A,
                    (byte)Math.Max(0, color.R - 80),
                    (byte)Math.Max(0, color.G - 80),
                    (byte)Math.Max(0, color.B - 80)
                ));
            }
            return originalBrush;
        }

        /// <summary>
        /// Sets up event handlers for button interactions.
        /// </summary>
        private void SetupEventHandlers()
        {
            Click += OnClick;

            // Register mouse event handlers with forced event handling to ensure they fire
            AddHandler(UIElement.MouseLeftButtonDownEvent, new MouseButtonEventHandler(OnMouseDown), true);
            AddHandler(UIElement.MouseLeftButtonUpEvent, new MouseButtonEventHandler(OnMouseUp), true);
            AddHandler(UIElement.MouseEnterEvent, new MouseEventHandler(OnMouseEnter), true);
            AddHandler(UIElement.MouseLeaveEvent, new MouseEventHandler(OnMouseLeave), true);
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
            if (IsEnabled && !_isPressed)
            {
                _isHovered = true;
                GoToHoverState();
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
                _isPressed = false;
                GoToNormalState();
            }
        }

        /// <summary>
        /// Handles the mouse down event to trigger pressed state.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The mouse button event arguments.</param>
        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsEnabled)
            {
                _isPressed = true;
                GoToPressedState();
                e.Handled = false; // Allow the event to continue for click handling
            }
        }

        /// <summary>
        /// Handles the mouse up event to return to hover or normal state.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The mouse button event arguments.</param>
        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (IsEnabled && _isPressed)
            {
                _isPressed = false;
                if (_isHovered)
                    GoToHoverState();
                else
                    GoToNormalState();
            }
        }

        /// <summary>
        /// Handles the click event to ensure proper state after click.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The routed event arguments.</param>
        private void OnClick(object sender, RoutedEventArgs e)
        {
            if (IsEnabled)
            {
                if (_isHovered)
                    GoToHoverState();
                else
                    GoToNormalState();
            }
        }

        #endregion

        #region Property Change Handlers

        /// <summary>
        /// Handles changes to the CornerRadius property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnCornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedButton button && button._rootBorder != null)
            {
                button._rootBorder.CornerRadius = (CornerRadius)e.NewValue;
            }
        }

        /// <summary>
        /// Handles changes to the IconGeometry property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnIconGeometryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedButton button && button._iconPath != null)
            {
                button._iconPath.Data = (Geometry)e.NewValue;
                button.UpdateLayout();
            }
        }

        /// <summary>
        /// Handles changes to layout-affecting properties.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedButton button)
            {
                button.UpdateLayout();
            }
        }

        /// <summary>
        /// Handles changes to the DisplayMode property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnDisplayModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedButton button)
            {
                button.UpdateLayout();
            }
        }

        /// <summary>
        /// Handles changes to the HasShadow property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnShadowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedButton button)
            {
                button.UpdateShadow();
            }
        }

        /// <summary>
        /// Handles changes to the IconSize property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnIconSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedButton button && button._iconPath != null)
            {
                var size = (double)e.NewValue;
                button._iconPath.Width = size;
                button._iconPath.Height = size;
            }
        }

        /// <summary>
        /// Handles changes to the IconBrush property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnIconBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedButton button && button._iconPath != null)
            {
                button._iconPath.Fill = (Brush)e.NewValue;
            }
        }

        /// <summary>
        /// Handles changes to the ButtonText property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnButtonTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ExtendedButton button && button._textBlock != null)
            {
                button._textBlock.Text = e.NewValue?.ToString() ?? "";
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
            base.OnContentChanged(oldContent, newContent);

            // If ButtonText is not set, use the Content as text
            if (_textBlock != null && string.IsNullOrEmpty(ButtonText))
            {
                _textBlock.Text = newContent?.ToString() ?? "";
            }
        }

        #endregion
    }
}