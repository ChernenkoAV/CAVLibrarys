﻿using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using Cav.Soap;

namespace Cav.DigitalSignature
{
    /// <summary>
    /// Реализация ЭЦП по нашим гостам по ЭЦП средствами КриптоПро
    /// </summary>
    sealed public class CryptoProRutine
    {
        #region Функционал

        /// <summary>
        /// Подпись узла XML AppData структурированных данных СМЕВ в пакете SOAP
        /// </summary>
        /// <param name="XMLForSign">XML для подписи</param>
        /// <param name="Certificate">Сертификат для подписания</param>
        /// <param name="ReferenceUri">Референс в подписи</param>
        /// <returns>Подпись</returns>
        public static XmlElement SignXMLElementSMEV(XmlElement XMLForSign, X509Certificate2 Certificate, String ReferenceUri = null)
        {
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(XMLForSign.OuterXml);

            // Создаём объект SmevSignedXml - наследник класса SignedXml с перегруженным GetIdElement
            // для корректной обработки атрибута wsu:Id. 
            SmevSignedXml signedXml = new SmevSignedXml(doc);
            // Задаём ключ подписи для документа SmevSignedXml.
            signedXml.SigningKey = Certificate.PrivateKey;
            // Создаем ссылку на подписываемый узел XML. В данном примере и в методических
            // рекомендациях СМЭВ подписываемый узел soapenv:Body помечен идентификатором "body".
            Reference reference = new Reference();
            reference.Uri = ReferenceUri ?? String.Empty;

            // Задаём алгоритм хэширования подписываемого узла - ГОСТ Р 34.11-94. Необходимо
            // использовать устаревший идентификатор данного алгоритма, т.к. именно такой
            // идентификатор используется в СМЭВ.
#pragma warning disable 612
            reference.DigestMethod = CPSignedXml.XmlDsigGost3411UrlObsolete;
#pragma warning restore 612

            // Добавляем преобразование для создания приложенной подписи.
            XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
            reference.AddTransform(env);

            // Добавляем преобразование для приведения подписываемого узла к каноническому виду
            // по алгоритму http://www.w3.org/2001/10/xml-exc-c14n# в соответствии с методическими
            // рекомендациями СМЭВ.
            XmlDsigExcC14NTransform c14 = new XmlDsigExcC14NTransform();
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

#pragma warning disable 612
            signedXml.SignedInfo.SignatureMethod = CPSignedXml.XmlDsigGost3410UrlObsolete;
#pragma warning restore 612


            // Создаем объект KeyInfo.
            KeyInfo keyInfo = new KeyInfo();

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
        public class SmevSignedXml : SignedXml
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
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(document.NameTable);
                nsmgr.AddNamespace("wsu", CPSignedXml.WSSecurityWSUNamespaceUrl);
                return document.SelectSingleNode("//*[@wsu:Id='" + idValue + "']", nsmgr) as XmlElement;
            }
        }

        #endregion

        #region Визуализация ЭЦП СМЕВ

