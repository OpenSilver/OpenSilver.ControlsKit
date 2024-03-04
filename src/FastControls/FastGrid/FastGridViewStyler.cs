using System;
using System.Collections.Generic;
using System.Text;

namespace FastGrid.FastGrid
{
    public class FastGridViewStyler
    {

        public virtual void StyleColumn(FastGridViewColumn column) {
            // you can override to style the column
            // the idea: you can't style it via xaml, since it's not a dependency object anymore
        }

    }
}
