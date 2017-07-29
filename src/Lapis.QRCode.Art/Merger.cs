using Lapis.QRCode.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lapis.QRCode.Art
{
    public interface IMerger
    {
        BitSquare Merge(BitSquare qrCode, int typeNumber, BitMatrix backgroundMatrix);
    }

    public class Merger : IMerger
    {        
        public BitSquare Merge(BitSquare qrCode, int typeNumber, BitMatrix backgroundMatrix)
        {
            if (qrCode == null)
                throw new ArgumentNullException(nameof(qrCode));
            if (backgroundMatrix == null)
                throw new ArgumentNullException(nameof(backgroundMatrix));

            int moduleCount = qrCode.Size;
            var result = new BitSquare(moduleCount * 3);
            backgroundMatrix.CopyTo(result);

            for (var r = 0; r < moduleCount; r += 1)
            {
                for (var c = 0; c < moduleCount; c += 1)
                {
                    if (QRCodeHelper.IsPositionProbePattern(typeNumber, r, c) ||
                        QRCodeHelper.IsPositionAdjustPattern(typeNumber, r, c))                    
                        result.Fill(r * 3, c * 3, 3, 3, qrCode[r, c]);                    
                    else                    
                        result[r * 3 + 1, c * 3 + 1] = qrCode[r, c];                    
                }
            }
            return result;
        }
    }
}
