﻿using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Cav.DigitalSignature
{
    /// <summary>
    /// Общие механизмы, не зависящие от провайдера криптографии
    /// </summary>
    public static class DSGeneric
    {
        /// <summary>
        /// Получение сертификата по отпечатоку или из строки. (+ невалидные)
        /// </summary>
        /// <param name="thumbprintOrBase64Cert">Отперчаток или сертификат в BASE64</param>
        /// <param name="localMachine">Хранилище. null - смотреть везде, true - локальный компьютер, false - пользователь</param>
        /// <returns></returns>
        public static X509Certificate2 FindCertByThumbprint(String thumbprintOrBase64Cert, Boolean? localMachine = null)
        {
            if (thumbprintOrBase64Cert.IsNullOrWhiteSpace())
                return null;

            thumbprintOrBase64Cert = new String(thumbprintOrBase64Cert.ToCharArray().Where(x => Char.IsLetterOrDigit(x) || x.In('+', '/', '=')).ToArray());

            X509Certificate2 cert = null;

            try
            {
                cert = new X509Certificate2(Convert.FromBase64String(thumbprintOrBase64Cert));
                return cert;
            }
            catch
            {
            }

            if (!localMachine.HasValue || localMachine.Value)
            {
                using (var store = new X509Store(StoreLocation.LocalMachine))
                {
                    store.Open(OpenFlags.ReadOnly);
                    var cc = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprintOrBase64Cert, false);
                    if (cc.Count != 0)
                        cert = cc[0];
                }
            }

            if (cert != null)
                return cert;

            if (!localMachine.HasValue || localMachine.Value == false)
            {
                using (var store = new X509Store(StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadOnly);
                    var cc = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprintOrBase64Cert, false);
                    if (cc.Count != 0)
                        cert = cc[0];
                }
            }

            return cert;
        }

        /// <summary>
        /// Выбор сертификата(ов)
        /// </summary>
        /// <param name="singleCertificate">true - выбор одного сертификата(по умолчанию)</param>
        /// <param name="nameCertificate">Имя сертификата, по которому будет осуществлен поиск в хранилище</param>
        /// <returns>Коллекция сертификатов</returns>
        public static X509Certificate2Collection SelectCertificate(bool singleCertificate = true, String nameCertificate = null)
        {
            using (var store = new X509Store(StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
                var scollection = String.IsNullOrEmpty(nameCertificate)
                    ? X509Certificate2UI.SelectFromCollection(
                        store.Certificates,
                        "Выбор сертификата",
                        "Выберите сертификат.",
                        singleCertificate ? X509SelectionFlag.SingleSelection : X509SelectionFlag.MultiSelection)
                    : store.Certificates.Find(
                        X509FindType.FindBySubjectName,
                        nameCertificate,
                        true);
                return scollection;
            }
        }
    }
}
