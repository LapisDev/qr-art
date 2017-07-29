using Lapis.QRCode.Encoding;
using Lapis.QRCode.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lapis.QRCode.Art
{
    public interface IQRArtCreator
    {
        IImage Create(string data, IRgb24BitmapBase image);
    }

    public class QRArtCreator : IQRArtCreator
    {
        public QRArtCreator(
            IQRCodeEncoder qrCodeEncoder,
            IBinarizer binarizer, IMerger merger,
            IBitMatrixDrawer bitMatrixDrawer)
        {
            if (qrCodeEncoder == null)
                throw new ArgumentNullException(nameof(qrCodeEncoder));
            if (binarizer == null)
                throw new ArgumentNullException(nameof(binarizer));
            if (merger == null)
                throw new ArgumentNullException(nameof(merger));
            if (bitMatrixDrawer == null)
                throw new ArgumentNullException(nameof(bitMatrixDrawer));
            QRCodeEncoder = qrCodeEncoder;
            Binarizer = binarizer;
            Merger = merger;
            BitMatrixDrawer = bitMatrixDrawer;
        }

        public IQRCodeEncoder QRCodeEncoder { get; }

        public IBinarizer Binarizer { get; }

        public IMerger Merger { get; }

        public IBitMatrixDrawer BitMatrixDrawer { get; }

        public virtual IImage Create(string data, IRgb24BitmapBase image)
        {
            var bitMatrix = QRCodeEncoder.Build(data);
            if (image != null)
            {
                int moduleCount = bitMatrix.Size;
                var imgMatrix = Binarizer.Binarize(image, moduleCount * 3, moduleCount * 3);
                bitMatrix = Merger.Merge(bitMatrix, QRCodeEncoder.TypeNumber, imgMatrix);
            }
            return BitMatrixDrawer.Draw(bitMatrix);
        }
    }
}
