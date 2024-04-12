using System;
using System.Collections.Generic;
using System.Text;

namespace OpenSilver.ControlsKit.Edit.Args
{
    public class CellEditBeginArgs : EventArgs {
        public Object RowObject;
        public int Row, Column;
        public bool Cancel = false;
    }
}
