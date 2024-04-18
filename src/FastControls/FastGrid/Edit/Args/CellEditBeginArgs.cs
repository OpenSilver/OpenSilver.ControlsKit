using System;

namespace OpenSilver.ControlsKit.FastGrid.Edit.Args
{
    public class CellEditBeginArgs : EventArgs {
        public Object RowObject;
        public int Row, Column;
        public bool Cancel = false;
    }
}
