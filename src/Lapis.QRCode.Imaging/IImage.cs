using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lapis.QRCode.Imaging
{
    public interface IImage
    {        
        void Save(Stream stream);
    }
}
