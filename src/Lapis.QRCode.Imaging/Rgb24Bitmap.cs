using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lapis.QRCode.Imaging
{
    public interface IRgb24BitmapBase : IImage
    {
        int Height { get; }

        int Width { get; }

        int GetPixel(int x, int y);

        void SetPixel(int x, int y, int pixel);

        int[] GetPixels();
    }

    public interface IRgb24BitmapFrame : IRgb24BitmapBase
    {
    }

    public interface IRgb24Bitmap : IRgb24BitmapBase
    {
        IRgb24BitmapFrame GetFrame(int index);

        int FrameCount { get; }
    }

    public class Rgb24BitmapFrame : IRgb24BitmapFrame
    {
        public int Width { get; }

        public int Height { get; }

        private readonly int[] _data;

        public Rgb24BitmapFrame(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException();
            Width = width;
            Height = height;
            _data = new int[width * height];
        }

        public int GetPixel(int x, int y)
        {
            if (x < 0 || Width <= x)
                throw new ArgumentOutOfRangeException();
            if (y < 0 || Height <= y)
                throw new ArgumentOutOfRangeException();
            return _data[y * Width + x];
        }

        public void SetPixel(int x, int y, int pixel)
        {
            if (x < 0 || Width <= x)
                throw new ArgumentOutOfRangeException();
            if (y < 0 || Height <= y)
                throw new ArgumentOutOfRangeException();
            pixel = 0xFFFFFF & pixel;
            _data[y * Width + x] = pixel;
        }

        public int[] GetPixels() =>  _data;

        public void Save(Stream stream)
        {
            this.Save(stream, new Gif87aEncoder());
        }
    }

    public class Rgb24Bitmap : IRgb24Bitmap
    {
        private Rgb24Bitmap()
        {
            Frames = new List<IRgb24BitmapFrame>();
        }

        public Rgb24Bitmap(IRgb24BitmapFrame frame) : this()
        {
            Frames.Add(frame);
        }

        public Rgb24Bitmap(IEnumerable<IRgb24BitmapFrame> frames)
        {
            if (frames == null)
                throw new ArgumentNullException(nameof(frames));
            Frames = frames.ToList();
            if (Frames.Count < 1)
                throw new ArgumentException("At least one frame is required.");
        }

        public IList<IRgb24BitmapFrame> Frames { get; }

        public int Height => Frames[0].Height;

        public int Width => Frames[0].Width;

        public int FrameCount => Frames.Count;

        public IRgb24BitmapFrame GetFrame(int index) => Frames[index];

        public int GetPixel(int x, int y) => Frames[0].GetPixel(x, y);

        public int[] GetPixels() => Frames[0].GetPixels();

        public void SetPixel(int x, int y, int pixel) => Frames[0].SetPixel(x, y, pixel);

        public void Save(Stream stream)
        {
            this.Save(stream, new Gif89aEncoder());
        }        
    }
}