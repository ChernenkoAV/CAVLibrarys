using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using System.ServiceModel.Security.Tokens;
using System.Text;
using System.Xml;
using Cav.DigitalSignature;

namespace Cav.Wcf
{
    #region Биндинг для СМЭВ
    /// <summary>
    /// Инкапсуляция настроек биндинга для СМЭВ
    /// </summary>
    public sealed class SmevBinding : CustomBinding
    {
        private SmevBinding() { }

        private SmevBinding(Binding binding)
            : base(binding) { }

        /// <summary>
        /// Создание биндинга для взаимодействия по СМЭВ
        /// </summary>
        /// <param name="algorithmSuite">Указывает набор алгоритмов.</param>
        /// <param name="proxy">Прокси для клиента</param>
        /// <param name="senderActor">Actor отправителя (по умолчанию <code>http://smev.gosuslugi.ru/actors/smev</code>)</param>
        /// <param name="recipientActor">Actor получателя (по умолчанию <code>http://smev.gosuslugi.ru/actors/recipient</code>)</param>
        /// <param name="enableUnsecuredResponse">Задает значение, указывающее, может ли отправлять и получать небезопасные ответы или безопасные запросы.</param>        
        /// <param name="allowInsecureTransport">Можно ли отправлять сообщения в смешанном режиме безопасности</param>
        /// <returns></returns>
        public static SmevBinding Create(
            SecurityAlgorithmSuite algorithmSuite,
            String proxy = null,
            String senderActor = null,
            String recipientActor = null,
            Boolean enableUnsecuredResponse = false,
            Boolean allowInsecureTransport = false)
        {

            senderActor = senderActor.GetNullIfIsNullOrWhiteSpace() ?? "http://smev.gosuslugi.ru/actors/smev";
            recipientActor = recipientActor.GetNullIfIsNullOrWhiteSpace() ?? "http://smev.gosuslugi.ru/actors/recipient";

            System.Net.WebRequest.DefaultWebProxy.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;

            var basicHttpBinding = new BasicHttpBinding();
            basicHttpBinding.Security.Mode = BasicHttpSecurityMode.Message;
            basicHttpBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
            basicHttpBinding.Security.Transport.ProxyCredentialType = HttpProxyCredentialType.None;
            basicHttpBinding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.Certificate;
            basicHttpBinding.Security.Message.AlgorithmSuite = algorithmSuite;

            var binding = new SmevBinding(basicHttpBinding)
            {
                Name = "SmevBinding_" + Guid.NewGuid().ToShortString()
            };

            binding.Elements.Remove<TextMessageEncodingBindingElement>();
            binding.Elements.Insert(0, new SMEVMessageEncodingBindingElement()
            {
                SenderActor = senderActor,
                RecipientActor = recipientActor
            });

            var asbe = binding.Elements.Find<AsymmetricSecurityBindingElement>();
            asbe.EnableUnsecuredResponse = enableUnsecuredResponse;
            asbe.IncludeTimestamp = false;
            asbe.MessageProtectionOrder = MessageProtectionOrder.SignBeforeEncrypt;
            asbe.LocalClientSettings.DetectReplays = false;
            asbe.LocalServiceSettings.DetectReplays = false;
            asbe.AllowSerializedSigningTokenOnReply = true;
            asbe.RecipientTokenParameters.RequireDerivedKeys = false;
            asbe.RecipientTokenParameters.InclusionMode = SecurityTokenInclusionMode.AlwaysToInitiator;
            asbe.AllowInsecureTransport = allowInsecureTransport;

            var htbe = binding.Elements.Find<HttpTransportBindingElement>();
            htbe.ManualAddressing = false;
            htbe.MaxReceivedMessageSize = int.MaxValue - 1;
            htbe.MaxBufferSize = int.MaxValue - 1;
            htbe.MaxBufferPoolSize = int.MaxValue - 1;

            if (!proxy.IsNullOrWhiteSpace())
            {
                htbe.ProxyAddress = new Uri(proxy);
                htbe.UseDefaultWebProxy = false;
            }

            return binding;
        }
    }

    #endregion

    #region Биндинг-элемент кодировщика для СМЭВ

    internal sealed class SMEVMessageEncodingBindingElement : MessageEncodingBindingElement, IWsdlExportExtension, IPolicyExportExtension
    {
        public SMEVMessageEncodingBindingElement()
        {
            var tmebe = new TextMessageEncodingBindingElement
            {
                MessageVersion = messageVer
            };
            tmebe.ReaderQuotas.MaxStringContentLength = int.MaxValue - 1;
            tmebe.ReaderQuotas.MaxArrayLength = int.MaxValue - 1;
            tmebe.ReaderQuotas.MaxBytesPerRead = int.MaxValue - 1;
            tmebe.ReaderQuotas.MaxDepth = int.MaxValue - 1;
            tmebe.ReaderQuotas.MaxNameTableCharCount = int.MaxValue - 1;

            innerBindingElement = tmebe;
        }

