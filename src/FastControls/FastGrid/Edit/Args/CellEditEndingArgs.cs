using System;

namespace OpenSilver.ControlsKit.FastGrid.Edit.Args
{
    public class CellEditEndingArgs : EventArgs
    {
        public Object RowObject;
        public int Row, Column;
        // if true -> we're cancelling the save
        public bool Cancel = false;
    }
}
