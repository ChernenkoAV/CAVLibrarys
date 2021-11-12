﻿using System;
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
                throw new ArgumentException($"{nameof(ComputeMD5ChecksumString)}:{nameof(inputData)}");

#pragma warning disable CA5351 // Не используйте взломанные алгоритмы шифрования
            using (var md5 = MD5.Create())
#pragma warning restore CA5351 // Не используйте взломанные алгоритмы шифрования
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
                throw new ArgumentException($"{nameof(ComputeMD5ChecksumString)}:{nameof(inputData)}");

            using (var ms = new MemoryStream(inputData))
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
                throw new ArgumentException($"{nameof(ComputeMD5ChecksumString)}:{nameof(filePath)}");

            using (var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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
                throw new ArgumentException($"{nameof(ComputeMD5ChecksumString)}:{nameof(str)}");

            return Encoding.UTF8.GetBytes(str).ComputeMD5Checksum();
        }
    }
}
