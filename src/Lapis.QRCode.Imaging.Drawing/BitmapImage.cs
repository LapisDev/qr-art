using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Collections;

namespace Lapis.QRCode.Imaging.Drawing
{
    public class BitmapImage : IRgb24Bitmap, IDisposable
    {
        private BitmapImage()
        {
            Frames = new List<BitmapFrame>();
        }

        public BitmapImage(BitmapFrame frame) : this()
        {
            Frames.Add(frame);
        }

        public BitmapImage(IEnumerable<BitmapFrame> frames)
        {
            if (frames == null)
                throw new ArgumentNullException(nameof(frames));
            Frames = frames.ToList();
            if (Frames.Count < 1)
                throw new ArgumentException("At least one frame is required.");
        }

        public BitmapImage(IEnumerable<Bitmap> bitmaps)
            : this(bitmaps?.Select(b => new BitmapFrame(b)))
        {
        }

        public BitmapImage(Image image)
            : this(GetFrames(image))
        {
        }

        public IList<BitmapFrame> Frames { get; }

        public int Height => Frames[0].Height;

        public int Width => Frames[0].Width;

        public int FrameCount => Frames.Count;

        public IRgb24BitmapFrame GetFrame(int index) => Frames[index];

        public int GetPixel(int x, int y) => Frames[0].GetPixel(x, y);

        public int[] GetPixels() => Frames[0].GetPixels();

        public void SetPixel(int x, int y, int pixel) => Frames[0].SetPixel(x, y, pixel);

        public void Save(Stream stream)
        {
            if (FrameCount == 1)
                Frames[0].Save(stream);
            else
                SaveGifAnimation(Frames.Select(b => b.Bitmap).ToList(), stream, 10);
        }

        private static IEnumerable<Bitmap> GetFrames(Image image)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            var fd = new FrameDimension(image.FrameDimensionsList[0]);
            int count = image.GetFrameCount(fd);

            for (int i = 0; i < count; i++)
            {
                image.SelectActiveFrame(fd, i);
                yield return new Bitmap(image);
            }
        }

        private static void SaveGifAnimation(IReadOnlyList<Bitmap> bitmaps, Stream stream, float frameRate)
        {
            // Gdi+ constants absent from System.Drawing.
            const int PropertyTagFrameDelay = 0x5100;
            const int PropertyTagLoopCount = 0x5101;
            const short PropertyTagTypeLong = 4;
            const short PropertyTagTypeShort = 3;

            const int UintBytes = 4;

            var gifEncoder = GetEncoder(ImageFormat.Gif);
            // Params of the first frame.
            var encoderParams1 = new EncoderParameters(1);
            encoderParams1.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.MultiFrame);
            // Params of other frames.
            var encoderParamsN = new EncoderParameters(1);
            encoderParamsN.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.FrameDimensionTime);
            // Params for the finalizing call.
            var encoderParamsFlush = new EncoderParameters(1);
            encoderParamsFlush.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.Flush);

            // PropertyItem for the frame delay (apparently, no other way to create a fresh instance).
            var frameDelay = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
            frameDelay.Id = PropertyTagFrameDelay;
            frameDelay.Type = PropertyTagTypeLong;
            // Length of the value in bytes.
            frameDelay.Len = bitmaps.Count * UintBytes;
            // The value is an array of 4-byte entries: one per frame.
            // Every entry is the frame delay in 1/100-s of a second, in little endian.
            frameDelay.Value = new byte[bitmaps.Count * UintBytes];
            // E.g., here, we're setting the delay of every frame to 1 second.
            var frameDelayBytes = BitConverter.GetBytes((uint)(100 / frameRate));
            for (int j = 0; j < bitmaps.Count; ++j)
                Array.Copy(frameDelayBytes, 0, frameDelay.Value, j * UintBytes, UintBytes);

            // PropertyItem for the number of animation loops.
            var loopPropertyItem = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
            loopPropertyItem.Id = PropertyTagLoopCount;
            loopPropertyItem.Type = PropertyTagTypeShort;
            loopPropertyItem.Len = 1;
            // 0 means to animate forever.
            loopPropertyItem.Value = BitConverter.GetBytes((ushort)0);

            bool first = true;
            Bitmap firstBitmap = null;
            // Bitmaps is a collection of Bitmap instances that'll become gif frames.
            foreach (var bitmap in bitmaps)
            {
                if (first)
                {
                    firstBitmap = bitmap;
                    firstBitmap.SetPropertyItem(frameDelay);
                    firstBitmap.SetPropertyItem(loopPropertyItem);
                    firstBitmap.Save(stream, gifEncoder, encoderParams1);
                    first = false;
                }
                else
                {
                    firstBitmap.SaveAdd(bitmap, encoderParamsN);
                }
            }
            firstBitmap.SaveAdd(encoderParamsFlush);
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }


        #region IDisposable Support

        private bool _isDisposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    foreach (var bitmap in Frames)
                        bitmap.Dispose();
                }
                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }        

        #endregion

    }
}
