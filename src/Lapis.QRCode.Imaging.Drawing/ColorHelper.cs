using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lapis.QRCode.Imaging.Drawing
{
    public static class ColorHelper
    {
        public static Color FromIntRgb24(int rgb)
        {
            return Color.FromArgb(255, (rgb & 0xFF0000) >> 16, (rgb & 0xFF00) >> 8, rgb & 0xFF);
        }

        public static int ToIntRgb24(Color color)
        {
            return (color.R << 16) | (color.G << 8) | color.B;
        }
    }
}
