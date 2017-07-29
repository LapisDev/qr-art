using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lapis.QRCode.Encoding.Internal;

namespace Lapis.QRCode.Encoding
{
    public static class QRCodeHelper
    {
        public static int GetModuleCount(int typeNumber)
        {
            if (typeNumber <= 0 || typeNumber > 40)
                throw new ArgumentOutOfRangeException(nameof(typeNumber));
            return typeNumber * 4 + 17;
        }

        public static bool IsPositionProbePattern(int typeNumber, int row, int column)
        {
            var moduleCount = GetModuleCount(typeNumber);
            if (row < 0 || row >= moduleCount)
                throw new ArgumentOutOfRangeException(nameof(row));
            if (column < 0 || column >= moduleCount)
                throw new ArgumentOutOfRangeException(nameof(column));

            var result = (row <= 7 && column <= 7) ||
                (row <= 7 && moduleCount - column <= 7) ||
                (moduleCount - row <= 7 && column <= 7);
            return result;
        }

        public static bool IsPositionAdjustPattern(int typeNumber, int row, int column)
        {
            var moduleCount = GetModuleCount(typeNumber);
            if (IsPositionProbePattern(typeNumber, row, column))
                return false;

            var pos = QRUtil.GetPatternPosition(typeNumber);
            var result = (from x in pos
                          from y in pos
                          where x - 2 <= column && x + 2 >= column && y - 2 <= row && y + 2 >= row
                          where !IsPositionProbePattern(typeNumber, x, y)
                          select true)
                          .Any();
            return result;
        }
    }
}
