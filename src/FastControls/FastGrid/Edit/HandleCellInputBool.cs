using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OpenSilver.ControlsKit.FastGrid.Util;

namespace OpenSilver.ControlsKit.FastGrid.Edit
{
    internal class HandleCellInputBool : HandleCellInputGeneric
    {
        private CheckBox _checkBox;

        public HandleCellInputBool(FrameworkElement root, CheckBox checkBox, FastGridViewEditCell cell) : base(root, checkBox, cell){
            _checkBox = checkBox;
        }

        protected override void KeyDown(object sender, KeyEventArgs e) {
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
                    // handled by default
                    break;

                case KeyNavigateAction.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void GotFocus(bool viaClick) {
            // allow first click to toggle
            if (viaClick)
                _checkBox.IsChecked = !_checkBox.IsChecked;
            base.GotFocus(viaClick);
        }
    }
}
