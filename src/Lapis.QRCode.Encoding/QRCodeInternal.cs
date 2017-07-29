using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lapis.QRCode.Encoding.Internal
{
    // Translate form
    //
    //---------------------------------------------------------------------
    //
    // QR Code Generator for TypeScript
    //
    // Copyright (c) 2015 Kazuhiko Arase
    //
    // URL: http://www.d-project.com/
    //
    // Licensed under the MIT license:
    //  http://www.opensource.org/licenses/mit-license.php
    //
    // The word 'QR Code' is registered trademark of
    // DENSO WAVE INCORPORATED
    //  http://www.denso-wave.com/qrcode/faqpatent-e.html
    //
    //---------------------------------------------------------------------
    class QRCodeInternal
    {

        private const int Pad0 = 0xEC;

        private const int Pad1 = 0x11;

        private int _typeNumber;

        private ErrorCorrectLevel _errorCorrectLevel;

        private bool?[,] _modules;

        private int _moduleCount;

        private byte[] _dataCache;

        private List<QR8BitByte> _dataList;


        public QRCodeInternal(int typeNumber, ErrorCorrectLevel errorCorrectLevel)
        {
            _typeNumber = typeNumber;
            _errorCorrectLevel = errorCorrectLevel;
            _dataList = new List<QR8BitByte>();
        }


        private void MakeImpl(bool test, MaskPattern maskPattern)
        {
            _moduleCount = _typeNumber * 4 + 17;
            _modules = new bool?[_moduleCount, _moduleCount];

            SetupPositionProbePattern(0, 0);
            SetupPositionProbePattern(_moduleCount - 7, 0);
            SetupPositionProbePattern(0, _moduleCount - 7);
            SetupPositionAdjustPattern();
            SetupTimingPattern();
            SetupTypeInfo(test, maskPattern);

            if (_typeNumber >= 7)
                SetupTypeNumber(test);

            if (_dataCache == null)
                _dataCache = CreateData(_typeNumber, _errorCorrectLevel, _dataList);

            MapData(_dataCache, maskPattern);
        }

        private void SetupPositionProbePattern(int row, int col)
        {
            for (var r = -1; r <= 7; r += 1)
            {
                if (row + r <= -1 || _moduleCount <= row + r)
                    continue;
                for (var c = -1; c <= 7; c += 1)
                {
                    if (col + c <= -1 || _moduleCount <= col + c)
                        continue;
                    if ((0 <= r && r <= 6 && (c == 0 || c == 6))
                        || (0 <= c && c <= 6 && (r == 0 || r == 6))
                        || (2 <= r && r <= 4 && 2 <= c && c <= 4))
                        _modules[row + r, col + c] = true;
                    else
                        _modules[row + r, col + c] = false;
                }
            }
        }

        private MaskPattern GetBestMaskPattern()
        {
            var minLostPoint = 0;
            MaskPattern pattern = 0;
            for (var i = 0; i < 8; i += 1)
            {
                MakeImpl(true, (MaskPattern)i);
                var lostPoint = QRUtil.GetLostPoint(this);

                if (i == 0 || minLostPoint > lostPoint)
                {
                    minLostPoint = lostPoint;
                    pattern = (MaskPattern)i;
                }
            }
            return pattern;
        }

        private void SetupTimingPattern()
        {
            for (var r = 8; r < _moduleCount - 8; r += 1)
            {
                if (_modules[r, 6] != null)
                    continue;
                _modules[r, 6] = (r % 2 == 0);
            }
            for (var c = 8; c < _moduleCount - 8; c += 1)
            {
                if (_modules[6, c] != null)
                    continue;
                _modules[6, c] = (c % 2 == 0);
            }
        }

        private void SetupPositionAdjustPattern()
        {
            var pos = QRUtil.GetPatternPosition(_typeNumber);
            for (var i = 0; i < pos.Length; i += 1)
            {
                for (var j = 0; j < pos.Length; j += 1)
                {
                    var row = pos[i];
                    var col = pos[j];
                    if (_modules[row, col] != null)
                        continue;
                    for (var r = -2; r <= 2; r += 1)
                        for (var c = -2; c <= 2; c += 1)
                            if (r == -2 || r == 2 || c == -2 || c == 2
                                || (r == 0 && c == 0))
                                _modules[row + r, col + c] = true;
                            else
                                _modules[row + r, col + c] = false;
                }
            }
        }

        private void SetupTypeNumber(bool test)
        {
            var bits = QRUtil.GetBchTypeNumber(_typeNumber);
            for (var i = 0; i < 18; i += 1)
            {
                var mod = (!test && ((bits >> i) & 1) == 1);
                _modules[i / 3, i % 3 + _moduleCount - 8 - 3] = mod;
            }
            for (var i = 0; i < 18; i += 1)
            {
                var mod = (!test && ((bits >> i) & 1) == 1);
                _modules[i % 3 + _moduleCount - 8 - 3, i / 3] = mod;
            }
        }

        private void SetupTypeInfo(bool test, MaskPattern maskPattern)
        {
            var data = ((int)_errorCorrectLevel << 3) | (int)maskPattern;
            var bits = QRUtil.GetBchTypeInfo(data);

            // vertical
            for (var i = 0; i < 15; i += 1)
            {
                var mod = (!test && ((bits >> i) & 1) == 1);
                if (i < 6)
                    _modules[i, 8] = mod;
                else if (i < 8)
                    _modules[i + 1, 8] = mod;
                else
                    _modules[_moduleCount - 15 + i, 8] = mod;
            }

            // horizontal
            for (var i = 0; i < 15; i += 1)
            {
                var mod = (!test && ((bits >> i) & 1) == 1);
                if (i < 8)
                    _modules[8, _moduleCount - i - 1] = mod;
                else if (i < 9)
                    _modules[8, 15 - i - 1 + 1] = mod;
                else
                    _modules[8, 15 - i - 1] = mod;
            }

            // fixed module
            _modules[_moduleCount - 8, 8] = (!test);
        }

        private void MapData(byte[] data, MaskPattern maskPattern)
        {
            var inc = -1;
            var row = _moduleCount - 1;
            var bitIndex = 7;
            var byteIndex = 0;

            for (var col = _moduleCount - 1; col > 0; col -= 2)
            {
                if (col == 6)
                    col -= 1;

                while (true)
                {
                    for (var c = 0; c < 2; c += 1)
                    {
                        if (_modules[row, col - c] == null)
                        {
                            var dark = false;
                            if (byteIndex < data.Length)
                                dark = (((uint)data[byteIndex] >> bitIndex) & 1) == 1;

                            var mask = QRUtil.GetMask(maskPattern, row, col - c);
                            if (mask)
                                dark = !dark;

                            _modules[row, col - c] = dark;
                            bitIndex -= 1;

                            if (bitIndex == -1)
                            {
                                byteIndex += 1;
                                bitIndex = 7;
                            }
                        }
                    }

                    row += inc;

                    if (row < 0 || _moduleCount <= row)
                    {
                        row -= inc;
                        inc = -inc;
                        break;
                    }
                }
            }
        }

        private byte[] CreateBytes(QRBitBuffer buffer, QRRSBlock[] rsBlocks)
        {
            var offset = 0;

            var maxDcCount = 0;
            var maxEcCount = 0;

            var dcdata = new int[rsBlocks.Length][];
            var ecdata = new int[rsBlocks.Length][];

            for (var r = 0; r < rsBlocks.Length; r += 1)
            {

                var dcCount = rsBlocks[r].DataCount;
                var ecCount = rsBlocks[r].TotalCount - dcCount;

                maxDcCount = Math.Max(maxDcCount, dcCount);
                maxEcCount = Math.Max(maxEcCount, ecCount);

                dcdata[r] = new int[dcCount];

                for (var i = 0; i < dcdata[r].Length; i += 1)
                    dcdata[r][i] = 0xff & buffer.GetByte(i + offset);

                offset += dcCount;

                var rsPoly = QRUtil.GetErrorCorrectPolynomial(ecCount);
                var rawPoly = new QRPolynomial(dcdata[r], rsPoly.Length - 1);

                var modPoly = rawPoly.Mod(rsPoly);
                ecdata[r] = new int[rsPoly.Length - 1];
                for (var i = 0; i < ecdata[r].Length; i += 1)
                {
                    var modIndex = i + modPoly.Length - ecdata[r].Length;
                    ecdata[r][i] = (modIndex >= 0) ? modPoly[modIndex] : 0;
                }
            }

            var totalCodeCount = 0;
            for (var i = 0; i < rsBlocks.Length; i += 1)
                totalCodeCount += rsBlocks[i].TotalCount;


            var data = new byte[totalCodeCount];
            var index = 0;

            for (var i = 0; i < maxDcCount; i += 1)
            {
                for (var r = 0; r < rsBlocks.Length; r += 1)
                {
                    if (i < dcdata[r].Length)
                    {
                        data[index] = (byte)dcdata[r][i];
                        index += 1;
                    }
                }
            }

            for (var i = 0; i < maxEcCount; i += 1)
            {
                for (var r = 0; r < rsBlocks.Length; r += 1)
                {
                    if (i < ecdata[r].Length)
                    {
                        data[index] = (byte)ecdata[r][i];
                        index += 1;
                    }
                }
            }

            return data;
        }

        private byte[] CreateData(int typeNumber, ErrorCorrectLevel errorCorrectLevel, List<QR8BitByte> dataList)
        {
            var rsBlocks = QRRSBlock.GetRSBlocks(typeNumber, errorCorrectLevel);
            var buffer = new QRBitBuffer();

            for (var i = 0; i < dataList.Count; i += 1)
            {
                var data = dataList[i];
                buffer.Put((int)data.Mode, 4);
                buffer.Put(data.Length, QRUtil.GetLengthInBits(data.Mode, typeNumber));
                data.Write(buffer);
            }

            // calc num max data.
            var totalDataCount = 0;
            for (var i = 0; i < rsBlocks.Length; i += 1)
                totalDataCount += rsBlocks[i].DataCount;

            if (buffer.LengthInBits > totalDataCount * 8)
                throw new OverflowException(
                    $"code length overflow. ({buffer.LengthInBits}>{totalDataCount * 8})"
                );

            // end code
            if (buffer.LengthInBits + 4 <= totalDataCount * 8)
                buffer.Put(0, 4);

            // padding
            while (buffer.LengthInBits % 8 != 0)
                buffer.PutBit(false);

            // padding
            while (true)
            {
                if (buffer.LengthInBits >= totalDataCount * 8)
                    break;

                buffer.Put(Pad0, 8);

                if (buffer.LengthInBits >= totalDataCount * 8)
                    break;

                buffer.Put(Pad1, 8);
            }

            return CreateBytes(buffer, rsBlocks);
        }


        public void AddData(string data)
        {
            var newData = new QR8BitByte(data);
            _dataList.Add(newData);
            _dataCache = null;
        }

        public bool IsDark(int row, int col)
        {
            if (row < 0 || _moduleCount <= row || col < 0 || _moduleCount <= col)
                throw new ArgumentOutOfRangeException(row.ToString() + ',' + col);
            return _modules[row, col] == true;
        }

        public int ModuleCount => _moduleCount;

        public void Make() => MakeImpl(false, GetBestMaskPattern());
    }

    static class QRUtil
    {
        private static readonly int[][] PatternPositionTable = new int[][]
        {
             new int[] {},
             new int[] {6, 18},
             new int[] {6, 22},
             new int[] {6, 26},
             new int[] {6, 30},
             new int[] {6, 34},
             new int[] {6, 22, 38},
             new int[] {6, 24, 42},
             new int[] {6, 26, 46},
             new int[] {6, 28, 50},
             new int[] {6, 30, 54},
             new int[] {6, 32, 58},
             new int[] {6, 34, 62},
             new int[] {6, 26, 46, 66},
             new int[] {6, 26, 48, 70},
             new int[] {6, 26, 50, 74},
             new int[] {6, 30, 54, 78},
             new int[] {6, 30, 56, 82},
             new int[] {6, 30, 58, 86},
             new int[] {6, 34, 62, 90},
             new int[] {6, 28, 50, 72, 94},
             new int[] {6, 26, 50, 74, 98},
             new int[] {6, 30, 54, 78, 102},
             new int[] {6, 28, 54, 80, 106},
             new int[] {6, 32, 58, 84, 110},
             new int[] {6, 30, 58, 86, 114},
             new int[] {6, 34, 62, 90, 118},
             new int[] {6, 26, 50, 74, 98, 122},
             new int[] {6, 30, 54, 78, 102, 126},
             new int[] {6, 26, 52, 78, 104, 130},
             new int[] {6, 30, 56, 82, 108, 134},
             new int[] {6, 34, 60, 86, 112, 138},
             new int[] {6, 30, 58, 86, 114, 142},
             new int[] {6, 34, 62, 90, 118, 146},
             new int[] {6, 30, 54, 78, 102, 126, 150},
             new int[] {6, 24, 50, 76, 102, 128, 154},
             new int[] {6, 28, 54, 80, 106, 132, 158},
             new int[] {6, 32, 58, 84, 110, 136, 162},
             new int[] {6, 26, 54, 82, 110, 138, 166},
             new int[] {6, 30, 58, 86, 114, 142, 170}
        };

        private static readonly int G15 = (1 << 10) | (1 << 8) | (1 << 5) | (1 << 4) | (1 << 2) | (1 << 1) | (1 << 0);

        private static readonly int G18 = (1 << 12) | (1 << 11) | (1 << 10) | (1 << 9) | (1 << 8) | (1 << 5) | (1 << 2) | (1 << 0);

        private static readonly int G15Mask = (1 << 14) | (1 << 12) | (1 << 10) | (1 << 4) | (1 << 1);


        private static int GetBchDigit(int data)
        {
            int digit = 0;
            while (data != 0)
            {
                digit += 1;
                data = (int)((uint)data >> 1);
            }
            return digit;
        }

        public static int GetBchTypeInfo(int data)
        {
            int d = data << 10;
            while (GetBchDigit(d) - GetBchDigit(G15) >= 0)
            {
                d ^= (G15 << (GetBchDigit(d) - GetBchDigit(G15)));
            }
            return ((data << 10) | d) ^ G15Mask;
        }

        public static int GetBchTypeNumber(int data)
        {
            int d = data << 12;
            while (GetBchDigit(d) - GetBchDigit(G18) >= 0)
            {
                d ^= (G18 << (GetBchDigit(d) - GetBchDigit(G18)));
            }
            return (data << 12) | d;
        }

        public static int[] GetPatternPosition(int typeNumber)
        {
            return PatternPositionTable[typeNumber - 1];
        }

        public static bool GetMask(MaskPattern maskPattern, int i, int j)
        {
            switch (maskPattern)
            {
                case MaskPattern.Pattern000:
                    return (i + j) % 2 == 0;
                case MaskPattern.Pattern001:
                    return i % 2 == 0;
                case MaskPattern.Pattern010:
                    return j % 3 == 0;
                case MaskPattern.Pattern011:
                    return (i + j) % 3 == 0;
                case MaskPattern.Pattern100:
                    return (i / 2 + j / 3) % 2 == 0;
                case MaskPattern.Pattern101:
                    return (i * j) % 2 + (i * j) % 3 == 0;
                case MaskPattern.Pattern110:
                    return ((i * j) % 2 + (i * j) % 3) % 2 == 0;
                case MaskPattern.Pattern111:
                    return ((i * j) % 3 + (i + j) % 2) % 2 == 0;
                default:
                    throw new ArgumentException("bad maskPattern:" + maskPattern);
            }
        }

        public static QRPolynomial GetErrorCorrectPolynomial(int errorCorrectLength)
        {
            var a = new QRPolynomial(new int[] { 1 }, 0);
            for (int i = 0; i < errorCorrectLength; i++)
            {
                a = a.Multiply(new QRPolynomial(new int[] { 1, QRMath.GExp(i) }, 0));
            }
            return a;
        }

        public static int GetLengthInBits(Mode mode, int type)
        {
            if (1 <= type && type < 10)
            {
                switch (mode)
                {
                    case Mode.Number:
                        return 10;
                    case Mode.AlphaNum:
                        return 9;
                    case Mode.Byte8Bit:
                        return 8;
                    case Mode.Kanji:
                        return 8;
                    default:
                        throw new ArgumentException("mode:" + mode);
                }
            }
            else if (type < 27)
            {
                switch (mode)
                {
                    case Mode.Number:
                        return 12;
                    case Mode.AlphaNum:
                        return 11;
                    case Mode.Byte8Bit:
                        return 16;
                    case Mode.Kanji:
                        return 10;
                    default:
                        throw new ArgumentException("mode:" + mode);
                }
            }
            else if (type < 41)
            {
                switch (mode)
                {
                    case Mode.Number:
                        return 14;
                    case Mode.AlphaNum:
                        return 13;
                    case Mode.Byte8Bit:
                        return 16;
                    case Mode.Kanji:
                        return 12;
                    default:
                        throw new ArgumentException("mode:" + mode);
                }
            }
            else
            {
                throw new ArgumentException("type:" + type);
            }
        }

        public static int GetLostPoint(QRCodeInternal qrCode)
        {
            int moduleCount = qrCode.ModuleCount;
            int lostPoint = 0;

            // LEVEL1
            for (var row = 0; row < moduleCount; row += 1)
            {
                for (var col = 0; col < moduleCount; col += 1)
                {
                    var sameCount = 0;
                    var dark = qrCode.IsDark(row, col);
                    for (var r = -1; r <= 1; r += 1)
                    {
                        if (row + r < 0 || moduleCount <= row + r)
                            continue;
                        for (var c = -1; c <= 1; c += 1)
                        {
                            if (col + c < 0 || moduleCount <= col + c)
                                continue;
                            if (r == 0 && c == 0)
                                continue;
                            if (dark == qrCode.IsDark(row + r, col + c))
                                sameCount += 1;
                        }
                    }
                    if (sameCount > 5)
                        lostPoint += (3 + sameCount - 5);
                }
            }

            // LEVEL2
            for (var row = 0; row < moduleCount - 1; row += 1)
            {
                for (var col = 0; col < moduleCount - 1; col += 1)
                {
                    var count = 0;
                    if (qrCode.IsDark(row, col))
                        count += 1;
                    if (qrCode.IsDark(row + 1, col))
                        count += 1;
                    if (qrCode.IsDark(row, col + 1))
                        count += 1;
                    if (qrCode.IsDark(row + 1, col + 1))
                        count += 1;
                    if (count == 0 || count == 4)
                        lostPoint += 3;
                }
            }

            // LEVEL3
            for (var row = 0; row < moduleCount; row += 1)
            {
                for (var col = 0; col < moduleCount - 6; col += 1)
                {
                    if (qrCode.IsDark(row, col)
                        && !qrCode.IsDark(row, col + 1)
                        && qrCode.IsDark(row, col + 2)
                        && qrCode.IsDark(row, col + 3)
                        && qrCode.IsDark(row, col + 4)
                        && !qrCode.IsDark(row, col + 5)
                        && qrCode.IsDark(row, col + 6))
                        lostPoint += 40;
                }
            }
            for (var col = 0; col < moduleCount; col += 1)
            {
                for (var row = 0; row < moduleCount - 6; row += 1)
                {
                    if (qrCode.IsDark(row, col)
                        && !qrCode.IsDark(row + 1, col)
                        && qrCode.IsDark(row + 2, col)
                        && qrCode.IsDark(row + 3, col)
                        && qrCode.IsDark(row + 4, col)
                        && !qrCode.IsDark(row + 5, col)
                        && qrCode.IsDark(row + 6, col))
                        lostPoint += 40;
                }
            }

            // LEVEL4
            var darkCount = 0;
            for (var col = 0; col < moduleCount; col += 1)
                for (var row = 0; row < moduleCount; row += 1)
                    if (qrCode.IsDark(row, col))
                        darkCount += 1;


            var ratio = Math.Abs(100 * darkCount / moduleCount / moduleCount - 50) / 5;
            lostPoint += ratio * 10;
            return lostPoint;
        }

    }

    static class QRMath
    {
        private static readonly int[] ExpTable;

        private static readonly int[] LogTable;

        static QRMath()
        {
            ExpTable = new int[256];
            for (int i = 0; i < 8; i++)
                ExpTable[i] = 1 << i;

            for (int i = 8; i < 256; i++)
                ExpTable[i] = ExpTable[i - 4] ^
                    ExpTable[i - 5] ^
                    ExpTable[i - 6] ^
                    ExpTable[i - 8];

            LogTable = new int[256];
            for (int i = 0; i < 255; i++)
                LogTable[ExpTable[i]] = i;
        }

        public static int GLog(int n)
        {
            if (n < 1)
                throw new ArithmeticException("glog(" + n + ")");
            return LogTable[n];
        }

        public static int GExp(int n)
        {
            while (n < 0)
                n += 255;
            while (n >= 256)
                n -= 255;
            return ExpTable[n];
        }
    }

    class QRPolynomial
    {
        private readonly int[] _num;

        public QRPolynomial(int[] num, int shift)
        {
            int offset = 0;
            while (offset < num.Length && num[offset] == 0)
                offset++;
            _num = new int[num.Length - offset + shift];
            for (var i = 0; i < num.Length - offset; i += 1)
                _num[i] = num[i + offset];
        }

        public int this[int index] => _num[index];

        public int Length => _num.Length;

        public QRPolynomial Multiply(QRPolynomial other)
        {
            var num = new int[Length + other.Length - 1];
            for (int i = 0; i < Length; i++)
                for (int j = 0; j < other.Length; j++)
                    num[i + j] ^= QRMath.GExp(QRMath.GLog(this[i]) + QRMath.GLog(other[j]));

            return new QRPolynomial(num, 0);
        }

        public QRPolynomial Mod(QRPolynomial other)
        {
            if (Length - other.Length < 0)
                return this;

            var ratio = QRMath.GLog(this[0]) - QRMath.GLog(other[0]);

            var num = new int[Length];
            Array.Copy(_num, num, Length);
            for (int i = 0; i < other.Length; i++)
                num[i] ^= QRMath.GExp(QRMath.GLog(other[i]) + ratio);

            return new QRPolynomial(num, 0).Mod(other);
        }
    }

    class QRRSBlock
    {
        private static readonly int[][] RsBlockTable = new int[][]
        {
            // L
            // M
            // Q
            // H

            // 1
            new int[] {1, 26, 19},
            new int[] {1, 26, 16},
            new int[] {1, 26, 13},
            new int[] {1, 26, 9},

            // 2
            new int[] {1, 44, 34},
            new int[] {1, 44, 28},
            new int[] {1, 44, 22},
            new int[] {1, 44, 16},

            // 3
            new int[] {1, 70, 55},
            new int[] {1, 70, 44},
            new int[] {2, 35, 17},
            new int[] {2, 35, 13},

            // 4
            new int[] {1, 100, 80},
            new int[] {2, 50, 32},
            new int[] {2, 50, 24},
            new int[] {4, 25, 9},

            // 5
            new int[] {1, 134, 108},
            new int[] {2, 67, 43},
            new int[] {2, 33, 15, 2, 34, 16},
            new int[] {2, 33, 11, 2, 34, 12},

            // 6
            new int[] {2, 86, 68},
            new int[] {4, 43, 27},
            new int[] {4, 43, 19},
            new int[] {4, 43, 15},

            // 7
            new int[] {2, 98, 78},
            new int[] {4, 49, 31},
            new int[] {2, 32, 14, 4, 33, 15},
            new int[] {4, 39, 13, 1, 40, 14},

            // 8
            new int[] {2, 121, 97},
            new int[] {2, 60, 38, 2, 61, 39},
            new int[] {4, 40, 18, 2, 41, 19},
            new int[] {4, 40, 14, 2, 41, 15},

            // 9
            new int[] {2, 146, 116},
            new int[] {3, 58, 36, 2, 59, 37},
            new int[] {4, 36, 16, 4, 37, 17},
            new int[] {4, 36, 12, 4, 37, 13},

            // 10
            new int[] {2, 86, 68, 2, 87, 69},
            new int[] {4, 69, 43, 1, 70, 44},
            new int[] {6, 43, 19, 2, 44, 20},
            new int[] {6, 43, 15, 2, 44, 16},

            // 11
            new int[]{4, 101, 81},
            new int[]{1, 80, 50, 4, 81, 51},
            new int[]{4, 50, 22, 4, 51, 23},
            new int[]{3, 36, 12, 8, 37, 13},

            // 12
            new int[]{2, 116, 92, 2, 117, 93},
            new int[]{6, 58, 36, 2, 59, 37},
            new int[]{4, 46, 20, 6, 47, 21},
            new int[]{7, 42, 14, 4, 43, 15},

            // 13
            new int[]{4, 133, 107},
            new int[]{8, 59, 37, 1, 60, 38},
            new int[]{8, 44, 20, 4, 45, 21},
            new int[]{12, 33, 11, 4, 34, 12},

            // 14
            new int[]{3, 145, 115, 1, 146, 116},
            new int[]{4, 64, 40, 5, 65, 41},
            new int[]{11, 36, 16, 5, 37, 17},
            new int[]{11, 36, 12, 5, 37, 13},

            // 15
            new int[]{5, 109, 87, 1, 110, 88},
            new int[]{5, 65, 41, 5, 66, 42},
            new int[]{5, 54, 24, 7, 55, 25},
            new int[]{11, 36, 12, 7, 37, 13},

            // 16
            new int[]{5, 122, 98, 1, 123, 99},
            new int[]{7, 73, 45, 3, 74, 46},
            new int[]{15, 43, 19, 2, 44, 20},
            new int[]{3, 45, 15, 13, 46, 16},

            // 17
            new int[]{1, 135, 107, 5, 136, 108},
            new int[]{10, 74, 46, 1, 75, 47},
            new int[]{1, 50, 22, 15, 51, 23},
            new int[]{2, 42, 14, 17, 43, 15},

            // 18
            new int[]{5, 150, 120, 1, 151, 121},
            new int[]{9, 69, 43, 4, 70, 44},
            new int[]{17, 50, 22, 1, 51, 23},
            new int[]{2, 42, 14, 19, 43, 15},

            // 19
            new int[]{3, 141, 113, 4, 142, 114},
            new int[]{3, 70, 44, 11, 71, 45},
            new int[]{17, 47, 21, 4, 48, 22},
            new int[]{9, 39, 13, 16, 40, 14},

            // 20
            new int[]{3, 135, 107, 5, 136, 108},
            new int[]{3, 67, 41, 13, 68, 42},
            new int[]{15, 54, 24, 5, 55, 25},
            new int[]{15, 43, 15, 10, 44, 16},

            // 21
            new int[]{4, 144, 116, 4, 145, 117},
            new int[]{17, 68, 42},
            new int[]{17, 50, 22, 6, 51, 23},
            new int[]{19, 46, 16, 6, 47, 17},

            // 22
            new int[]{2, 139, 111, 7, 140, 112},
            new int[]{17, 74, 46},
            new int[]{7, 54, 24, 16, 55, 25},
            new int[]{34, 37, 13},

            // 23
            new int[]{4, 151, 121, 5, 152, 122},
            new int[]{4, 75, 47, 14, 76, 48},
            new int[]{11, 54, 24, 14, 55, 25},
            new int[]{16, 45, 15, 14, 46, 16},

            // 24
            new int[]{6, 147, 117, 4, 148, 118},
            new int[]{6, 73, 45, 14, 74, 46},
            new int[]{11, 54, 24, 16, 55, 25},
            new int[]{30, 46, 16, 2, 47, 17},

            // 25
            new int[]{8, 132, 106, 4, 133, 107},
            new int[]{8, 75, 47, 13, 76, 48},
            new int[]{7, 54, 24, 22, 55, 25},
            new int[]{22, 45, 15, 13, 46, 16},

            // 26
            new int[]{10, 142, 114, 2, 143, 115},
            new int[]{19, 74, 46, 4, 75, 47},
            new int[]{28, 50, 22, 6, 51, 23},
            new int[]{33, 46, 16, 4, 47, 17},

            // 27
            new int[]{8, 152, 122, 4, 153, 123},
            new int[]{22, 73, 45, 3, 74, 46},
            new int[]{8, 53, 23, 26, 54, 24},
            new int[]{12, 45, 15, 28, 46, 16},

            // 28
            new int[]{3, 147, 117, 10, 148, 118},
            new int[]{3, 73, 45, 23, 74, 46},
            new int[]{4, 54, 24, 31, 55, 25},
            new int[]{11, 45, 15, 31, 46, 16},

            // 29
            new int[]{7, 146, 116, 7, 147, 117},
            new int[]{21, 73, 45, 7, 74, 46},
            new int[]{1, 53, 23, 37, 54, 24},
            new int[]{19, 45, 15, 26, 46, 16},

            // 30
            new int[]{5, 145, 115, 10, 146, 116},
            new int[]{19, 75, 47, 10, 76, 48},
            new int[]{15, 54, 24, 25, 55, 25},
            new int[]{23, 45, 15, 25, 46, 16},

            // 31
            new int[]{13, 145, 115, 3, 146, 116},
            new int[]{2, 74, 46, 29, 75, 47},
            new int[]{42, 54, 24, 1, 55, 25},
            new int[]{23, 45, 15, 28, 46, 16},

            // 32
            new int[]{17, 145, 115},
            new int[]{10, 74, 46, 23, 75, 47},
            new int[]{10, 54, 24, 35, 55, 25},
            new int[]{19, 45, 15, 35, 46, 16},

            // 33
            new int[]{17, 145, 115, 1, 146, 116},
            new int[]{14, 74, 46, 21, 75, 47},
            new int[]{29, 54, 24, 19, 55, 25},
            new int[]{11, 45, 15, 46, 46, 16},

            // 34
            new int[]{13, 145, 115, 6, 146, 116},
            new int[]{14, 74, 46, 23, 75, 47},
            new int[]{44, 54, 24, 7, 55, 25},
            new int[]{59, 46, 16, 1, 47, 17},

            // 35
            new int[]{12, 151, 121, 7, 152, 122},
            new int[]{12, 75, 47, 26, 76, 48},
            new int[]{39, 54, 24, 14, 55, 25},
            new int[]{22, 45, 15, 41, 46, 16},

            // 36
            new int[]{6, 151, 121, 14, 152, 122},
            new int[]{6, 75, 47, 34, 76, 48},
            new int[]{46, 54, 24, 10, 55, 25},
            new int[]{2, 45, 15, 64, 46, 16},

            // 37
            new int[]{17, 152, 122, 4, 153, 123},
            new int[]{29, 74, 46, 14, 75, 47},
            new int[]{49, 54, 24, 10, 55, 25},
            new int[]{24, 45, 15, 46, 46, 16},

            // 38
            new int[]{4, 152, 122, 18, 153, 123},
            new int[]{13, 74, 46, 32, 75, 47},
            new int[]{48, 54, 24, 14, 55, 25},
            new int[]{42, 45, 15, 32, 46, 16},

            // 39
            new int[]{20, 147, 117, 4, 148, 118},
            new int[]{40, 75, 47, 7, 76, 48},
            new int[]{43, 54, 24, 22, 55, 25},
            new int[]{10, 45, 15, 67, 46, 16},

            // 40
            new int[]{19, 148, 118, 6, 149, 119},
            new int[]{18, 75, 47, 31, 76, 48},
            new int[]{34, 54, 24, 34, 55, 25},
            new int[]{20, 45, 15, 61, 46, 16}
        };

        public int TotalCount { get; }

        public int DataCount { get; }

        private QRRSBlock(int totalCount, int dataCount)
        {
            TotalCount = totalCount;
            DataCount = dataCount;
        }

        private static int[] GetRsBlockTable(int typeNumber, ErrorCorrectLevel errorCorrectLevel)
        {
            switch (errorCorrectLevel)
            {
                case ErrorCorrectLevel.L:
                    return RsBlockTable[(typeNumber - 1) * 4 + 0];
                case ErrorCorrectLevel.M:
                    return RsBlockTable[(typeNumber - 1) * 4 + 1];
                case ErrorCorrectLevel.Q:
                    return RsBlockTable[(typeNumber - 1) * 4 + 2];
                case ErrorCorrectLevel.H:
                    return RsBlockTable[(typeNumber - 1) * 4 + 3];
                default:
                    throw new ArgumentException("bad rs block @ typeNumber:" + typeNumber +
                        "/errorCorrectLevel:" + errorCorrectLevel);
            }
        }

        public static QRRSBlock[] GetRSBlocks(int typeNumber, ErrorCorrectLevel errorCorrectLevel)
        {
            int[] rsBlock = GetRsBlockTable(typeNumber, errorCorrectLevel);

            int length = rsBlock.Length / 3;

            var list = new List<QRRSBlock>();
            for (int i = 0; i < length; i++)
            {
                int count = rsBlock[i * 3 + 0];
                int totalCount = rsBlock[i * 3 + 1];
                int dataCount = rsBlock[i * 3 + 2];
                for (int j = 0; j < count; j++)
                    list.Add(new QRRSBlock(totalCount, dataCount));
            }
            return list.ToArray();
        }
    }

    class QRBitBuffer
    {

        private readonly List<byte> _buffer = new List<byte>();

        private int _length;

        public QRBitBuffer() { }

        public byte GetByte(int index) => _buffer[index];

        public bool this[int index]
        {
            get
            {
                var bufIndex = index / 8;
                return (((uint)_buffer[bufIndex] >> (7 - index % 8)) & 1) == 1;
            }
        }

        public void Put(int num, int length)
        {
            for (var i = 0; i < length; i += 1)            
                PutBit((((uint)num >> (length - i - 1)) & 1) == 1);            
        }

        public int LengthInBits => _length;

        public void PutBit(bool bit)
        {
            var bufIndex = _length / 8;
            if (_buffer.Count <= bufIndex)            
                _buffer.Add(0);            
            if (bit)            
                _buffer[bufIndex] |= (byte)((uint)0x80 >> (_length % 8));
            
            _length += 1;
        }
    }

    enum MaskPattern
    {
        Pattern000 = 0,
        Pattern001 = 1,
        Pattern010 = 2,
        Pattern011 = 3,
        Pattern100 = 4,
        Pattern101 = 5,
        Pattern110 = 6,
        Pattern111 = 7
    }

    enum Mode
    {
        Number = 1 << 0,
        AlphaNum = 1 << 1,
        Byte8Bit = 1 << 2,
        Kanji = 1 << 3
    }

    class QR8BitByte
    {
        public Mode Mode => Mode.Byte8Bit;

        public string Data { get; }

        private readonly byte[] _bytes;

        public QR8BitByte(string data)
        {
            Data = data;
            _bytes = System.Text.Encoding.UTF8.GetBytes(data);
        }

        public int Length => _bytes.Length;

        public void Write(QRBitBuffer buffer)
        {
            for (var i = 0; i < _bytes.Length; i += 1)            
                buffer.Put(_bytes[i], 8);            
        }
    }
}
