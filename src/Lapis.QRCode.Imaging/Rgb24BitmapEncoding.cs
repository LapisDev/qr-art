using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lapis.QRCode.Imaging
{
    public interface IRgb24BitmapEncoder
    {
        void Encode(IRgb24BitmapBase bitmap, Stream stream);
    }

    public static class Rgb24BitmapEncoding
    {        
        public static void Save(this IRgb24BitmapBase bitmap, Stream stream, IRgb24BitmapEncoder encoder)
        {
            encoder.Encode(bitmap, stream);
        }
    }
}