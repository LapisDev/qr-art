using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Lapis.QRCode.Encoding;

namespace Lapis.QRCode.Imaging
{
    public class SvgDrawer : BitMatrixDrawerBase
    {        
        public override IImage Draw(BitMatrix bitMatrix)
        {
            if (bitMatrix == null)
                throw new ArgumentNullException(nameof(bitMatrix));

            int rowCount = bitMatrix.RowCount;
            int columnCount = bitMatrix.ColumnCount;
            int imageHeight = CellSize * rowCount + Margin * 2;
            int imageWidth = CellSize * rowCount + Margin * 2;
            var image = new SvgImage(imageWidth, imageHeight);

            var back = new XElement("rect");
            back.SetAttributeValue("width", imageWidth.ToString() + "px");
            back.SetAttributeValue("height", imageHeight.ToString() + "px");
            back.SetAttributeValue("fill", "#" + Background.ToString("x6"));
            image.Add(back);

            var path = new XElement("path");
            var rect = "l" + CellSize + ",0 0," + CellSize + " -" + CellSize + ",0 0,-" + CellSize + "z ";
            var sb = new StringBuilder();
            for (var r = 0; r < rowCount; r += 1)
            {
                for (var c = 0; c < columnCount; c += 1)
                {
                    if (bitMatrix[r, c])
                    {
                        var x = Margin + c * CellSize;
                        var y = Margin + r * CellSize;
                        sb.Append("M").Append(x).Append(",").Append(y).Append(rect);
                    }
                }
            }
            path.SetAttributeValue("d", sb.ToString());
            path.SetAttributeValue("stroke", "transparent");
            path.SetAttributeValue("fill", "#" + Foreground.ToString("x6"));
            image.Add(path);

            return image;
        }
    }

    public class SvgImage : XElement, IImage
    {
        public SvgImage(float width, float height) : base(XName.Get("svg", "http://www.w3.org/2000/svg"))
        {
            SetAttributeValue("width", width.ToString() + "px");
            SetAttributeValue("height", height.ToString() + "px");
        }
    }
}
