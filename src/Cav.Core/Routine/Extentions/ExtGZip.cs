using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Cav
{
    /// <summary>
    /// Работа с GZip и ZipArchive
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

        /// <summary>
        /// Упаковать файлы в архив (в памяти). Работа с ZipArchive - все правила и ошибки его
        /// </summary>
        /// <param name="files">Коллекция "относительный путь в архиве"-"тело файла". Относительный путь - entryName в <see cref="ZipArchive.CreateEntry(string)"/></param>
        /// <returns>Тело архива, Если <paramref name="files"/> null или пуст - возвращается null </returns>

        public static byte[] ZipArhiveCompress(this Dictionary<string, byte[]> files)
        {
            if (files == null || !files.Any())
                return null;

            var memoryStream = new MemoryStream(); // для него юзинга нет, так как его закроет ZipArchive

            using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create))
            {
                foreach (var f in files)
                {
                    var fileEntry = zipArchive.CreateEntry(f.Key);
                    using (var writer = new BinaryWriter(fileEntry.Open()))
                        writer.Write(f.Value);
                }
            }

            return memoryStream.ToArray();
        }

        /// <summary>
        /// Распаковать архив (в памяти). Работа с ZipArchive - все правила и ошибки его
        /// </summary>
        /// <param name="arc">Тело архива</param>
        /// <returns>Коллекция "относительный путь в архиве"-"тело файла". Относительный путь - <see cref="ZipArchiveEntry.FullName"/> </returns>
        public static IEnumerable<KeyValuePair<string, byte[]>> ZipArhiveDecompress(this byte[] arc)
        {
            if (arc == null || !arc.Any())
                return null;

            var res = new List<KeyValuePair<string, byte[]>>();

            using (var memoryStream = new MemoryStream(arc))
            using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Read))
            {
                foreach (var zipArchiveEntry in zipArchive.Entries)
                {
                    byte[] body = null;
                    using (var reader = new MemoryStream())
                    using (var sStream = zipArchiveEntry.Open())
                    {
                        sStream.CopyTo(reader);
                        body = reader.ToArray();
                    }

                    res.Add(new KeyValuePair<string, byte[]>(zipArchiveEntry.FullName, body));
                }
            }

            return res;
        }
    }
}
