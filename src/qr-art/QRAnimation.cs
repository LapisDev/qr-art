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
        private static bool CheckImagePathAnimation(string imagePath, out IRgb24Bitmap animation)
        {
            if (imagePath == null)
            {
                animation = null;
                LogError("Image required.");
                return false;
            }
            if (!File.Exists(imagePath))
            {
                LogError("File not found.");
                animation = null;
                return false;
            }
            try
            {
                var img = Image.FromFile(imagePath) as Image;
                animation = new BitmapImage(img);
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                animation = null;
                return false;
            }
        }

        private static bool CheckFormatAnimation(string format, out IBitMatrixDrawer drawer)
        {
            if (format == null)
            {
                LogError("Format required.");
                drawer = null;
                return false;
            }
            if (format.Equals("gif", StringComparison.OrdinalIgnoreCase))
            {
                // drawer = new GraphicsDrawer();
                drawer = new Rgb24BitmapDrawer();
                return true;
            }
            LogError("Only gif format is supported for animated QR code.");
            drawer = null;
            return false;
        }
    }
}