using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lapis.QRCode.Imaging
{
    public class Gif89aEncoder : Gif87aEncoder
    {
        public override void Encode(IRgb24BitmapBase bitmap, Stream stream)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            WriteHeader(stream);

            var context = new GifEncodingContext(bitmap, stream);
            var bmp = bitmap as IRgb24Bitmap;
            if (bmp != null && bmp.FrameCount > 1)
            {
                context.Colors = Enumerable.Range(0, bmp.FrameCount)
                    .Select(i => bmp.GetFrame(i).GetPixels())
                    .SelectMany(l => l)
                    .Distinct()
                    .ToArray();

                WriteLogicalScreenDescriptor(context);
                WriteColorTable(context);
                WriteGraphicsControlExtension(context);

                for (var i = 0; i < bmp.FrameCount; i++)
                {
                    var frame = bmp.GetFrame(i);
                    var fContext = new GifEncodingContext(frame, stream);
                    fContext.Data = frame.GetPixels();
                    fContext.Colors = context.Colors;

                    WriteImageDescriptor(fContext);                    
                    WriteImageData(fContext);
                }
            }
            else
            {
                context.Data = bitmap.GetPixels();
                context.Colors = context.Data.Distinct().ToArray();
                WriteLogicalScreenDescriptor(context);
                WriteColorTable(context);
                WriteImageDescriptor(context);
                WriteImageData(context);
            }

            // Trailer
            stream.WriteByte(0x3B);
        }

        private void WriteHeader(Stream stream)
        {
            // Signature: GIF
            stream.WriteByte(0x47);
            stream.WriteByte(0x49);
            stream.WriteByte(0x46);
            // Version: 89a
            stream.WriteByte(0x38);
            stream.WriteByte(0x39);
            stream.WriteByte(0x61);
        }

        protected void WriteGraphicsControlExtension(GifEncodingContext context)
        {
            // Extension introducer
            context.Stream.WriteByte(0x21);
            // Graphic control label
            context.Stream.WriteByte(0xF9);
            // Block size
            context.Stream.WriteByte(0x04);

            // Packed field
            // Reserved
            // Disposal method: 1
            // User input flag
            // Transparent color flag
            context.Stream.WriteByte(0x04);

            // Delay time
            context.Stream.WriteByte(0x0A);
            context.Stream.WriteByte(0x00);

            // Transparent color index
            context.Stream.WriteByte(0x00);

            // Block terminator
            context.Stream.WriteByte(0x00);
        }
    }
}