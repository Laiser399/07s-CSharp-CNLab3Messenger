using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace CNLab3_Messenger
{
    public static class Extensions
    {
        public static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize, int length,
            CancellationToken token, Action<double> progressCallback = null)
        {
            progressCallback?.Invoke(0);

            byte[] buffer = new byte[bufferSize];
            int toRead = length;
            do
            {
                if (token.IsCancellationRequested)
                    break;

                int inBuf = await source.ReadAsync(buffer, 0, Math.Min(bufferSize, toRead));
                await destination.WriteAsync(buffer, 0, inBuf);
                toRead -= inBuf;

                progressCallback?.Invoke((length - toRead) / (double)length);
            } while (toRead > 0);
        }
    }
}
