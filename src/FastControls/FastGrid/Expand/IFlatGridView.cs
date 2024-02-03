using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using FastGrid.FastGrid.Data;

namespace FastGrid.FastGrid.Expand
{
    internal class RowInfo {
        public readonly FastGridViewDataHolder DataHolder;
        public readonly object RowObject;

        public readonly ItemsControl HeaderControl = null;
        // unique ID of this header, to easily identify which headers to hide
        public readonly int HeaderId;

        public readonly int HeaderRowCount;
        public readonly int HeaderRowIndex;
        public int IndentLevel;

        public RowInfo(FastGridViewDataHolder dataHolder, object rowObject, int headerId, int headerRowCount, int startBeforeCount, int indentLevel) {
            RowObject = rowObject;
            HeaderId = headerId;
            HeaderRowCount = headerRowCount;
            HeaderRowIndex = startBeforeCount;
            IndentLevel = indentLevel;
            DataHolder = dataHolder;
            HeaderControl = dataHolder?.HeaderControl();
        }

        public RowInfo(FastGridViewDataHolder dataHolder, object rowObject, int indentLevel ) : this(dataHolder, rowObject, 0, 0, 0, indentLevel) {
            HeaderControl = null;
        }

    }

    internal interface IFlatGridView {
        int RowCount();
        // returns -1 if not found
        int ObjectToRowIndex(object obj, int suggestedFindIndex);
        // returns null if not found
        object RowIndexToObject(int idx);
        RowInfo RowIndexToInfo(int idx);

        (object TopRow, int TopRowIndex) ComputeTopRowIndex(object oldTopRow, int oldTopRowIndex);
        void OnBeforeUpdateUI();
        void OnCollectionUpdate(FastGridViewDataHolder dataHolder);
        
        void SetExpanded(object o, bool isExpanded);
        void ToggleExpanded(object o);
        
        int MaxRowIdx();
        void UpdateExpandRow(FastGridViewRow row);

        void FullReFilter();
    }
}
