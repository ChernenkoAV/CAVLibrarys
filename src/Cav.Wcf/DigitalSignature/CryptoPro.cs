using System;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.ServiceModel.Security;
using System.Xml;
using Cav.ReflectHelpers;

namespace Cav.DigitalSignature
{
    /// <summary>
    /// Реализация ЭЦП по нашим гостам по ЭЦП средствами КриптоПро
    /// </summary>
    public sealed class CryptoProRutine
    {
        #region Алгоритмы для ЭЦП

        private static object lockObj = new object();
        private static Assembly cryptoProSharpeiServiceModel;
        /// <summary>
        /// Получение GostAlgorithmSuite.BasicGostObsolete из КриптоПрошной сборки с отложенной загрузкой.
        /// </summary>
        /// <remarks>
        /// CryptoPro.Sharpei.ServiceModel должен быть в GAC.
        /// Используется CryptoPro.Sharpei.ServiceModel, Version=1.4.0.1, Culture=neutral, PublicKeyToken=473b8c5086e795f5, processorArchitecture=MSIL
        /// </remarks>
        public static SecurityAlgorithmSuite CriptoProBasicGostObsolete
        {
            get
            {
                if (criptoProBasicGostObsolete == null)
                    lock (lockObj)
                    {
                        if (cryptoProSharpeiServiceModel == null)
                            cryptoProSharpeiServiceModel = Assembly.Load("CryptoPro.Sharpei.ServiceModel, Version=1.4.0.1, Culture=neutral, PublicKeyToken=473b8c5086e795f5, processorArchitecture=MSIL");
                        criptoProBasicGostObsolete = (SecurityAlgorithmSuite)cryptoProSharpeiServiceModel.GetStaticOrConstPropertyOrFieldValue("GostAlgorithmSuite", "BasicGostObsolete");
                    }

                return criptoProBasicGostObsolete;
            }
        }

        private static SecurityAlgorithmSuite criptoProBasicGostObsolete;

        /// <summary>
        /// Получение Gost2012_256AlgorithmSuite.BasicGost из КриптоПрошной сборки с отложенной загрузкой.
        /// </summary>
        /// <remarks>
        /// CryptoPro.Sharpei.ServiceModel должен быть в GAC.
        /// Используется CryptoPro.Sharpei.ServiceModel, Version=1.4.0.1, Culture=neutral, PublicKeyToken=473b8c5086e795f5, processorArchitecture=MSIL
        /// </remarks>
        public static SecurityAlgorithmSuite CriptoProGost2012_256BasicGost
        {
            get
            {
                if (criptoProGost2012_256BasicGost == null)
                    lock (lockObj)
                    {
                        if (cryptoProSharpeiServiceModel == null)
                            cryptoProSharpeiServiceModel = Assembly.Load("CryptoPro.Sharpei.ServiceModel, Version=1.4.0.1, Culture=neutral, PublicKeyToken=473b8c5086e795f5, processorArchitecture=MSIL");

                        criptoProGost2012_256BasicGost = (SecurityAlgorithmSuite)cryptoProSharpeiServiceModel.GetStaticOrConstPropertyOrFieldValue("Gost2012_256AlgorithmSuite", "BasicGost");
                    }

                return criptoProGost2012_256BasicGost;
            }
        }

        private static SecurityAlgorithmSuite criptoProGost2012_256BasicGost;

        /// <summary>
        /// Получение Gost2012_512AlgorithmSuite.BasicGost из КриптоПрошной сборки с отложенной загрузкой.
        /// </summary>
        /// <remarks>
        /// CryptoPro.Sharpei.ServiceModel должен быть в GAC.
        /// Используется CryptoPro.Sharpei.ServiceModel, Version=1.4.0.1, Culture=neutral, PublicKeyToken=473b8c5086e795f5, processorArchitecture=MSIL
        /// </remarks>
        public static SecurityAlgorithmSuite CriptoProGost2012_512BasicGost
        {
            get
            {
                if (criptoProGost2012_512BasicGost == null)
                    lock (lockObj)
                    {
                        if (cryptoProSharpeiServiceModel == null)
                            cryptoProSharpeiServiceModel = Assembly.Load("CryptoPro.Sharpei.ServiceModel, Version=1.4.0.1, Culture=neutral, PublicKeyToken=473b8c5086e795f5, processorArchitecture=MSIL");

                        criptoProGost2012_512BasicGost = (SecurityAlgorithmSuite)cryptoProSharpeiServiceModel.GetStaticOrConstPropertyOrFieldValue("Gost2012_512AlgorithmSuite", "BasicGost");
                    }

                return criptoProGost2012_512BasicGost;
            }
        }

        private static SecurityAlgorithmSuite criptoProGost2012_512BasicGost;

        #endregion

        #region Функционал