        /// <summary>
        /// Визуализация ЭЦП в XML
        /// </summary>
        /// <param name="Xml"></param>
        /// <returns></returns>
        public static String ViewDSigSMEV(String Xml)
        {
            if (String.IsNullOrWhiteSpace(Xml))
                return null;

            StringBuilder ResText = new StringBuilder();

            // Создаем новый XML документ в памяти.
            XmlDocument xmlDocument = new XmlDocument();
            // Сохраняем все пробельные символы, они важны при проверке 
            // подписи.
            xmlDocument.PreserveWhitespace = true;

            // Загружаем подписанный документ из строки
            using (XmlReader xr = XmlReader.Create(new StringReader(Xml)))
            {
                xmlDocument.Load(xr);
            }

            if (xmlDocument.GetElementsByTagName("Reference", SignedXml.XmlDsigNamespaceUrl).Count == 0)
                return "ЭЦП отсутствуют";

            foreach (XmlElement Sign in xmlDocument.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl))
            {
                ResText.AppendLine("ЭЦП:");

                // Создаем объект SignedXml для проверки подписи документа.
                SignedXml signedXml = new SmevSignedXml(xmlDocument);
                // Загружаем узел с подписью.
                signedXml.LoadXml(Sign);

                #region Определяем, какую область XML подписывает эта подпись

                ResText.AppendLine(" Подписано:");
                foreach (Reference r in signedXml.SignedInfo.References)
                {
                    XmlElement areasign = signedXml.GetIdElement(xmlDocument, r.Uri.Substring(1));

                    if (areasign == null)
                    {
                        areasign = xmlDocument.SelectSingleNode("//*[@id='" + r.Uri.Substring(1) + "']") as XmlElement;

                        if (areasign == null)
                        {
                            ResText.AppendLine("  Не найден узел XML!");
                            continue;
                        }
                    }

                    if (areasign.NamespaceURI.Contains("smev.gosuslugi.ru") & areasign.LocalName == "Header")
                    {
                        ResText.AppendLine("  Заголовок СМЭВ");
                        continue;
                    }

                    if (areasign.NamespaceURI.Contains(SoapHelper.Soap11Namespace) & areasign.LocalName == "Body")
                    {
                        ResText.AppendLine("  Тело сообщения");
                        continue;
                    }

                    if (areasign.NamespaceURI.Contains("smev.gosuslugi.ru") & areasign.LocalName == "AppData")
                    {
                        ResText.AppendLine(" Структурированные данные");
                        continue;
                    }

                    ResText.AppendLine("  Узел XML:'" + areasign.NamespaceURI + "':" + areasign.LocalName);
                }

                #endregion

                #region Получение данных сертификатов

                ResText.AppendLine(" Данные сертификата:");

                X509Certificate2 X509Cert = null;

                // сертификат... и флаг и данные....                
                // Смотрим сертификаты в KeyInfo подписи
                IEnumerator ki = signedXml.KeyInfo.GetEnumerator();
                ki.Reset();
                while (ki.MoveNext())
                {
                    KeyInfoX509Data kix509 = ki.Current as KeyInfoX509Data;
                    if (kix509 == null)
                        continue;

                    // В смеве подписывают одним сертификатом. наверное.
                    X509Cert = (X509Certificate2)kix509.Certificates[0];
                    //signedXml = new SignedXml(xmlDocument);
                    //signedXml.LoadXml(Sign);
                }

                // Смотрим сертификат в BinarySecurityToken
                if (X509Cert == null)
                {
                    XmlNodeList referenceList = signedXml.KeyInfo.GetXml().GetElementsByTagName(
                        "Reference", CPSignedXml.WSSecurityWSSENamespaceUrl);
                    if (referenceList.Count == 0)
                    {
                        ResText.AppendLine("  Не удалось найти ссылку на сертификат");
                        continue;
                    }

                    // Ищем среди аттрибутов ссылку на сертификат.
                    string binaryTokenReference = ((XmlElement)referenceList[0]).GetAttribute("URI");

                    // Ссылка должна быть на узел внутри данного документа XML, т.е. она имеет вид
                    // #ID, где ID - идентификатор целевого узла
                    if (string.IsNullOrEmpty(binaryTokenReference))//|| binaryTokenReference[0] != '#')
                    {
                        ResText.AppendLine("  Не удалось найти ссылку на сертификат");
                        continue;
                    }

                    // Получаем узел BinarySecurityToken с закодированным в base64 сертификатом
                    // вот так правильно (то есть в ссылка должна быть с лидирующим #)
                    XmlElement binaryTokenElement = signedXml.GetIdElement(xmlDocument, binaryTokenReference.Substring(1));
                    // Но бывает и без '#'. Уроды...
                    if (binaryTokenElement == null)
                        binaryTokenElement = signedXml.GetIdElement(xmlDocument, binaryTokenReference);

                    if (binaryTokenElement == null)
                    {
                        ResText.AppendLine("  Не удалось найти сертификат");
                        continue;
                    }

                    X509Cert = new X509Certificate2(Convert.FromBase64String(binaryTokenElement.InnerText));
                }

                #endregion

                ResText.Append(DSGeneric.ViewCertificat(X509Cert));

                #region Проверяем валидность подписи

                try
                {
                    if (signedXml.CheckSignature(X509Cert.PublicKey.Key))
                        ResText.AppendLine("Подпись данных действительна");
                    else
                        ResText.AppendLine("Подпись данных НЕ действительна!");
                }
                catch (Exception ex)
                {
                    ResText.AppendLine("Проверка подписи неуспешна: " + ex.Message);
                }

                #endregion

                ResText.AppendLine("".PadLeft(50, '_'));
            }

