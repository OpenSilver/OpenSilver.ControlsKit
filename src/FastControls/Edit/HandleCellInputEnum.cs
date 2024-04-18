using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;

namespace OpenSilver.ControlsKit.Edit
{
    internal class HandleCellInputEnum: HandleCellInputGeneric
    {
        private ComboBox Combo => _control as ComboBox;

        public HandleCellInputEnum(FrameworkElement root, Control control, FastGridViewEditCell cell) : base(root, control, cell){
            _root = root;
            _control = control;
            _cell = cell;
        }

        protected override void KeyDown(object sender, KeyEventArgs e) {
            // if combo open, disregard action
            if (Combo != null && Combo.IsDropDownOpen)
                return;

            base.KeyDown(sender, e);
        }
    }
}
