using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

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

            thumbprintOrBase64Cert = new String(thumbprintOrBase64Cert.ToCharArray().Where(x => Char.IsLetterOrDigit(x) || x == '=').ToArray());

            X509Certificate2 cert = null;
            X509Store store = null;

            try
            {
                cert = new X509Certificate2(Convert.FromBase64String(thumbprintOrBase64Cert));
                if (cert != null)
                    return cert;
            }
            catch
            {
            }

            if (!localMachine.HasValue || localMachine.Value == true)
            {
                store = new X509Store(StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly);
                var cc = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprintOrBase64Cert, false);
                if (cc.Count != 0)
                    cert = cc[0];
            }

            if (cert != null)
                return cert;

            if (!localMachine.HasValue || localMachine.Value == false)
            {
                store = new X509Store(StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);
                var cc = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprintOrBase64Cert, false);
                if (cc.Count != 0)
                    cert = cc[0];
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
            //X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            X509Store store = new X509Store(StoreLocation.CurrentUser);
            store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
            X509Certificate2Collection scollection = null;
            if (String.IsNullOrEmpty(nameCertificate))
            {
                scollection = X509Certificate2UI.SelectFromCollection(
                    store.Certificates,
                    "Выбор сертификата",
                    "Выберите сертификат.",
                    singleCertificate ? X509SelectionFlag.SingleSelection : X509SelectionFlag.MultiSelection);
            }
            else
            {
                scollection = store.Certificates.Find(
                    X509FindType.FindBySubjectName,
                    nameCertificate,
                    true);
            }
            return scollection;
        }

        /// <summary>
        /// Подписывание сообщения в виде массива байт
        /// </summary>
        /// <param name="certificate">Сертификат</param>
        /// <param name="message">Сообщение в виде массива байт</param>
        /// <param name="detached">Подпись откреплена от сообщения(по умолчанию)</param>
        /// <param name="fileSignBASE64">Путь к файлу для записи результата подписания(кодирование в Base64) (null - запись в файл не производится) </param>
        /// <returns>Результат подписания</returns>
        public static byte[] SignPKCS7(X509Certificate2 certificate, byte[] message, bool detached = true, String fileSignBASE64 = null)
        {
            SignedCms signedCms = new SignedCms(new ContentInfo(message), detached);
            signedCms.ComputeSignature(new CmsSigner(certificate), false);
            byte[] sign = signedCms.Encode();

            if (!String.IsNullOrWhiteSpace(fileSignBASE64))
            {
                String signbase64 =
                        "-----BEGIN PKCS7-----" + Environment.NewLine +
                        Convert.ToBase64String(sign) + Environment.NewLine +
                        "-----END PKCS7-----";
                File.WriteAllText(fileSignBASE64, signbase64);
            }

            return sign;
        }

        /// <summary>
        /// Коллекция сертификатов (буфер)
        /// </summary>
        public static X509Certificate2Collection CertificateCollection { get; set; }

        /// <summary>
        /// Сертификат (буфер)
        /// </summary>
        public static X509Certificate2 Certificate { get; set; }

        #region Проверка и визуализация подписей Xml

        /// <summary>
        /// Проверка всех подписей в Xml. Если хоть одна не верна - false, если их нет - true
        /// </summary>
        /// <param name="BodyXml">Тело Xml</param>
        /// <returns>Результат проверки</returns>
        public static bool VerifiXML(String BodyXml)
        {
            // Проверяем все подписи.
            foreach (SignedXml Sign in GetSignatures(BodyXml))
            {
                // Проверяем подпись и выводим результат.
                if (!Sign.CheckSignature())
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Получение подписей в Xml
        /// </summary>
        /// <param name="BodyXml"></param>
        /// <returns></returns>
        public static List<SignedXml> GetSignatures(String BodyXml)
        {
            List<SignedXml> sign = new List<SignedXml>();
            foreach (XmlElement Sign in ReadXml(BodyXml).GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl))
            {
                // Создаем объект SignedXml для проверки подписи документа.
                SignedXml signedXml = new SignedXml(ReadXml(BodyXml));
                // Загружаем узел с подписью.
                signedXml.LoadXml(Sign);
                // Добавляем в коллекцию
                sign.Add(signedXml);
            }
            return sign;
        }

        /// <summary>
        /// Чтение String в необходимый XmlDocument
        /// </summary>
        /// <param name="BodyXml"></param>
        /// <returns></returns>
        private static XmlDocument ReadXml(String BodyXml)
        {
            // Создаем новый XML документ в памяти.
            XmlDocument xmlDocument = new XmlDocument();
            // Сохраняем все пробельные символы, они важны при проверке 
            // подписи.
            xmlDocument.PreserveWhitespace = true;
            // Загружаем подписанный документ из строки
            using (XmlReader xr = XmlReader.Create(new StringReader(BodyXml)))
            {
                xmlDocument.Load(xr);
            }

            return xmlDocument;
        }

        /// <summary>
        /// Визуализация элементов сертификата
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static string ViewCertificat(X509Certificate2 ct)
        {
            StringBuilder res = new StringBuilder();
            string value = null;

            Dictionary<String, String> OID = new Dictionary<string, string>();
            foreach (String x in ct.Issuer.Split(new String[] { ", " }, StringSplitOptions.RemoveEmptyEntries))
            {
                OID.Add(x.Substring(0, x.IndexOf("=")), x.Substring(x.IndexOf("=") + 1));
            }
            res.AppendLine("Издатель сертификата:");
            res.AppendLine("Адрес:");

            if (OID.TryGetValue("C", out value))
                res.AppendLine(" Страна: " + value);
            if (OID.TryGetValue("S", out value))
                res.AppendLine(" Область/край: " + value);
            if (OID.TryGetValue("L", out value))
                res.AppendLine(" Город: " + value);
            if (OID.TryGetValue("STREET", out value))
                res.AppendLine(" Улица: " + value);
            if (OID.TryGetValue("O", out value))
                res.AppendLine(" Организация: " + value);
            if (OID.TryGetValue("OU", out value))
                res.AppendLine(" Подразделение: " + value);
            if (OID.TryGetValue("T", out value))
                res.AppendLine(" Должность: " + value);
            if (OID.TryGetValue("CN", out value))
                res.AppendLine(" Субьект: " + value);
            if (OID.TryGetValue("E", out value))
                res.AppendLine(" e-mail: " + value);

            OID.Clear();
            foreach (String x in ct.Subject.Split(new String[] { ", " }, StringSplitOptions.RemoveEmptyEntries))
            {
                OID.Add(x.Substring(0, x.IndexOf("=")), x.Substring(x.IndexOf("=") + 1));
            }

            res.AppendLine("Получатель сертификата:");
            res.AppendLine("Адрес:");

            if (OID.TryGetValue("C", out value))
                res.AppendLine(" Страна: " + value);
            if (OID.TryGetValue("S", out value))
                res.AppendLine(" Область/край: " + value);
            if (OID.TryGetValue("L", out value))
                res.AppendLine(" Город: " + value);
            if (OID.TryGetValue("STREET", out value))
                res.AppendLine(" Улица: " + value);
            if (OID.TryGetValue("O", out value))
                res.AppendLine(" Организация: " + value);
            if (OID.TryGetValue("OU", out value))
                res.AppendLine(" Подразделение: " + value);
            if (OID.TryGetValue("T", out value))
                res.AppendLine(" Должность: " + value);
            if (OID.TryGetValue("CN", out value))
                res.AppendLine(" Субьект: " + value);
            if (OID.TryGetValue("E", out value))
                res.AppendLine(" e-mail: " + value);
            res.AppendLine();

            res.AppendLine("Начало действия сертификата: " + ct.GetEffectiveDateString());
            res.AppendLine("Срок действия сертификата: " + ct.GetExpirationDateString());
            return res.ToString();
        }

        #endregion


    }

    /// <summary>
    /// Константы для упрощения создания Xml подписи по ГОСТ 34.10.
    /// </summary>
    public static class CPSignedXml
    {
        /// <summary>
        /// Представляет универсальный код ресурса (URI) метода подписи ГОСТ 34.10-2001 для цифровых подписей XML. Это поле имеет постоянное значение.
        /// </summary>
        public const string XmlDsigGost3410Url = "urn:ietf:params:xml:ns:cpxmlsec:algorithms:gostr34102001-gostr3411";

        /// <summary>
        /// Представляет универсальный код ресурса (URI) метода подписи ГОСТ 34.10-2001 для цифровых подписей XML. Это поле имеет постоянное значение.
        /// </summary>
        [Obsolete]
        public const string XmlDsigGost3410UrlObsolete = "http://www.w3.org/2001/04/xmldsig-more#gostr34102001-gostr3411";

        /// <summary>
        /// Представляет универсальный код ресурса (URI) алгоритма GOST3411HMAC для цифровых подписей XML. Это поле имеет постоянное значение.
        /// </summary>
        public const string XmlDsigGost3411HMACUrl = "urn:ietf:params:xml:ns:cpxmlsec:algorithms:hmac-gostr3411";

        /// <summary>
        /// Представляет универсальный код ресурса (URI) метода хэширования ГОСТ Р 34.11. Это поле имеет постоянное значение.
        /// </summary>
        public const string XmlDsigGost3411Url = "urn:ietf:params:xml:ns:cpxmlsec:algorithms:gostr3411";

        /// <summary>
        /// Представляет универсальный код ресурса (URI) метода хэширования ГОСТ Р 34.11. Это поле имеет постоянное значение.
        /// </summary>
        [Obsolete]
        public const string XmlDsigGost3411UrlObsolete = "http://www.w3.org/2001/04/xmldsig-more#gostr3411";

        /// <summary>
        /// Пространство имен  для wssecurity-utility
        /// </summary>
        public const string WSSecurityWSUNamespaceUrl = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

        /// <summary>
        /// Пространство имен  для wssecurity-secext
        /// </summary>
        public const string WSSecurityWSSENamespaceUrl = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    }
}
