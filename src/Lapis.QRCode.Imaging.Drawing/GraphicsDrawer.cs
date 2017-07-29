using Lapis.QRCode.Encoding;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lapis.QRCode.Imaging.Drawing
{
    public class GraphicsDrawer : BitMatrixDrawerBase
    {
        public override IImage Draw(BitMatrix bitMatrix)
        {
            if (bitMatrix == null)
                throw new ArgumentNullException(nameof(bitMatrix));

            int rowCount = bitMatrix.RowCount;
            int columnCount = bitMatrix.ColumnCount;
            int imageHeight = CellSize * rowCount + Margin * 2;
            int imageWidth = CellSize * rowCount + Margin * 2;
            var bitmap = new Bitmap(imageHeight, imageWidth);

            using (var graph = Graphics.FromImage(bitmap))
            {
                graph.Clear(ColorHelper.FromIntRgb24(Background));
                var foreBrush = new SolidBrush(ColorHelper.FromIntRgb24(Foreground));
                for (var r = 0; r < rowCount; r += 1)
                {
                    for (var c = 0; c < columnCount; c += 1)
                    {
                        if (bitMatrix[r, c])
                        {
                            var x = Margin + c * CellSize;
                            var y = Margin + r * CellSize;
                            graph.FillRectangle(foreBrush, x, y, CellSize, CellSize);
                        }
                    }
                }
            }

            return new BitmapFrame(bitmap);
        }         
        
    }
}
