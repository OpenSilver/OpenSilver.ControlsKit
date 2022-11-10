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
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using CSHTML5.Internal;

namespace OpenSilver.ControlsKit
{
    public class FastCheckBox : Control
    {
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(
                "IsChecked",
                typeof(bool?),
                typeof(FastCheckBox),
                new PropertyMetadata(false, OnIsCheckedChanged)
                {
                    CallPropertyChangedWhenLoadedIntoVisualTree = WhenToCallPropertyChangedEnum.IfPropertyIsSet
                });

        public static readonly DependencyProperty IsThreeStateProperty =
            DependencyProperty.Register(
                "IsThreeState",
                typeof(bool),
                typeof(FastCheckBox),
                new PropertyMetadata(false));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text",
                typeof(string),
                typeof(FastCheckBox),
                new PropertyMetadata(string.Empty, OnTextChanged)
                {
                    CallPropertyChangedWhenLoadedIntoVisualTree = WhenToCallPropertyChangedEnum.IfPropertyIsSet
                });

        private object _checkboxHtmlElementRef;

        private JavascriptCallback _jsCallbackOnClick;
        private object _labelHtmlElementRef;
        private object _spanHtmlElementRef;

        public FastCheckBox()
        {
            Unloaded += (s, e) => DisposeJsCallbacks();
        }

        internal override bool EnablePointerEventsCore
        {
            get { return true; }
        }

