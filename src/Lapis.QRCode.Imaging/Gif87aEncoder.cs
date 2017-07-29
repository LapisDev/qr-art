using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lapis.QRCode.Imaging
{
    // Translate form
    //
    //---------------------------------------------------------------------
    //
    // QR Code Generator for TypeScript
    //
    // Copyright (c) 2015 Kazuhiko Arase
    //
    // URL: http://www.d-project.com/
    //
    // Licensed under the MIT license:
    //  http://www.opensource.org/licenses/mit-license.php
    //
    // The word 'QR Code' is registered trademark of
    // DENSO WAVE INCORPORATED
    //  http://www.denso-wave.com/qrcode/faqpatent-e.html
    //
    //---------------------------------------------------------------------
    public class Gif87aEncoder : IRgb24BitmapEncoder
    {        
        protected class GifEncodingContext
        {
            public GifEncodingContext(IRgb24BitmapBase bitmap, Stream stream)
            {
                Bitmap = bitmap;
                Stream = stream;
            }

            public IRgb24BitmapBase Bitmap { get; }

            public Stream Stream { get; }

            public int[] Data { get; set; }

            public int[] Colors { get; set; }

            public byte ColorTableSize { get; set; }
        }

        public virtual void Encode(IRgb24BitmapBase bitmap, Stream stream)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            WriteHeader(stream);

            var context = new GifEncodingContext(bitmap, stream);
            context.Data = bitmap.GetPixels();
            context.Colors = context.Data.Distinct().ToArray();

            WriteLogicalScreenDescriptor(context);

            // Global color map
            WriteColorTable(context);

            WriteImageDescriptor(context);
          
            // Local color table: empty

            WriteImageData(context);

            // Trailer
            stream.WriteByte(0x3B);
        }

        private void WriteHeader(Stream stream)
        {
            // Signature: GIF
            stream.WriteByte(0x47);
            stream.WriteByte(0x49);
            stream.WriteByte(0x46);
            // Version: 87a
            stream.WriteByte(0x38);
            stream.WriteByte(0x37);
            stream.WriteByte(0x61);
        }

        protected void WriteLogicalScreenDescriptor(GifEncodingContext context)
        {
            // Canvas width
            WriteInt16(context.Stream, context.Bitmap.Width, true);
            // Canvas height
            WriteInt16(context.Stream, context.Bitmap.Height, true);
            
            context.ColorTableSize = GetGlobalColorTableSize(context.Colors.Length);

            // Packed field
            // Global color table flag: 1
            // Color resolution: 111 (8 bits/pixel)
            // Sort flag: 0
            // Size of global color table: 2^(N+1)
            context.Stream.WriteByte((byte)(0b1_111_0_000 | context.ColorTableSize & 0b111));

            // Background color index
            context.Stream.WriteByte(0x00);
            // Pixel aspect ratio
            context.Stream.WriteByte(0x00);            
        }

        private byte GetGlobalColorTableSize(int colorCount)
        {
            int size = 0;
            if (colorCount > 256)
                throw new OverflowException("Too many colors.");
            while (colorCount > 2)
            {
                colorCount >>= 1;
                size += 1;
            }
            return (byte)size;
        }

        protected void WriteColorTable(GifEncodingContext context)
        {
            int colorCount = 1 << (context.ColorTableSize + 1);
            int count = 0;
            foreach (var color in context.Colors)
            {
                // RGB24
                WriteInt24(context.Stream, color);
                count += 1;
            }
            while (count < colorCount)
            {
                context.Stream.WriteByte(0);
                context.Stream.WriteByte(0);
                context.Stream.WriteByte(0);
                count += 1;
            }
        }

        protected void WriteImageDescriptor(GifEncodingContext context)
        {
            // Image separator
            context.Stream.WriteByte(0x2C);
            // Image left position
            WriteInt16(context.Stream, 0);
            // Image top position
            WriteInt16(context.Stream, 0);
            // Image width
            WriteInt16(context.Stream, context.Bitmap.Width, true);
            // Image height
            WriteInt16(context.Stream, context.Bitmap.Height, true);

            // Packed field
            // Local color table flag
            // Interlace flag
            // Sort flag
            // Reserved
            // Size of local color table
            context.Stream.WriteByte(0);            
        }

        protected void WriteImageData(GifEncodingContext context)
        {            
            int lzwMinCodeSize = context.ColorTableSize + 1;
            lzwMinCodeSize = lzwMinCodeSize < 2 ? 2 : lzwMinCodeSize;
            
            // LZW minimum code size
            context.Stream.WriteByte((byte)lzwMinCodeSize);

            byte[] raster = GetLzwRaster(context.Colors, context.Data, lzwMinCodeSize);
            int offset = 0;
            while (raster.Length - offset > 255)
            {
                // Number of bytes in sub-block
                context.Stream.WriteByte(255);
                // Sub-block
                context.Stream.Write(raster, offset, 255);
                offset += 255;
            }
            context.Stream.WriteByte((byte)(raster.Length - offset));
            context.Stream.Write(raster, offset, raster.Length - offset);
            
            // Block terminator
            context.Stream.WriteByte(0x00);
        }

        protected void WriteInt16(Stream stream, int i, bool littleEndian = false)
        {
            var b1 = (byte)(((uint)i >> 8) & 0xFF);
            var b2 = (byte)(i & 0xFF);
            if (littleEndian)
            {
                stream.WriteByte(b2);
                stream.WriteByte(b1);
            }
            else
            {
                stream.WriteByte(b1);
                stream.WriteByte(b2);
            }
        }

        protected void WriteInt24(Stream stream, int i, bool littleEndian = false)
        {
            var b1 = (byte)(((uint)i >> 16) & 0xFF);
            var b2 = (byte)(((uint)i >> 8) & 0xFF);
            var b3 = (byte)(i & 0xFF);
            if (littleEndian)
            {
                stream.WriteByte(b3);
                stream.WriteByte(b2);
                stream.WriteByte(b1);
            }
            else
            {
                stream.WriteByte(b1);
                stream.WriteByte(b2);
                stream.WriteByte(b3);
            }
        }
        
        private byte[] GetLzwRaster(IEnumerable<int> colors, int[] data, int lzwMinCodeSize)
        {
            int clearCode = 1 << lzwMinCodeSize;
            int endCode = (1 << lzwMinCodeSize) + 1;
            int bitLength = lzwMinCodeSize + 1;

            LzwTable table = new LzwTable();
            for (int i = 0; i < clearCode; i++)
            {
                table.Add(((char)i).ToString());
            }
            table.Add(((char)clearCode).ToString());
            table.Add(((char)endCode).ToString());

            var colorIndexs = colors
                .Select((color, i) => Tuple.Create(color, i))
                .ToDictionary(t => t.Item1, t => t.Item2);

            using (var byteOut = new MemoryStream())
            using (var bitOut = new BitOutputStream(byteOut))
            {
                bitOut.Write(clearCode, bitLength);
                var dataIndex = 0;
                var s = ((char)colorIndexs[data[dataIndex]]).ToString();
                dataIndex += 1;
                while (dataIndex < data.Length)
                {
                    var c = ((char)colorIndexs[data[dataIndex]]).ToString();
                    dataIndex += 1;
                    if (table.Contains(s + c))
                        s = s + c;                    
                    else
                    {
                        bitOut.Write(table.IndexOf(s), bitLength);
                        if (table.Count < 0xfff)
                        {
                            if (table.Count == (1 << bitLength))
                                bitLength += 1;
                            table.Add(s + c);
                        }
                        s = c;
                    }
                }
                bitOut.Write(table.IndexOf(s), bitLength);
                bitOut.Write(endCode, bitLength);
                bitOut.Flush();
                return byteOut.ToArray();
            }
        }
    }

    class LzwTable
    {
        private Dictionary<string, int> _map;

        public LzwTable()
        {
            _map = new Dictionary<string, int>();
        }

        public void Add(string key)
        {
            if (Contains(key))
            {
                throw new ArgumentException("Duplicated key:" + key);
            }
            _map.Add(key, _map.Count);
        }

        public int Count => _map.Count;

        public int IndexOf(string key) => _map[key];

        public bool Contains(string key) => _map.ContainsKey(key);
    }


    class BitOutputStream : Stream
    {
        private Stream _baseStream;

        private int _bitLength;

        private int _bitBuffer;

        public override bool CanRead => _baseStream.CanRead;

        public override bool CanSeek => _baseStream.CanSeek;

        public override bool CanWrite => _baseStream.CanWrite;

        public override long Length => _baseStream.Length;

        public override long Position
        {
            get { return _baseStream.Position; }
            set { _baseStream.Position = value; }
        }

        public BitOutputStream(Stream baseStream)
        {
            this._baseStream = baseStream;
            this._bitLength = 0;
        }

        public void Write(int data, int length)
        {
            if (((uint)data >> length) != 0)
            {
                throw new IOException("length over");
            }
            while (_bitLength + length >= 8)
            {
                _baseStream.WriteByte((byte)(0xff & ((data << _bitLength) | _bitBuffer)));
                length -= (8 - _bitLength);
                data = (int)((uint)data >> (8 - _bitLength));
                _bitBuffer = 0;
                _bitLength = 0;
            }
            _bitBuffer = (data << _bitLength) | _bitBuffer;
            _bitLength = _bitLength + length;
        }

        protected override void Dispose(bool disposing)
        {
            Flush();
            _baseStream.Dispose();
            base.Dispose(disposing);
        }

        public override void Flush()
        {
            if (_bitLength > 0)
            {
                _baseStream.WriteByte((byte)_bitBuffer);
            }
            _baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count) => _baseStream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);

        public override void SetLength(long value) => _baseStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => _baseStream.Write(buffer, offset, count);
    }
}