            return ResText.ToString();
        }

        #endregion


        //TODO причесать GostAlgorithmSuite относительно CPSignedXml

        ///// <summary>
        /////  Набор алгоритмов безопасности по российским ГОСТ. Продублировано(вырезано) для исключения подключения сборок КриптоПро
        ///// </summary>
        //public class GostAlgorithmSuite : SecurityAlgorithmSuite
        //{
        //    private static SecurityAlgorithmSuite _basicGost;
        //    private static SecurityAlgorithmSuite _basicGostObsolete;

        //    private bool useOldAlgorithSuite;

        //    protected GostAlgorithmSuite()
        //    {
        //    }

        //    protected GostAlgorithmSuite(bool useOldAlgorithmSuite)
        //    {
        //        this.useOldAlgorithSuite = useOldAlgorithmSuite;
        //    }

        //    public override bool IsAsymmetricKeyLengthSupported(int length)
        //    {
        //        return (length == 0x200);
        //    }

        //    public override bool IsAsymmetricKeyWrapAlgorithmSupported(string algorithm)
        //    {
        //        return (algorithm == this.DefaultAsymmetricKeyWrapAlgorithm);
        //    }

        //    public override bool IsAsymmetricSignatureAlgorithmSupported(string algorithm)
        //    {
        //        return (algorithm == this.DefaultAsymmetricSignatureAlgorithm);
        //    }

        //    public override bool IsDigestAlgorithmSupported(string algorithm)
        //    {
        //        return (algorithm == this.DefaultDigestAlgorithm);
        //    }

        //    public override bool IsEncryptionAlgorithmSupported(string algorithm)
        //    {
        //        return (algorithm == this.DefaultEncryptionAlgorithm);
        //    }

        //    public override bool IsEncryptionKeyDerivationAlgorithmSupported(string algorithm)
        //    {
        //        return (algorithm == "urn:ietf:params:xml:ns:cpxmlsec:algorithms:dk-p-gostr3411");
        //    }

        //    public override bool IsSignatureKeyDerivationAlgorithmSupported(string algorithm)
        //    {
        //        return (algorithm == "urn:ietf:params:xml:ns:cpxmlsec:algorithms:dk-p-gostr3411");
        //    }

        //    public override bool IsSymmetricKeyLengthSupported(int length)
        //    {
        //        return (length == this.DefaultSymmetricKeyLength);
        //    }

        //    public override bool IsSymmetricKeyWrapAlgorithmSupported(string algorithm)
        //    {
        //        return (algorithm == this.DefaultSymmetricKeyWrapAlgorithm);
        //    }

        //    public override bool IsSymmetricSignatureAlgorithmSupported(string algorithm)
        //    {
        //        return (algorithm == this.DefaultSymmetricSignatureAlgorithm);
        //    }

        //    public static SecurityAlgorithmSuite BasicGost
        //    {
        //        get
        //        {
        //            if (_basicGost == null)
        //            {
        //                SecurityAlgorithmSuite suite = new GostAlgorithmSuite();
        //                Interlocked.CompareExchange<SecurityAlgorithmSuite>(ref _basicGost, suite, null);
        //            }
        //            return _basicGost;
        //        }
        //    }

        //    public static SecurityAlgorithmSuite BasicGostObsolete
        //    {
        //        get
        //        {
        //            if (_basicGostObsolete == null)
        //            {
        //                SecurityAlgorithmSuite suite = new GostAlgorithmSuite(true);
        //                Interlocked.CompareExchange<SecurityAlgorithmSuite>(ref _basicGostObsolete, suite, null);
        //            }
        //            return _basicGostObsolete;
        //        }
        //    }

        //    public override string DefaultAsymmetricKeyWrapAlgorithm
        //    {
        //        get
        //        {
        //            return "urn:ietf:params:xml:ns:cpxmlsec:algorithms:transport-gost2001";
        //        }
        //    }

        //    public override string DefaultAsymmetricSignatureAlgorithm
        //    {
        //        get
        //        {
        //            if (this.useOldAlgorithSuite)
        //            {
        //                return "http://www.w3.org/2001/04/xmldsig-more#gostr34102001-gostr3411";
        //            }
        //            return "urn:ietf:params:xml:ns:cpxmlsec:algorithms:gostr34102001-gostr3411";
        //        }
        //    }

        //    public override string DefaultCanonicalizationAlgorithm
        //    {
        //        get
        //        {
        //            return SecurityAlgorithmSuite.Default.DefaultCanonicalizationAlgorithm;
        //        }
        //    }

        //    public override string DefaultDigestAlgorithm
        //    {
        //        get
        //        {
        //            if (this.useOldAlgorithSuite)
        //            {
        //                return "http://www.w3.org/2001/04/xmldsig-more#gostr3411";
        //            }
        //            return "urn:ietf:params:xml:ns:cpxmlsec:algorithms:gostr3411";
        //        }
        //    }

        //    public override string DefaultEncryptionAlgorithm
        //    {
        //        get
        //        {
        //            return "urn:ietf:params:xml:ns:cpxmlsec:algorithms:gost28147";
        //        }
        //    }

        //    public override int DefaultEncryptionKeyDerivationLength
        //    {
        //        get
        //        {
        //            return this.DefaultSymmetricKeyLength;
        //        }
        //    }

        //    public override int DefaultSignatureKeyDerivationLength
        //    {
        //        get
        //        {
        //            return this.DefaultSymmetricKeyLength;
        //        }
        //    }

        //    public override int DefaultSymmetricKeyLength
        //    {
        //        get
        //        {
        //            return 0x100;
        //        }
        //    }

        //    public override string DefaultSymmetricKeyWrapAlgorithm
        //    {
        //        get
        //        {
        //            return "urn:ietf:params:xml:ns:cpxmlsec:algorithms:kw-cp";
        //        }
        //    }

        //    public override string DefaultSymmetricSignatureAlgorithm
        //    {
        //        get
        //        {
        //            return "urn:ietf:params:xml:ns:cpxmlsec:algorithms:hmac-gostr3411";
        //        }
        //    }
        //    public bool IsObsoleteAlgorithSuite
        //    {
        //        get
        //        {
        //            return this.useOldAlgorithSuite;
        //        }
        //    }

        //}

    }
}
