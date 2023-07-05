using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace FastGrid.FastGrid
{
    public class BrushCache {
        // how many max brushes we can cache
        public int MaxBrushCache = 16 * 1024;
        private BrushCache() {
        }

        private Dictionary<uint, SolidColorBrush> _solidBrushes = new Dictionary<uint, SolidColorBrush>();

        // FIXME not implemented yet
        private Dictionary<string, LinearGradientBrush> _linearBrushes = new Dictionary<string, LinearGradientBrush>();

        public static BrushCache Inst { get; } = new BrushCache();

        private static uint ToInt(Color color) {
            uint argb = color.A;
            argb <<= 8;
            argb += color.R;
            argb <<= 8;
            argb += color.G;
            argb <<= 8;
            argb += color.B;
            return argb;
        }

        public SolidColorBrush GetByColor(Color color) {
            var key = ToInt(color);
            if (_solidBrushes.TryGetValue(key, out var brush))
                return brush;
            var newBrush = new SolidColorBrush(color);
            _solidBrushes.Add(key, newBrush);
            return newBrush;
        }
    }
}
