using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Cav
{
    /// <summary>
    /// Сериализация-десериализация и шифрование Aes
    /// </summary>
    public static class ExtAes
    {
        /// <summary>
        /// Сериализация объекта и шифрование алгоритмом AES
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <param name="key">Ключ шифрования</param>
        /// <returns>Зашифрованный объект</returns>
        public static byte[] SerializeAesEncrypt(this Object obj, String key)
        {
            if (obj == null)
                return null;

            var keyByte = key.ComputeMD5ChecksumString().ToByteArray();
            var ivByte = keyByte.ComputeMD5Checksum().ToByteArray();
            var data = Encoding.UTF8.GetBytes(obj.JsonSerialize()).GZipCompress();

            using (var aes = Aes.Create())
            {
#pragma warning disable CA5401 // Не используйте CreateEncryptor с вектором инициализации, отличным от значения по умолчанию.
                using (var crtr = aes.CreateEncryptor(keyByte, ivByte))
#pragma warning restore CA5401 // Не используйте CreateEncryptor с вектором инициализации, отличным от значения по умолчанию.
                using (var memres = new MemoryStream())
                using (var crstr = new CryptoStream(memres, crtr, CryptoStreamMode.Write))
                {
                    crstr.Write(data, 0, data.Length);
                    crstr.FlushFinalBlock();
                    data = memres.ToArray();
                }

                aes.Clear();
            }

            return data;
        }

        /// <summary>
        /// Дешифрация алгоритмом AES и десереализация объекта из массива шифра. Работает только после <see cref="SerializeAesEncrypt(object, string)"/>
        /// , так как с ключом производятся манипуляции
        /// </summary>
        /// <typeparam name="T">Тип для десериализации</typeparam>
        /// <param name="data">Массив шифрованных данных</param>
        /// <param name="key">Ключь шифрования</param>
        /// <returns></returns>
        public static T DeserializeAesDecrypt<T>(this byte[] data, String key)
        {
            if (data == null)
                return default;

            var keyByte = key.ComputeMD5ChecksumString().ToByteArray();
            var ivByte = keyByte.ComputeMD5Checksum().ToByteArray();

            using (var aes = Aes.Create())
            {
                using (var crtr = aes.CreateDecryptor(keyByte, ivByte))
                using (var memres = new MemoryStream())
                using (var crstr = new CryptoStream(memres, crtr, CryptoStreamMode.Write))
                {
                    crstr.Write(data, 0, data.Length);
                    crstr.FlushFinalBlock();
                    data = memres.ToArray();
                }

                aes.Clear();
            }

            var strJson = Encoding.UTF8.GetString(data.GZipDecompress());

            return strJson.JsonDeserealize<T>();

        }
    }
}
