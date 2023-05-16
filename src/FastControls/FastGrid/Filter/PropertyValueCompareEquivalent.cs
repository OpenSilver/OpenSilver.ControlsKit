using System;
using System.Collections.Generic;
using System.Text;

namespace FastGrid.FastGrid.Filter
{
    // allows comparing for equivalence
    public class PropertyValueCompareEquivalent
    {
        // ... just in case we end up comparing doubles
        public double DoubleTolerance { get; set; } = 0.0000001;

        // if two date-time values fall within the same Tolerance range, they are equivalent
        // in our case, if two dates are from the same minute, we condider them equivalent
        public int DateTimeToleranceMillisecs { get; set; } = 60 * 1000;

        public string DateTimeFormat { get; set; } = "yyyy/MM/dd HH:mm:ss";

        // used when computing unique values (when computing Filter possible values)
        // I need to easily know if two values are equivalent
        //
        // note: later I can add some extra members to deal with converting double/float/decimal into a unique hash
        public long NumberToLongHash(object a) {
            // the idea -> i want to convert doubles to integers, to easily convert all objects to unique values (which I can later filter by)
            var MULTIPLY_DOUBLES = 1000;
            if (a is double)
                // simple, for now
                return (long)((double)a * MULTIPLY_DOUBLES);
            if (a is float)
                // simple, for now
                return (long)((float)a * MULTIPLY_DOUBLES);
            if (a is decimal)
                // simple, for now
                return (long)(Convert.ToDouble(((decimal)a)) * MULTIPLY_DOUBLES);
            if (a is int)
                return ((int)a);
            if (a is short)
                return ((short)a);
            if (a is long)
                return ((long)a);
            if (a is byte)
                return ((byte)a);
            if (a is char)
                return ((char)a);
            if (a is ushort)
                return ((ushort)a);
            if (a is uint)
                return ((uint)a);
            if (a is ulong)
                // FIXME this can throw if result overflows
                return Convert.ToInt64((ulong)a) ;

            return 0;
        }

        private static DateTime _startDate = new DateTime(1970, 1, 1);
        public long DateTimeToLongHash(DateTime dt) {
            return (long)((dt - _startDate).TotalMilliseconds / DateTimeToleranceMillisecs);
        }

        public string DateTimeToString(DateTime dt) => dt.ToString(DateTimeFormat);

    }
}
