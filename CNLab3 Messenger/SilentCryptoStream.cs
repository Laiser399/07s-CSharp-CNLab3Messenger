using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CNLab3_Messenger
{
    public class SilentCryptoStream : CryptoStream
    {
        private readonly Stream _stream;

        public SilentCryptoStream(Stream stream, ICryptoTransform transform, CryptoStreamMode mode)
            : base(stream, transform, mode)
        {
            _stream = stream;
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                base.Dispose(disposing);
            }
            catch (CryptographicException)
            {
                if (disposing)
                {
                    _stream.Dispose();
                }
            }
        }
    }
}
