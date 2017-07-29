using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lapis.QRCode.Encoding
{
    public class BitMatrix
    {

        public BitMatrix(int rowCount, int columnCount)
        {
            if (rowCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(rowCount));
            if (columnCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(columnCount));
            _values = new bool[rowCount, columnCount];
        }

        public BitMatrix(int rowCount, int columnCount, bool value)
            : this(rowCount, columnCount)
        {
            SetAll(value);
        }

        public int RowCount => _values.GetLength(0);

        public int ColumnCount => _values.GetLength(1);

        public bool this[int row, int column]
        {
            get { return _values[row, column]; }
            set { _values[row, column] = value; }
        }

        public void SetAll(bool value)
        {
            for (int r = 0; r < RowCount; r++)
                for (int c = 0; c < ColumnCount; c++)
                    _values[r, c] = value;
        }

        public void Fill(int rowStart, int columnStart, int rowLength, int columnLength, bool value)
        {
            for (var r = rowStart; r < rowStart + rowLength && r < RowCount; r++)
            {
                for (var c = columnStart; c < columnStart + columnLength && c < ColumnCount; c++)
                    this[r, c] = value;
            }
        }
                

        public void CopyTo(BitMatrix other)
        {
            for (int r = 0; r < RowCount && r < other.RowCount; r++)
                for (int c = 0; c < ColumnCount && c < other.ColumnCount; c++)
                    other._values[r, c] = _values[r, c];
        }

        private readonly bool[,] _values;


    }

    public class BitSquare : BitMatrix
    {
        public BitSquare(int size)
            : base(size, size)
        { }

        public int Size => RowCount;

    }
}
