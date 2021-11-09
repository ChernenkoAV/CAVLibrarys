using System.IO;
using System.IO.Compression;

namespace Cav
{
    /// <summary>
    /// Работа с GZip .Net
    /// </summary>
    public static class ExtGZip
    {
        /// <summary>
        /// Gzip сжатие массива байт
        /// </summary>
        /// <param name="sourse"></param>
        /// <returns></returns>
        public static byte[] GZipCompress(this byte[] sourse)
        {
            using (MemoryStream result = new MemoryStream())
            {
                using (GZipStream tstream = new GZipStream(result, CompressionMode.Compress))
                    tstream.Write(sourse, 0, sourse.Length);

                return result.ToArray();
            }
        }

        /// <summary>
        /// Распаковка GZip
        /// </summary>
        /// <param name="sourse"></param>
        /// <returns></returns>
        public static byte[] GZipDecompress(this byte[] sourse)
        {
            using (MemoryStream sms = new MemoryStream(sourse))
            using (GZipStream tstream = new GZipStream(sms, CompressionMode.Decompress))
            using (MemoryStream result = new MemoryStream())
            {
                byte[] buffer = new byte[1024];
                int readBytes = 0;

                do
                {
                    readBytes = tstream.Read(buffer, 0, buffer.Length);
                    result.Write(buffer, 0, readBytes);
                } while (readBytes != 0);

                return result.ToArray();
            }
        }
    }
}
