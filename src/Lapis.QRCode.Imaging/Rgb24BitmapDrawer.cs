using Lapis.QRCode.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lapis.QRCode.Imaging
{
    public class Rgb24BitmapDrawer : BitMatrixDrawerBase
    {
        public override IImage Draw(BitMatrix bitMatrix)
        {
            if (bitMatrix == null)
                throw new ArgumentNullException(nameof(bitMatrix));

            int rowCount = bitMatrix.RowCount;
            int columnCount = bitMatrix.ColumnCount;
            int imageHeight = CellSize * rowCount + Margin * 2;
            int imageWidth = CellSize * rowCount + Margin * 2;
            var image = new Rgb24BitmapFrame(imageHeight, imageWidth);

            for (int y = 0; y < imageHeight; y++)
            {
                for (int x = 0; x < imageWidth; x++)
                {
                    if (Margin <= x && x < imageWidth - Margin
                        && Margin <= y && y < imageHeight - Margin)
                    {
                        int c = (x - Margin) / CellSize;
                        int r = (y - Margin) / CellSize;
                        if (bitMatrix[r, c])                        
                            image.SetPixel(x, y, Foreground);                        
                        else                        
                            image.SetPixel(x, y, Background);   
                    }
                    else                    
                        image.SetPixel(x, y, Background);
                    
                }
            }
            return image;
        }        
    }

}
