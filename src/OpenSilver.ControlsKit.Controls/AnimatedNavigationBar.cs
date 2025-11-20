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
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace OpenSilver.ControlsKit
{
    /// <summary>
    /// A navigation bar control with smooth animated transitions for position, width, and text color changes.
    /// </summary>
    public class AnimatedNavigationBar : ListBox
    {
        private ContentPresenter _indicator;
        private ItemsPresenter _itemsPresenter;
        private DoubleAnimation _positionAnimation;
        private DoubleAnimation _widthAnimation;
        private Storyboard _indicatorStoryboard;
        private bool _isInitialized = false;
        private ListBoxItem _previousSelectedItem;

        /// <summary>
        /// Initializes static members of the <see cref="AnimatedNavigationBar"/> class.
        /// </summary>
        static AnimatedNavigationBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AnimatedNavigationBar),
                new FrameworkPropertyMetadata(typeof(AnimatedNavigationBar)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AnimatedNavigationBar"/> class.
        /// </summary>
        public AnimatedNavigationBar()
        {
            this.DefaultStyleKey = typeof(AnimatedNavigationBar);
        }

        /// <summary>
        /// Called when a template is applied to the control.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _indicator = GetTemplateChild("PART_Indicator") as ContentPresenter;
            _itemsPresenter = GetTemplateChild("PART_ItemsPresenter") as ItemsPresenter;
            InitializeAnimation();
            BindIndicatorProperties();

            // minimize the logic in design time
            if (DesignerProperties.GetIsInDesignMode(this)) return;

            if (SelectedItem != null)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var selectedContainer = ItemContainerGenerator.ContainerFromItem(SelectedItem) as ListBoxItem;
                    if (selectedContainer != null)
                    {
                        AnimateForeground(selectedContainer, SelectedForeground);
                        _previousSelectedItem = selectedContainer;
                    }
                }));
            }
        }


    /// <summary>
    /// Binds indicator dependency properties to the indicator element.
    /// </summary>
    private void BindIndicatorProperties()
        {
            if (_indicator != null)
            {
                _indicator.ContentTemplate = IndicatorTemplate;
                _indicator.Content = new object();
            }
        }

        /// <summary>
        /// Initializes the animation components.
        /// </summary>
        private void InitializeAnimation()
        {
            if (_indicator == null) return;

            _indicator.RenderTransform = new TranslateTransform();

            _positionAnimation = new DoubleAnimation
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(AnimationDuration)),
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseInOut }
            };

            _widthAnimation = new DoubleAnimation
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(AnimationDuration)),
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseInOut }
            };

            _indicatorStoryboard = new Storyboard();

            Storyboard.SetTarget(_positionAnimation, _indicator);
            Storyboard.SetTargetProperty(_positionAnimation, new PropertyPath("RenderTransform.(TranslateTransform.X)"));

            Storyboard.SetTarget(_widthAnimation, _indicator);
            Storyboard.SetTargetProperty(_widthAnimation, new PropertyPath("Width"));

            _indicatorStoryboard.Children.Add(_positionAnimation);
            _indicatorStoryboard.Children.Add(_widthAnimation);
        }

        /// <summary>
        /// Handles the selection changed event with animation.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            if (_previousSelectedItem != null)
            {
                AnimateForeground(_previousSelectedItem, Foreground);
            }

            if (SelectedItem != null)
            {
                var currentSelectedItem = ItemContainerGenerator.ContainerFromItem(SelectedItem) as ListBoxItem;
                if (currentSelectedItem != null)
                {
                    AnimateForeground(currentSelectedItem, SelectedForeground);
                    _previousSelectedItem = currentSelectedItem;
                }
            }

            UpdateIndicator();
        }

        /// <summary>
        /// Animates the foreground color of the specified item.
        /// </summary>
        /// <param name="item">The item to animate.</param>
        /// <param name="toBrush">The target brush.</param>
        private void AnimateForeground(ListBoxItem item, Brush toBrush)
        {
            if (item != null && toBrush is SolidColorBrush toSolidBrush)
            {
                var colorAnimation = new ColorAnimation
                {
                    To = toSolidBrush.Color,
                    Duration = new Duration(TimeSpan.FromMilliseconds(AnimationDuration)),
                    EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseInOut }
                };

                var foregroundStoryboard = new Storyboard();
                Storyboard.SetTarget(colorAnimation, item);
                Storyboard.SetTargetProperty(colorAnimation, new PropertyPath("Foreground.Color"));

                foregroundStoryboard.Children.Add(colorAnimation);
                foregroundStoryboard.Begin();
            }
        }

        /// <summary>
        /// Handles the render size changed event.
        /// </summary>
        /// <param name="sizeInfo">Information about the size change.</param>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateIndicator();
        }

        /// <summary>
        /// Updates the indicator position and size with proper offset calculation.
        /// </summary>
        private void UpdateIndicator()
        {
            if (_indicator == null || _itemsPresenter == null || SelectedIndex < 0 || ItemContainerGenerator == null)
                return;

            var selectedContainer = ItemContainerGenerator.ContainerFromIndex(SelectedIndex) as ListBoxItem;
            if (selectedContainer == null) return;

            try
            {
                var transform = selectedContainer.TransformToAncestor(_itemsPresenter);
                var position = transform.Transform(new Point(0, 0));
                var width = selectedContainer.ActualWidth;

                _positionAnimation.To = position.X;
                _widthAnimation.To = width;

                if (!_isInitialized)
                {
                    var translateTransform = _indicator.RenderTransform as TranslateTransform;
                    if (translateTransform != null)
                    {
                        translateTransform.X = position.X;
                    }
                    _indicator.Width = width;
                    _isInitialized = true;
                }
                else
                {
                    _indicatorStoryboard.Begin();
                }
            }
            catch (InvalidOperationException)
            {
                Dispatcher.BeginInvoke(new Action(UpdateIndicator));
            }
        }

        /// <summary>
        /// Handles the items collection changed event.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            Dispatcher.BeginInvoke(new Action(UpdateIndicator));
        }

        /// <summary>
        /// Gets or sets the animation duration in milliseconds.
        /// </summary>
        /// <value>The animation duration. Default is 300ms.</value>
        public int AnimationDuration
        {
            get { return (int)GetValue(AnimationDurationProperty); }
            set { SetValue(AnimationDurationProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="AnimationDuration"/> dependency property.
        /// </summary>
        /// <returns>The identifier for the <see cref="AnimationDuration"/> dependency property.</returns>
        public static readonly DependencyProperty AnimationDurationProperty =
            DependencyProperty.Register(nameof(AnimationDuration), typeof(int), typeof(AnimatedNavigationBar),
                new PropertyMetadata(300, OnAnimationDurationChanged));

        /// <summary>
        /// Gets or sets the padding for individual navigation items.
        /// </summary>
        /// <value>The item padding. Default is 20,10,20,10.</value>
        public Thickness ItemPadding
        {
            get { return (Thickness)GetValue(ItemPaddingProperty); }
            set { SetValue(ItemPaddingProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ItemPadding"/> dependency property.
        /// </summary>
        /// <returns>The identifier for the <see cref="ItemPadding"/> dependency property.</returns>
        public static readonly DependencyProperty ItemPaddingProperty =
            DependencyProperty.Register(nameof(ItemPadding), typeof(Thickness), typeof(AnimatedNavigationBar),
                new PropertyMetadata(new Thickness(20, 10, 20, 10)));

        /// <summary>
        /// Gets or sets the template for the indicator.
        /// </summary>
        /// <value>The indicator template.</value>
        public DataTemplate IndicatorTemplate
        {
            get { return (DataTemplate)GetValue(IndicatorTemplateProperty); }
            set { SetValue(IndicatorTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IndicatorTemplate"/> dependency property.
        /// </summary>
        /// <returns>The identifier for the <see cref="IndicatorTemplate"/> dependency property.</returns>
        public static readonly DependencyProperty IndicatorTemplateProperty =
            DependencyProperty.Register(nameof(IndicatorTemplate), typeof(DataTemplate), typeof(AnimatedNavigationBar),
                new PropertyMetadata(null, OnIndicatorTemplateChanged));

        /// <summary>
        /// Gets or sets the corner radius of the indicator.
        /// </summary>
        /// <value>The indicator corner radius.</value>
        public CornerRadius IndicatorCornerRadius
        {
            get { return (CornerRadius)GetValue(IndicatorCornerRadiusProperty); }
            set { SetValue(IndicatorCornerRadiusProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IndicatorCornerRadius"/> dependency property.
        /// </summary>
        /// <returns>The identifier for the <see cref="IndicatorCornerRadius"/> dependency property.</returns>
        public static readonly DependencyProperty IndicatorCornerRadiusProperty =
            DependencyProperty.Register(nameof(IndicatorCornerRadius), typeof(CornerRadius), typeof(AnimatedNavigationBar),
                new PropertyMetadata(new CornerRadius(0)));

        /// <summary>
        /// Gets or sets the foreground brush for selected items.
        /// </summary>
        /// <value>The selected foreground brush.</value>
        public Brush SelectedForeground
        {
            get { return (Brush)GetValue(SelectedForegroundProperty); }
            set { SetValue(SelectedForegroundProperty, value); }
        }
        /// <summary>
        /// Gets or sets the background brush for the indicator.
        /// </summary>
        /// <value>The indicator background brush.</value>
        public Brush IndicatorBackground
        {
            get { return (Brush)GetValue(IndicatorBackgroundProperty); }
            set { SetValue(IndicatorBackgroundProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="SelectedForeground"/> dependency property.
        /// </summary>
        /// <returns>The identifier for the <see cref="SelectedForeground"/> dependency property.</returns>
        public static readonly DependencyProperty SelectedForegroundProperty =
            DependencyProperty.Register(nameof(SelectedForeground), typeof(Brush), typeof(AnimatedNavigationBar),
                new PropertyMetadata(new SolidColorBrush(Colors.White)));

        /// <summary>
        /// Identifies the <see cref="IndicatorBackground"/> dependency property.
        /// </summary>
        /// <returns>The identifier for the <see cref="IndicatorBackground"/> dependency property.</returns>
        public static readonly DependencyProperty IndicatorBackgroundProperty =
            DependencyProperty.Register(nameof(IndicatorBackground), typeof(Brush), typeof(AnimatedNavigationBar),
                new PropertyMetadata(new SolidColorBrush(Colors.Transparent)));

        private static void OnAnimationDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AnimatedNavigationBar bar)
            {
                var duration = new Duration(TimeSpan.FromMilliseconds((int)e.NewValue));
                if (bar._positionAnimation != null)
                    bar._positionAnimation.Duration = duration;
                if (bar._widthAnimation != null)
                    bar._widthAnimation.Duration = duration;
            }
        }

        private static void OnIndicatorTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AnimatedNavigationBar bar)
            {
                bar.BindIndicatorProperties();
            }
        }
    }
}