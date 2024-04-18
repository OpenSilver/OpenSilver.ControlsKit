using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using OpenSilver.ControlsKit.FastGrid.Util;

namespace OpenSilver.ControlsKit.FastGrid.Edit
{
    internal class HandleCellInputGeneric : HandleCellInputBase {
        protected const int DelayBeforeFocusMs = 100;

        protected FrameworkElement _root;
        protected Control _control;
        protected FastGridViewEditCell _cell;

        public HandleCellInputGeneric(FrameworkElement root, Control control, FastGridViewEditCell cell) {
            _root = root;
            _cell = cell;
            _control = control ;
        }

        private async void _control_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_control.IsEnabled && _control.DataContext != null) {
                await Task.Delay(DelayBeforeFocusMs);
                StartEdit();
            }
        }

        protected virtual void StartEdit() {
            _control.Focus();
        }

        private async void _control_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool isEnabled && isEnabled) {
                await Task.Delay(DelayBeforeFocusMs);
                StartEdit();
            }
        }

        protected virtual void KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            var action = FastGridInternalUtil.KeyToNavigateAction(e);
            switch (action) {
                case KeyNavigateAction.Up:
                case KeyNavigateAction.Down:
                case KeyNavigateAction.Prev:
                case KeyNavigateAction.Next:
                case KeyNavigateAction.NextOrDown:
                case KeyNavigateAction.PrevOrUp:
                case KeyNavigateAction.Escape:
                    _cell.Navigate(action);
                    e.Handled = true;
                    break;

                case KeyNavigateAction.Toggle:
                case KeyNavigateAction.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public virtual void Subscribe() {
            if (_control == null)
                return;

            _control.KeyDown += KeyDown;

            _control.IsEnabledChanged += _control_IsEnabledChanged;
            _control.DataContextChanged += _control_DataContextChanged;
        }

        public virtual void Unsubscribe() {
            if (_control == null)
                return;

            _control.KeyDown -= KeyDown;
            _control.IsEnabledChanged -= _control_IsEnabledChanged;
            _control.DataContextChanged -= _control_DataContextChanged;
        }

        public virtual async void GotFocus(bool viaClick) {
            await Task.Delay(DelayBeforeFocusMs);
            StartEdit();
        }

        public virtual void LostFocus() {
        }
    }
}
