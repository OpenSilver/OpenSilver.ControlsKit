using System;

namespace OpenSilver.ControlsKit.FastGrid.Edit.Args
{
    public class CellEditEndedArgs : EventArgs
    {
        public Object RowObject;
        public int Row, Column;
        // if true -> we've saved this edit. if false, we've cancelled
        public bool Saved = false;
    }
}
