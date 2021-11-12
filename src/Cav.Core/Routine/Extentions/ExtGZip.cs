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
            if (sourse is null)
                throw new System.ArgumentNullException(nameof(sourse));

            using (var result = new MemoryStream())
            {
                using (var tstream = new GZipStream(result, CompressionMode.Compress))
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
            using (var sms = new MemoryStream(sourse))
            using (var tstream = new GZipStream(sms, CompressionMode.Decompress))
            using (var result = new MemoryStream())
            {
                var buffer = new byte[1024];
                var readBytes = 0;

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
