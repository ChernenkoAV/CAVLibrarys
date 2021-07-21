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

            byte[] keyByte = Encoding.UTF8.GetBytes(key).ComputeMD5Checksum().ToByteArray();
            byte[] data = Encoding.UTF8.GetBytes(obj.JsonSerialize()).GZipCompress();

            var aes = new AesCryptoServiceProvider();
            aes.Key = keyByte;
            aes.IV = keyByte;

            using (ICryptoTransform crtr = aes.CreateEncryptor())
            using (var memres = new MemoryStream())
            using (var crstr = new CryptoStream(memres, crtr, CryptoStreamMode.Write))
            {
                crstr.Write(data, 0, data.Length);
                crstr.FlushFinalBlock();
                data = memres.ToArray();
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
                return default(T);

            byte[] keyByte = Encoding.UTF8.GetBytes(key).ComputeMD5Checksum().ToByteArray();

            var aes = new AesCryptoServiceProvider();
            aes.Key = keyByte;
            aes.IV = keyByte;

            using (ICryptoTransform crtr = aes.CreateDecryptor())
            using (var memres = new MemoryStream())
            using (var crstr = new CryptoStream(memres, crtr, CryptoStreamMode.Write))
            {
                crstr.Write(data, 0, data.Length);
                crstr.FlushFinalBlock();
                data = memres.ToArray();
            }

            var strJson = Encoding.UTF8.GetString(data.GZipDecompress());

            T res = default(T);

            try
            {
                res = strJson.JsonDeserealize<T>();
            }
            catch
            {
                res = strJson.JSONDeserialize<T>();
            }

            return res;
        }
    }
}
