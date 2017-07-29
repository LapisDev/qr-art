using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lapis.QRCode.Encoding;

namespace Lapis.QRCode.Imaging
{
    public interface IBitMatrixDrawer
    {
        int CellSize { get; set; }

        int Margin { get; set; }

        int Foreground { get; set; }

        int Background { get; set; }

        IImage Draw(BitMatrix bitMatrix);
    }

    public abstract class BitMatrixDrawerBase : IBitMatrixDrawer
    {
        public int CellSize 
        { 
            get { return _cellSize; } 
            set 
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(CellSize));
            }
        }

        private int _cellSize = 2;

        public int Margin
        { 
            get { return _margin; } 
            set 
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(Margin));
            }
        }

        private int _margin = 8;

        public int Foreground { get; set; } = 0x000000;

        public int Background { get; set; } = 0xFFFFFF;

        public abstract IImage Draw(BitMatrix bitMatrix);
    }
}
