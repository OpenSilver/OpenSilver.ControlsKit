using System;
using System.Collections.Generic;
using System.Text;

namespace OpenSilver.ControlsKit.Edit
{
    internal interface HandleCellInputBase {
        void Subscribe();
        void Unsubscribe();

        void GotFocus(bool viaClick);
        void LostFocus();
    }
}
