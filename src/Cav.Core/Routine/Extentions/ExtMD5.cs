using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Cav
{
    /// <summary>
    /// Вычисления MD5
    /// </summary>
    public static class ExtMD5
    {
        /// <summary>
        /// Вычисление MD5-хеша для потока
        /// </summary>
        /// <param name="inputData">Поток</param>
        /// <returns>Хеш, перобразованный к Guid</returns>
        public static Guid ComputeMD5Checksum(this Stream inputData)
        {
            if (inputData == null)
                throw new ArgumentNullException($"{nameof(ComputeMD5ChecksumString)}:{nameof(inputData)}");
            MD5 md5 = new MD5CryptoServiceProvider();
            return new Guid(md5.ComputeHash(inputData));
        }

        /// <summary>
        /// Вычисление MD5-хеша массива байт
        /// </summary>
        /// <param name="inputData">Массив байт</param>
        /// <returns>Хеш, перобразованный к Guid</returns>
        public static Guid ComputeMD5Checksum(this byte[] inputData)
        {
            if (inputData == null)
                throw new ArgumentNullException($"{nameof(ComputeMD5ChecksumString)}:{nameof(inputData)}");
            using (MemoryStream ms = new MemoryStream(inputData))
                return ComputeMD5Checksum(ms);
        }

        /// <summary>
        /// Вычисление MD5-хеша файла
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns>Хеш, перобразованный к Guid</returns>
        public static Guid ComputeMD5ChecksumFile(this string filePath)
        {
            if (filePath.IsNullOrWhiteSpace())
                throw new ArgumentNullException($"{nameof(ComputeMD5ChecksumString)}:{nameof(filePath)}");

            using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                return ComputeMD5Checksum(fs);
        }

        /// <summary>
        /// Вычисление MD5-хеша строки. Байты берутся UTF8.
        /// </summary>
        /// <param name="str">Входная строка</param>
        /// <returns>Хеш, перобразованный к Guid</returns>
        public static Guid ComputeMD5ChecksumString(this string str)
        {
            if (str.IsNullOrWhiteSpace())
                throw new ArgumentNullException($"{nameof(ComputeMD5ChecksumString)}:{nameof(str)}");

            return Encoding.UTF8.GetBytes(str).ComputeMD5Checksum();
        }
    }
}