        public SMEVMessageEncodingBindingElement(MessageEncodingBindingElement innerBindingElement) =>
            this.innerBindingElement = innerBindingElement;

        private MessageEncodingBindingElement innerBindingElement;
        private MessageVersion messageVer = MessageVersion.Soap11;

        public string SenderActor { get; set; }
        public string RecipientActor { get; set; }

        public override MessageVersion MessageVersion
        {
            get => innerBindingElement.MessageVersion;
            set => innerBindingElement.MessageVersion = value;
        }

        public override MessageEncoderFactory CreateMessageEncoderFactory()
        {
            var sef =
                new SMEVMessageEncoderFactory(
                    "text/xml",
                    "utf-8",
                    messageVer,
                    innerBindingElement.CreateMessageEncoderFactory(),
                    SenderActor,
                    RecipientActor);
            return sef;
        }

        public override BindingElement Clone()
        {
            var clone = new SMEVMessageEncodingBindingElement(innerBindingElement)
            {
                MessageVersion = MessageVersion,
                RecipientActor = RecipientActor,
                SenderActor = SenderActor
            };
            return clone;
        }

        public override T GetProperty<T>(BindingContext context) =>
            typeof(T) == typeof(XmlDictionaryReaderQuotas)
                ? innerBindingElement.GetProperty<T>(context)
                : base.GetProperty<T>(context);

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            context.BindingParameters.Add(this);
            return context.BuildInnerChannelFactory<TChannel>();
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            context.BindingParameters.Add(this);
            return context.BuildInnerChannelListener<TChannel>();
        }

        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            context.BindingParameters.Add(this);
            return context.CanBuildInnerChannelListener<TChannel>();
        }

        #region Члены IWsdlExportExtension

        public void ExportContract(WsdlExporter exporter, WsdlContractConversionContext context) { }

        public void ExportEndpoint(WsdlExporter exporter, WsdlEndpointConversionContext context) =>
            ((IWsdlExportExtension)innerBindingElement).ExportEndpoint(exporter, context);

        #endregion

        #region Члены IPolicyExportExtension

        public void ExportPolicy(MetadataExporter exporter, PolicyConversionContext context) =>
            ((IPolicyExportExtension)innerBindingElement).ExportPolicy(exporter, context);

        #endregion
    }

    #endregion

    #region Кодировщик

    internal class SMEVMessageEncoder : MessageEncoder
    {
        public SMEVMessageEncoder(SMEVMessageEncoderFactory factory)
        {
            this.factory = factory;

            writerSettings = new XmlWriterSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment,
                OmitXmlDeclaration = true,
                NewLineHandling = NewLineHandling.Entitize,
                Encoding = this.factory.CharSet.Trim().ToLower(CultureInfo.CurrentCulture) == "utf-8"
                ? new UTF8Encoding(false)
                : Encoding.GetEncoding(this.factory.CharSet)
            };

            theContentType = string.Format("{0}; charset={1}", this.factory.MediaType, writerSettings.Encoding.HeaderName);
        }

        private string theContentType;
        private SMEVMessageEncoderFactory factory;
        private XmlWriterSettings writerSettings;

        public override string ContentType => theContentType;

        public override string MediaType => factory.MediaType;

        public override MessageVersion MessageVersion => factory.MessageVersion;

        #region ReadMessage

        public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
        {
            var msgContents = new byte[buffer.Count];
            Array.Copy(buffer.Array, buffer.Offset, msgContents, 0, msgContents.Length);

            using (var mstr = new MemoryStream(msgContents))
                return ReadMessage(mstr, int.MaxValue, contentType);
        }

        public override Message ReadMessage(Stream stream, int maxSizeOfHeaders, string contentType)
        {
            var xmlDocument = new XmlDocument
            {
                PreserveWhitespace = true
            };
#pragma warning disable CA3075 // Небезопасная обработка DTD в формате XML
            xmlDocument.Load(stream);
#pragma warning restore CA3075 // Небезопасная обработка DTD в формате XML

            var secNodeList = xmlDocument.GetElementsByTagName("Security", CPSignedXml.WSSecurityWSSENamespaceUrl);
            foreach (var secNode in secNodeList.Cast<XmlNode>())
            {
                var actorAttrib = secNode.Attributes["actor", WcfHelpers.soap11Namespace];
                if (actorAttrib != null && actorAttrib.Value == factory.RecipientActor)
                    secNode.Attributes.Remove(actorAttrib);

                secNode.Attributes.RemoveNamedItem("mustUnderstand", WcfHelpers.soap11Namespace);
            }

            Message res = null;

            using (var mstrm = new MemoryStream())
            {
                using (var xwr = XmlWriter.Create(mstrm, writerSettings))
                    xmlDocument.WriteTo(xwr);

                mstrm.Position = 0;

                using (var xrdr = XmlReader.Create(mstrm))
#pragma warning disable CA2000 // Ликвидировать объекты перед потерей области
                    res = Message.CreateMessage(xrdr, maxSizeOfHeaders, MessageVersion).CreateBufferedCopy(maxSizeOfHeaders).CreateMessage();
#pragma warning restore CA2000 // Ликвидировать объекты перед потерей области
            }

            return res;
        }

        #endregion

        #region WriteMessage

        public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
        {
            var memoryStream = new MemoryStream();
            WriteMessage(message, memoryStream);

            var buffer2 = memoryStream.GetBuffer();
            var messageLength = (int)memoryStream.Position;
            memoryStream.Close();
            var bufferSize = messageLength + messageOffset;
            var array = bufferManager.TakeBuffer(bufferSize);
            Array.Copy(buffer2, 0, array, messageOffset, messageLength);
            return new ArraySegment<byte>(array, messageOffset, messageLength);
        }

        public override void WriteMessage(Message message, Stream stream)
        {
            var xmlDocument = new XmlDocument
            {
                PreserveWhitespace = true
            };

            using (var memstr = new MemoryStream())
            {
                using (var xwriter = XmlWriter.Create(memstr, writerSettings))
                    message.WriteMessage(xwriter);
                memstr.Position = 0;
#pragma warning disable CA3075 // Небезопасная обработка DTD в формате XML
                xmlDocument.Load(memstr);
#pragma warning restore CA3075 // Небезопасная обработка DTD в формате XML
            }

            var securityXmlNodeList = xmlDocument.GetElementsByTagName("Security", CPSignedXml.WSSecurityWSSENamespaceUrl);
            foreach (var securityXmlNode in securityXmlNodeList.Cast<XmlNode>())
            {
                var actorXmlAttribute = xmlDocument.CreateAttribute("actor", WcfHelpers.soap11Namespace);
                actorXmlAttribute.Value = factory.SenderActor;

                securityXmlNode.Attributes.Append(actorXmlAttribute);
            }

            using (var xwr = XmlWriter.Create(stream, writerSettings))
                xmlDocument.WriteTo(xwr);
        }

        #endregion

        public override bool IsContentTypeSupported(string contentType)
        {
            if (base.IsContentTypeSupported(contentType))
            {
                return true;
            }

            if (contentType.Length == MediaType.Length)
            {
                return contentType.Equals(MediaType, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                if (contentType.StartsWith(MediaType, StringComparison.OrdinalIgnoreCase)
                    && (contentType[MediaType.Length] == ';'))
                {
                    return true;
                }
            }

            return false;
        }
    }

    #endregion

    #region Фабрика кодировщика

    internal class SMEVMessageEncoderFactory : MessageEncoderFactory
    {
        internal SMEVMessageEncoderFactory(string mediaType, string charSet, MessageVersion version, MessageEncoderFactory messageFactory)
            : this(mediaType, charSet, version, messageFactory, String.Empty, String.Empty)
        {
        }

        internal SMEVMessageEncoderFactory(string mediaType, string charSet, MessageVersion version, MessageEncoderFactory messageFactory, string senderActor, string recipientActor)
        {
            InnerFactory = messageFactory;
            MessageVersion = version;
            MediaType = mediaType;
            CharSet = charSet;
            SenderActor = senderActor;
            RecipientActor = recipientActor;
            Encoder = new SMEVMessageEncoder(this);
        }

        internal MessageEncoderFactory InnerFactory { get; }
        public override MessageEncoder Encoder { get; }
        public override MessageVersion MessageVersion { get; }
        public string SenderActor { get; }
        public string RecipientActor { get; }
        public string MediaType { get; }
        public string CharSet { get; }
    }

    #endregion

    #region Фабрика сервиса для IIS

    /// <summary>
    /// Фабрика создания хоста в IIS. Дабы не писать всякие расширения...
    /// </summary>
    public abstract class SmevServiceHostFactoryBase : ServiceHostFactory
    {
        /// <summary>
        /// Реализация сервиса (класс)
        /// </summary>
        protected Type ImplemetationServiceType { get; set; }
        /// <summary>
        /// Сертификат сервера
        /// </summary>
        protected X509Certificate2 CertificateServer { get; set; }
        /// <summary>
        /// Набор алгоритмов (Типа криптопрошных...)
        /// </summary>
        protected SecurityAlgorithmSuite AlgorithmSuite { get; set; }
        /// <summary>
        /// actor в исходящем сообщении
        /// </summary>
        protected String SenderActor { get; set; }
        /// <summary>
        /// actor в входящем сообщении
        /// </summary>
        protected String RecipientActor { get; set; }

        /// <summary>
        /// Создание нового экземпляра хоста службы
        /// </summary>
        /// <param name="t"></param>
        /// <param name="baseAddresses"></param>
        /// <returns></returns>
#pragma warning disable CA1725 // Имена параметров должны соответствовать базовому объявлению
        protected override ServiceHost CreateServiceHost(Type t, Uri[] baseAddresses) =>
            ImplemetationServiceType == null
                ? throw new InvalidOperationException("Не определен класс реализации сервиса")
                : CertificateServer == null
                ? throw new InvalidOperationException("Не определен сертификат сервера")
                : AlgorithmSuite == null
                ? throw new InvalidOperationException("Не определен набор алгоритмов")
                : (ServiceHost)new SmevServiceHost(ImplemetationServiceType, baseAddresses, AlgorithmSuite, CertificateServer, SenderActor, RecipientActor);

        /// <summary>
        /// Создание нового экземпляра хоста службы
        /// </summary>
        /// <param name="constructorString"></param>
        /// <param name="baseAddresses"></param>
        /// <returns></returns>
        public override ServiceHostBase CreateServiceHost(string constructorString, Uri[] baseAddresses) => CreateServiceHost(null, baseAddresses);
    }

    // TODO Реализовать для selfHosting!!

    /// <summary>
    /// Кастомизированный хост сервиса
    /// </summary>
    internal class SmevServiceHost : ServiceHost
    {
        /// <summary>
        /// Экземпляр хоста
        /// </summary>
        /// <param name="iS">Тип класа, реализующего сервис</param>
        /// <param name="uri">Uri базовых адресов</param>
        /// <param name="algorithmSuite">Набор алгоритмов</param>
        /// <param name="certificateServer">Сертификат сервера</param>
        /// <param name="senderActor">actor отправителя сообщения</param>
        /// <param name="recipientActor">actor получателя сообщения</param>
        internal SmevServiceHost(Type iS, Uri[] uri, SecurityAlgorithmSuite algorithmSuite, X509Certificate2 certificateServer, String senderActor, String recipientActor)
            : base(iS, uri)
        {
            this.algorithmSuite = algorithmSuite;
            сertificateServer = certificateServer;
            this.senderActor = senderActor;
            this.recipientActor = recipientActor;
        }
        /// <summary>
        /// Набор алгоритмов (Типа криптопрошных...)
        /// </summary>
        private SecurityAlgorithmSuite algorithmSuite;
        /// <summary>
        /// Сертификат сервера
        /// </summary>
        private X509Certificate2 сertificateServer;
        /// <summary>
        /// actor в посылаемом сообщении
        /// </summary>
        private String senderActor;
        /// <summary>
        /// actor в принимаемом сообщении
        /// </summary>
        private String recipientActor;

        /// <summary>
        /// Добавление конечной точки по умолчанию. Вызывается IIS, если в конфиге не указать конечную точку
        /// </summary>
        /// <returns></returns>
        public override System.Collections.ObjectModel.ReadOnlyCollection<ServiceEndpoint> AddDefaultEndpoints()
        {
            var lse = new List<ServiceEndpoint>();

            foreach (var item in BaseAddresses)
            {
                var sep = AddServiceEndpoint(
                    ImplementedContracts.Values.First().ContractType,
                    SmevBinding.Create(
                        algorithmSuite: algorithmSuite,
                        senderActor: senderActor,
                        recipientActor: recipientActor), "");
                sep.Contract.ProtectionLevel = System.Net.Security.ProtectionLevel.Sign;
                lse.Add(sep);
            }

            Credentials.ServiceCertificate.Certificate = сertificateServer;
            Credentials.ClientCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.None;
            Credentials.ClientCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;

            Description.Behaviors.Add(new ServiceMetadataBehavior() { HttpGetEnabled = true });

            return new System.Collections.ObjectModel.ReadOnlyCollection<ServiceEndpoint>(lse);
        }

        // Можно переписать настройки здесь.
        protected override void ApplyConfiguration()
        {
            //base.ApplyConfiguration();
        }
    }

    #endregion
}

