using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lapis.QRCode.Encoding.Internal;

namespace Lapis.QRCode.Encoding
{
    public interface IQRCodeEncoder
    {
        int TypeNumber { get; set; }

        ErrorCorrectLevel ErrorCorrectLevel { get; set; }

        BitSquare Build(string data);
    }

    public class QRCodeEncoder : IQRCodeEncoder
    {
        public QRCodeEncoder() { }

        public int TypeNumber
        {
            get { return _typeNumber; }
            set
            {
                if (value <= 0 || value > 40)
                    throw new ArgumentOutOfRangeException(nameof(TypeNumber));
                _typeNumber = value;
            }
        }

        public ErrorCorrectLevel ErrorCorrectLevel { get; set; }

        public BitSquare Build(string data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var qr = new QRCodeInternal(TypeNumber, ErrorCorrectLevel);
            qr.AddData(data);
            qr.Make();

            var moduleCount = qr.ModuleCount;
            var bits = new BitSquare(moduleCount);
            for (var r = 0; r < moduleCount; r++)
                for (var c = 0; c < moduleCount; c++)
                    bits[r, c] = qr.IsDark(r, c);

            return bits;
        }


        private int _typeNumber = 4;

        private string _data = string.Empty;
    }
}
