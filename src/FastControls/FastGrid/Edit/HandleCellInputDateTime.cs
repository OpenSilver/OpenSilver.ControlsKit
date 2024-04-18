using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenSilver.ControlsKit.FastGrid.Edit
{
    internal class HandleCellInputDateTime: HandleCellInputGeneric
    {
        private DatePicker DatePicker => _control as DatePicker;
        private TimePicker TimePicker => _control as TimePicker;

        public HandleCellInputDateTime(FrameworkElement root, Control control, FastGridViewEditCell cell) : base(root, control, cell) {
            _root = root;
            _control = control;
            _cell = cell;
        }

        protected override void KeyDown(object sender, KeyEventArgs e) {
            if (DatePicker != null && DatePicker.IsDropDownOpen)
                return;
            if (TimePicker != null && TimePicker.IsDropDownOpen)
                return;

            base.KeyDown(sender, e);
        }
    }
}
