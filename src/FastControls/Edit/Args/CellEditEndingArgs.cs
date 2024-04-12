using System;
using System.Collections.Generic;
using System.Text;

namespace OpenSilver.ControlsKit.Edit.Args
{
    public class CellEditEndingArgs : EventArgs
    {
        public Object RowObject;
        public int Row, Column;
        // if true -> we're cancelling the save
        public bool Cancel = false;
    }
}
