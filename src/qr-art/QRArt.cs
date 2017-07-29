using System;
using System.IO;
using System.Drawing;
using Microsoft.Extensions.CommandLineUtils;
using Lapis.QRCode.Encoding;
using Lapis.QRCode.Art;
using Lapis.QRCode.Imaging;
using Lapis.QRCode.Imaging.Drawing;
using System.Collections.Generic;
using System.Linq;

namespace Lapis.QrArt
{
    partial class Program
    {
        private static bool CheckContent(string content)
        {
            if (content == null)
            {
                LogError("Content required.");
                return false;
            }
            return true;
        }

        private static bool CheckImagePath(string imagePath, out IRgb24BitmapBase bitmap)
        {
            if (imagePath == null)
            {
                bitmap = null;
                return true;
            }
            if (!File.Exists(imagePath))
            {
                LogError("File not found.");
                bitmap = null;
                return false;
            }
            try
            {
                var bmp = Bitmap.FromFile(imagePath) as Bitmap;
                bitmap = new BitmapFrame(bmp);
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                bitmap = null;
                return false;
            }
        }

        private static bool CheckFormat(string format, out IBitMatrixDrawer drawer)
        {
            if (format == null)
            {
                LogError("Format required.");
                drawer = null;
                return false;
            }
            if (format.Equals("svg", StringComparison.OrdinalIgnoreCase))
            {
                drawer = new SvgDrawer();
                return true;
            }
            if (format.Equals("gif", StringComparison.OrdinalIgnoreCase))
            {
                drawer = new Rgb24BitmapDrawer();
                return true;
            }
            if (format.Equals("png", StringComparison.OrdinalIgnoreCase))
            {
                drawer = new GraphicsDrawer();
                return true;
            }
            LogError("Format not supported.");
            drawer = null;
            return false;
        }

        private static bool CheckType(string type, out int typeNumber)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                typeNumber = 4;
                return true;
            }
            if (int.TryParse(type, out typeNumber) &&
                typeNumber > 0 && typeNumber < 40)
                return true;
            LogError("Invalid type number.");
            return false;
        }

        private static bool CheckErrorCorrectLevel(string errcor, out ErrorCorrectLevel errorCorrectLevel)
        {
            if (string.IsNullOrWhiteSpace(errcor))
            {
                errorCorrectLevel = ErrorCorrectLevel.M;
                return true;
            }
            if (Enum.TryParse<ErrorCorrectLevel>(errcor, out errorCorrectLevel))
                return true;
            LogError("Invalid error correct level.");
            return false;
        }

        private static bool CheckForeground(string foregd, out int color)
        {
            if (string.IsNullOrWhiteSpace(foregd))
            {
                color = 0x000000;
                return true;
            }
            if (int.TryParse(foregd.TrimStart('#'), System.Globalization.NumberStyles.HexNumber, null, out color))
                return true;
            LogError("Invalid foreground color.");
            return false;
        }

        private static bool CheckBackground(string backgd, out int color)
        {
            if (string.IsNullOrWhiteSpace(backgd))
            {
                color = 0xFFFFFF;
                return true;
            }
            if (int.TryParse(backgd.TrimStart('#'), System.Globalization.NumberStyles.HexNumber, null, out color))
                return true;
            LogError("Invalid background color.");
            return false;
        }

        private static bool CheckCell(string cell, out int cellSize)
        {
            if (string.IsNullOrWhiteSpace(cell))
            {
                cellSize = 2;
                return true;
            }
            if (int.TryParse(cell, out cellSize) &&
                cellSize > 0 && cellSize < 50)
                return true;
            LogError("Invalid cell size.");
            return false;
        }

        private static bool CheckMargin(string margin, out int marginValue)
        {
            if (string.IsNullOrWhiteSpace(margin))
            {
                marginValue = 8;
                return true;
            }
            if (int.TryParse(margin, out marginValue) &&
                marginValue > 0 && marginValue < 50)
                return true;
            LogError("Invalid margin.");
            return false;
        }

        private static void Write(IImage image, string path)
        {
            using (var stream = new System.IO.FileStream(path, System.IO.FileMode.OpenOrCreate))
            {
                image.Save(stream);
            }
        }
    }
}