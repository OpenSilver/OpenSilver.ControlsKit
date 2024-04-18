﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using OpenSilver.ControlsKit.FastGrid.Util;

namespace OpenSilver.ControlsKit.Edit
{
    internal class HandleCellInputText : HandleCellInputGeneric {
        private TextBox Text => _control as TextBox;

        public HandleCellInputText(FrameworkElement root, Control control, FastGridViewEditCell cell) : base(root, control, cell) {
        }

        protected override void StartEdit() {
            Text.SelectionStart = Text.Text.Length;
            base.StartEdit();
        }


        public override void Subscribe() {
            if (Text != null) {
                Text.AcceptsReturn = false;
                Text.AcceptsTab = false;
            }
            base.Subscribe();
        }
    }
}