        public bool? IsChecked
        {
            get => (bool?)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        public bool IsThreeState
        {
            get => (bool)GetValue(IsThreeStateProperty);
            set => SetValue(IsThreeStateProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public event RoutedEventHandler Checked;
        public event RoutedEventHandler Indeterminate;
        public event RoutedEventHandler Unchecked;
        public event RoutedEventHandler Click;

        private void DisposeJsCallbacks()
        {
            _jsCallbackOnClick?.Dispose();
            _jsCallbackOnClick = null;
        }

        public override object CreateDomElement(object parentRef, out object domElementWhereToPlaceChildren)
        {
            INTERNAL_HtmlDomManager.CreateDomElementAppendItAndGetStyle("label", parentRef, this,
                    out _labelHtmlElementRef);

            _jsCallbackOnClick = JavascriptCallback.Create((Action)(() =>
            {
                OnToggle();

                // Trigger click notifications
                OnClick(new RoutedEventArgs
                {
                    OriginalSource = this
                });
            }));
            // Subscribe to the javascript click event through a simple listener
            Interop.ExecuteJavaScript("$0.onclick = function(e) { e.preventDefault(); $1(); }",
                _labelHtmlElementRef, _jsCallbackOnClick);

            INTERNAL_HtmlDomManager.CreateDomElementAppendItAndGetStyle(
                "input",
                _labelHtmlElementRef, this, out _checkboxHtmlElementRef);
            INTERNAL_HtmlDomManager.SetDomElementAttribute(_checkboxHtmlElementRef, "type", "checkbox");

            INTERNAL_HtmlDomManager.CreateDomElementAppendItAndGetStyle("span", _labelHtmlElementRef, this,
                out _spanHtmlElementRef);

            domElementWhereToPlaceChildren = _labelHtmlElementRef;
            return _labelHtmlElementRef;
        }

        protected virtual void UpdateCheckInterop()
        {
            if (IsChecked == true)
            {
                Interop.ExecuteJavaScript("$0.checked = true; $0.indeterminate = undefined", _checkboxHtmlElementRef);
            }
            else if (IsChecked.HasValue)
            {
                Interop.ExecuteJavaScript("$0.checked = false; $0.indeterminate = undefined", _checkboxHtmlElementRef);
            }
            else
            {
                Interop.ExecuteJavaScript("$0.checked = undefined; $0.indeterminate = true", _checkboxHtmlElementRef);
            }
        }

        protected internal virtual void OnToggle()
        {
            // If IsChecked == true && IsThreeState == true   --->  IsChecked = null
            // If IsChecked == true && IsThreeState == false  --->  IsChecked = false
            // If IsChecked == false                          --->  IsChecked = true
            // If IsChecked == null                           --->  IsChecked = false
            bool? isChecked;
            if (IsChecked == true)
            {
                isChecked = IsThreeState ? null : (bool?)false;
            }
            else // false or null
            {
                isChecked = IsChecked.HasValue; // HasValue returns true if IsChecked==false
            }

            SetCurrentValue(IsCheckedProperty, isChecked);
        }

        protected virtual void OnClick(RoutedEventArgs e)
        {
            Click?.Invoke(this, e);
        }

        protected virtual void OnChecked(RoutedEventArgs e)
        {
            Checked?.Invoke(this, e);
        }

        protected virtual void OnIndeterminate(RoutedEventArgs e)
        {
            Indeterminate?.Invoke(this, e);
        }

        protected virtual void OnUnchecked(RoutedEventArgs e)
        {
            Unchecked?.Invoke(this, e);
        }

        private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FastCheckBox checkbox = (FastCheckBox)d;
            if (checkbox == null || !checkbox.IsLoaded)
            {
                return;
            }

            bool? newValue = (bool?)e.NewValue;

            checkbox.UpdateCheckInterop();

            if (newValue == true)
            {
                checkbox.OnChecked(new RoutedEventArgs
                {
                    OriginalSource = checkbox
                });
            }
            else if (newValue == false)
            {
                checkbox.OnUnchecked(new RoutedEventArgs
                {
                    OriginalSource = checkbox
                });
            }
            else
            {
                checkbox.OnIndeterminate(new RoutedEventArgs
                {
                    OriginalSource = checkbox
                });
            }
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FastCheckBox element = (FastCheckBox)d;
            if (element.IsLoaded && e.NewValue != null)
            {
                Interop.ExecuteJavaScript(
                    "if($0.firstChild) { $0.removeChild($0.firstChild) }; $0.appendChild(document.createTextNode($1));",
                    element._spanHtmlElementRef, e.NewValue);
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (!(_checkboxHtmlElementRef is INTERNAL_HtmlDomElementReference) ||
                !(_spanHtmlElementRef is INTERNAL_HtmlDomElementReference))
            {
                return new Size();
            }

            // Get actual width with margin
            string sizeString = Interop.ExecuteJavaScript(@"
((parseInt(window.getComputedStyle($0).getPropertyValue('margin-left')) | 0) + (parseInt(window.getComputedStyle($0).getPropertyValue('margin-right')) | 0) + $0['offsetWidth'] +
(parseInt(window.getComputedStyle($1).getPropertyValue('margin-left')) | 0) + (parseInt(window.getComputedStyle($1).getPropertyValue('margin-right')) | 0) +  $1['offsetWidth']).toFixed(3) + '|' + 
Math.max((parseInt(window.getComputedStyle($0).getPropertyValue('margin-top')) | 0) + (parseInt(window.getComputedStyle($0).getPropertyValue('margin-bottom')) | 0) + $0['offsetHeight'],  
(parseInt(window.getComputedStyle($1).getPropertyValue('margin-top')) | 0) + (parseInt(window.getComputedStyle($1).getPropertyValue('margin-bottom')) | 0) + $1['offsetHeight']).toFixed(3);",
                _checkboxHtmlElementRef, _spanHtmlElementRef).ToString();

            int sepIndex = sizeString.IndexOf('|');
            if (sepIndex <= -1)
            {
                return new Size();
            }

            string actualWidthAsString = sizeString.Substring(0, sepIndex);
            string actualHeightAsString = sizeString.Substring(sepIndex + 1);
            double actualWidth = double.Parse(actualWidthAsString,
                CultureInfo.InvariantCulture);
            double actualHeight = double.Parse(actualHeightAsString,
                CultureInfo.InvariantCulture);
            return new Size(actualWidth, actualHeight);
        }
    }
}