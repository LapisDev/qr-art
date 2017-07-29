using Lapis.QRCode.Encoding;
using Lapis.QRCode.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lapis.QRCode.Art
{
    public class QRAnimationCreator : QRArtCreator
    {
        public QRAnimationCreator(
            IQRCodeEncoder qrCodeEncoder,
            IBinarizer binarizer, IMerger merger,
            IBitMatrixDrawer bitMatrixDrawer,
            Func<IReadOnlyList<IRgb24BitmapFrame>, IRgb24BitmapBase> frameMerger)
            : base(qrCodeEncoder, binarizer, merger, bitMatrixDrawer)
        {
            FrameMerger = frameMerger;
        }

        public Func<IReadOnlyList<IRgb24BitmapFrame>, IRgb24BitmapBase> FrameMerger { get; }

        public override IImage Create(string data, IRgb24BitmapBase image)
        {
            var bitmap = image as IRgb24Bitmap;
            if (bitmap?.FrameCount > 1)
            {
                var bitMatrix = QRCodeEncoder.Build(data);
                int moduleCount = bitMatrix.Size;
                var images = Enumerable.Range(0, bitmap.FrameCount).Select(i => 
                {
                    var imgMatrix = Binarizer.Binarize(bitmap.GetFrame(i), moduleCount * 3, moduleCount * 3);
                    var frameMatrix = Merger.Merge(bitMatrix, QRCodeEncoder.TypeNumber, imgMatrix);
                    return BitMatrixDrawer.Draw(frameMatrix);                    
                }).ToList();
                
                if (FrameMerger == null)
                    return images[0];

                var frames = images.Select(img => img as IRgb24BitmapFrame).ToList();
                if (frames.Contains(null))
                    return images[0];
                return FrameMerger(frames);                
            }
            else
                return base.Create(data, image);
        }
    }
}