        /// <summary>
        /// Подпись узла XML AppData структурированных данных СМЕВ в пакете SOAP
        /// </summary>
        /// <param name="XMLForSign">XML для подписи</param>
        /// <param name="Certificate">Сертификат для подписания</param>
        /// <param name="ReferenceUri">Референс в подписи</param>
        /// <returns>Подпись</returns>
        [Obsolete("Будет удалено")]
#pragma warning disable IDE1006 // Стили именования
#pragma warning disable CA1054 // Параметры, напоминающие URI, не должны быть строками
#pragma warning disable CA3075 // Небезопасная обработка DTD в формате XML
#pragma warning disable CA1062 // Проверить аргументы или открытые методы
        public static XmlElement SignXMLElementSMEV(XmlElement XMLForSign, X509Certificate2 Certificate, String ReferenceUri = null)
        {
            var doc = new XmlDocument();
            doc.PreserveWhitespace = true;

            doc.LoadXml(XMLForSign.OuterXml);

            // Создаём объект SmevSignedXml - наследник класса SignedXml с перегруженным GetIdElement
            // для корректной обработки атрибута wsu:Id. 
            var signedXml = new SmevSignedXml(doc);
            // Задаём ключ подписи для документа SmevSignedXml.
            signedXml.SigningKey = Certificate.PrivateKey;
            // Создаем ссылку на подписываемый узел XML. В данном примере и в методических
            // рекомендациях СМЭВ подписываемый узел soapenv:Body помечен идентификатором "body".
            var reference = new Reference();
            reference.Uri = ReferenceUri ?? String.Empty;

            // Задаём алгоритм хэширования подписываемого узла - ГОСТ Р 34.11-94. Необходимо
            // использовать устаревший идентификатор данного алгоритма, т.к. именно такой
            // идентификатор используется в СМЭВ.

            reference.DigestMethod = CPSignedXml.XmlDsigGost3411UrlObsolete;

            // Добавляем преобразование для создания приложенной подписи.
            var env = new XmlDsigEnvelopedSignatureTransform();
            reference.AddTransform(env);

            // Добавляем преобразование для приведения подписываемого узла к каноническому виду
            // по алгоритму http://www.w3.org/2001/10/xml-exc-c14n# в соответствии с методическими
            // рекомендациями СМЭВ.
            var c14 = new XmlDsigExcC14NTransform();
            reference.AddTransform(c14);

            // Добавляем ссылку на подписываемый узел.
            signedXml.AddReference(reference);

            // Задаём преобразование для приведения узла ds:SignedInfo к каноническому виду
            // по алгоритму http://www.w3.org/2001/10/xml-exc-c14n# в соответствии с методическими
            // рекомендациями СМЭВ.
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
            // Задаём алгоритм подписи - ГОСТ Р 34.10-2001. Необходимо использовать устаревший
            // идентификатор данного алгоритма, т.к. именно такой идентификатор используется в
            // СМЭВ.

            signedXml.SignedInfo.SignatureMethod = CPSignedXml.XmlDsigGost3410UrlObsolete;

            // Создаем объект KeyInfo.
            var keyInfo = new KeyInfo();

            // Добавляем сертификат в KeyInfo
            keyInfo.AddClause(new KeyInfoX509Data(Certificate));

            // Добавляем KeyInfo в SignedXml.
            signedXml.KeyInfo = keyInfo;

            // Можно явно проставить алгоритм подписи: ГОСТ Р 34.10.
            // Если сертификат ключа подписи ГОСТ Р 34.10
            // и алгоритм ключа подписи не задан, то будет использован
            // XmlDsigGost3410Url
            // signedXml.SignedInfo.SignatureMethod =
            //     CPSignedXml.XmlDsigGost3410Url;

            // Вычисляем подпись.
            signedXml.ComputeSignature();

            // Получаем представление подписи в виде XML.
            return signedXml.GetXml();
        }

        #endregion

        #region Дополнительные элементы

        /// <summary>
        /// Класс SmevSignedXml - наследник класса SignedXml с перегруженным GetIdElement для корректной обработки атрибута wsu:Id. 
        /// </summary>
        private class SmevSignedXml : SignedXml
        {
            /// <summary>
            /// Создание нового экземпляра SmevSignedXml
            /// </summary>
            /// <param name="document"></param>
            public SmevSignedXml(XmlDocument document)
                : base(document)
            { }

            /// <summary>
            /// Перегрузка GetIdElement
            /// </summary>
            /// <param name="document"></param>
            /// <param name="idValue"></param>
            /// <returns></returns>
            public override XmlElement GetIdElement(XmlDocument document, string idValue)
            {
                var nsmgr = new XmlNamespaceManager(document.NameTable);
                nsmgr.AddNamespace("wsu", CPSignedXml.WSSecurityWSUNamespaceUrl);
                return document.SelectSingleNode("//*[@wsu:Id='" + idValue + "']", nsmgr) as XmlElement;
            }
        }

        #endregion

    }
}
