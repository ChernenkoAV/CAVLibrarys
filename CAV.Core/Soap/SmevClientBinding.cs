using System;
using System.Collections.Generic;
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

namespace Cav.Soap
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
        /// <param name="AlgorithmSuite">Указывает набор алгоритмов.</param>
        /// <param name="Proxy">Прокси для клиента</param>
        /// <param name="LoggerInstance">Экземпляр объекта, реализующего ISoapPackageLog для логирования</param>
        /// <param name="EnableUnsecuredResponse">Задает значение, указывающее, может ли отправлять и получать небезопасные ответы или безопасные запросы.</param>
        /// <param name="SenderActor">Actor отправителя</param>
        /// <param name="RecipientActor">Actor получателя</param>
        /// <param name="AllowInsecureTransport">Можно ли отправлять сообщения в смешанном режиме безопасности</param>
        /// <returns></returns>
        public static SmevBinding Create(
            SecurityAlgorithmSuite AlgorithmSuite,
            String Proxy = null,
            String SenderActor = null,
            String RecipientActor = null,
            ISoapPackageLog LoggerInstance = null,
            Boolean EnableUnsecuredResponse = false,
            Boolean AllowInsecureTransport = false)
        {
            System.Net.WebRequest.DefaultWebProxy.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;

            BasicHttpBinding basicHttpBinding = new BasicHttpBinding();
            basicHttpBinding.Security.Mode = BasicHttpSecurityMode.Message;
            basicHttpBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
            basicHttpBinding.Security.Transport.ProxyCredentialType = HttpProxyCredentialType.None;
            basicHttpBinding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.Certificate;
            basicHttpBinding.Security.Message.AlgorithmSuite = AlgorithmSuite;

            
            

            SmevBinding binding = new SmevBinding(basicHttpBinding);
            binding.Name = "SmevBinding";

            binding.Elements.Remove<TextMessageEncodingBindingElement>();
            binding.Elements.Insert(0, new SMEVMessageEncodingBindingElement()
                {
                    LoggerInstance = LoggerInstance,
                    SenderActor = SenderActor,
                    RecipientActor = RecipientActor
                });

            AsymmetricSecurityBindingElement asbe = binding.Elements.Find<AsymmetricSecurityBindingElement>();
            asbe.EnableUnsecuredResponse = EnableUnsecuredResponse;
            asbe.IncludeTimestamp = false;
            asbe.MessageProtectionOrder = MessageProtectionOrder.SignBeforeEncrypt;
            asbe.LocalClientSettings.DetectReplays = false;
            asbe.LocalServiceSettings.DetectReplays = false;
            asbe.AllowSerializedSigningTokenOnReply = true;
            asbe.RecipientTokenParameters.RequireDerivedKeys = false;
            asbe.RecipientTokenParameters.InclusionMode = SecurityTokenInclusionMode.AlwaysToInitiator;
            asbe.AllowInsecureTransport = AllowInsecureTransport;

            HttpTransportBindingElement htbe = binding.Elements.Find<HttpTransportBindingElement>();
            htbe.ManualAddressing = false;
            htbe.MaxReceivedMessageSize = int.MaxValue - 1;
            htbe.MaxBufferSize = int.MaxValue - 1;
            htbe.MaxBufferPoolSize = int.MaxValue - 1;

            if (!Proxy.IsNullOrWhiteSpace())
            {
                htbe.ProxyAddress = new Uri(Proxy);
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
            theInnerBindingElement = new TextMessageEncodingBindingElement();
            theInnerBindingElement.MessageVersion = theVers;
        }

        public SMEVMessageEncodingBindingElement(MessageEncodingBindingElement innerBindingElement)
        {
            theInnerBindingElement = innerBindingElement;
        }

        public ISoapPackageLog LoggerInstance { get; set; }

        private MessageEncodingBindingElement theInnerBindingElement;
        private MessageVersion theVers = MessageVersion.Soap11;
        private string theSenderActor = "http://smev.gosuslugi.ru/actors/smev";
        private string theRecipientActor = "http://smev.gosuslugi.ru/actors/recipient";

        public string SenderActor
        {
            get
            {
                return theSenderActor;
            }
            set
            {
                if (value == null)
                    return;
                theSenderActor = value;
            }
        }

        public string RecipientActor
        {
            get
            {
                return theRecipientActor;
            }
            set
            {
                if (value == null)
                    return;
                theRecipientActor = value;
            }
        }

        public override MessageVersion MessageVersion
        {
            get
            {
                return theInnerBindingElement.MessageVersion;
            }
            set
            {
                theInnerBindingElement.MessageVersion = value;
            }
        }

        public override MessageEncoderFactory CreateMessageEncoderFactory()
        {
            SMEVMessageEncoderFactory sef = new SMEVMessageEncoderFactory("text/xml", "utf-8", theVers, theInnerBindingElement.CreateMessageEncoderFactory(), this.SenderActor, this.RecipientActor);
            sef.SetEvents(LoggerInstance);
            return sef;
        }

        public override BindingElement Clone()
        {
            SMEVMessageEncodingBindingElement clone = new SMEVMessageEncodingBindingElement(theInnerBindingElement);
            clone.MessageVersion = this.MessageVersion;
            clone.RecipientActor = this.RecipientActor;
            clone.SenderActor = this.SenderActor;
            clone.LoggerInstance = this.LoggerInstance;
            return (clone);
        }

        public override T GetProperty<T>(BindingContext context)
        {
            if (typeof(T) == typeof(XmlDictionaryReaderQuotas))
                return theInnerBindingElement.GetProperty<T>(context);
            else
                return base.GetProperty<T>(context);
        }

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            context.BindingParameters.Add((object)this);
            return context.BuildInnerChannelFactory<TChannel>();
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            context.BindingParameters.Add((object)this);
            return context.BuildInnerChannelListener<TChannel>();
        }

        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            context.BindingParameters.Add((object)this);
            return context.CanBuildInnerChannelListener<TChannel>();
        }

        #region Члены IWsdlExportExtension

        public void ExportContract(WsdlExporter exporter, WsdlContractConversionContext context) { }

        public void ExportEndpoint(WsdlExporter exporter, WsdlEndpointConversionContext context)
        {
            ((IWsdlExportExtension)theInnerBindingElement).ExportEndpoint(exporter, context);
        }

        #endregion

        #region Члены IPolicyExportExtension

        public void ExportPolicy(MetadataExporter exporter, PolicyConversionContext context)
        {
            ((IPolicyExportExtension)theInnerBindingElement).ExportPolicy(exporter, context);

        }

        #endregion
    }

    #endregion

    #region Кодировщик

    internal class SMEVMessageEncoder : MessageEncoder
    {
        public SMEVMessageEncoder(SMEVMessageEncoderFactory factory)
        {
            theFactory = factory;
            theContentType = theFactory.MediaType;
        }

        public ISoapPackageLog LoggerInstance { get; set; }

        private string theContentType;
        private SMEVMessageEncoderFactory theFactory;

        private const string Soap11Namespace = "http://schemas.xmlsoap.org/soap/envelope/";


        public override string ContentType
        {
            get
            {
                return theContentType;
            }
        }

        public override string MediaType
        {
            get
            {
                return theFactory.MediaType;
            }
        }

        public override MessageVersion MessageVersion
        {
            get
            {
                return theFactory.MessageVersion;
            }
        }

        public override T GetProperty<T>()
        {
            return theFactory.InnerFactory.Encoder.GetProperty<T>();
        }

        private Correlation CorrelationObject = null;

        public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
        {
            var msgContents = new byte[buffer.Count];
            Array.Copy(buffer.Array, buffer.Offset, msgContents, 0, msgContents.Length);
            var messageStr = new UTF8Encoding().GetString(msgContents);

            var xmlDocument = new XmlDocument();
            using (var reader = XmlReader.Create(new MemoryStream(msgContents)))
                xmlDocument.Load(reader);

            var elementsByTagName = xmlDocument.GetElementsByTagName("Envelope", Soap11Namespace);
            if (elementsByTagName.Count == 0)
                throw new XmlException("Не найден узел Envelope");
            var prefixOfNamespace = elementsByTagName[0].GetPrefixOfNamespace(Soap11Namespace);
            if (string.IsNullOrEmpty(prefixOfNamespace))
                throw new XmlException(string.Format("Не найден префикс пространста имен {0}", Soap11Namespace));

            #region Логирование

            try
            {
                if (LoggerInstance != null)
                {
                    if (CorrelationObject == null)
                    {
                        CorrelationObject = new Correlation();
                        CorrelationObject.Direction = Dir.ServiceDirection;
                        CorrelationObject.MessageID = Guid.NewGuid();

                        //CorrelationObject.Action = SoapMessageInspector.Headers.Action;
                        //CorrelationObject.From = SoapMessageInspector.Headers.From;
                        //CorrelationObject.To = SoapMessageInspector.Headers.To;
                    }

                    StringBuilder sb = new StringBuilder();
                    using (var xw = XmlWriter.Create(sb, new XmlWriterSettings() { OmitXmlDeclaration = true }))
                        xmlDocument.WriteTo(xw);


                    var sp = new SoapPackage(
                        Action: CorrelationObject.Action,
                        Message: sb.ToString(),
                        Direction: DirectionMessage.Receive,
                        To: CorrelationObject.To,
                        From: CorrelationObject.From,
                        MessageID: CorrelationObject.MessageID);

                    ExecLogThreadHelper.WriteLog(LoggerInstance, sp);
                }
            }
            catch
            {
                // TODO залогировать исключение
            }
            finally
            {
                if (CorrelationObject != null && CorrelationObject.Direction == Dir.ClientDirection)
                    CorrelationObject = null;
            }

            #endregion

            messageStr = messageStr.Replace(prefixOfNamespace + ":mustUnderstand=\"1\"", "");
            messageStr = messageStr.Replace(prefixOfNamespace + ":actor=\"" + theFactory.SenderActor + "\"", "");
            messageStr = messageStr.Replace(prefixOfNamespace + ":actor=\"" + theFactory.RecipientActor + "\"", "");
            var bytes = new UTF8Encoding().GetBytes(messageStr);
            var length = bytes.Length;
            var array = bufferManager.TakeBuffer(length);
            Array.Copy(bytes, 0, array, 0, length);
            buffer = new ArraySegment<byte>(array, 0, length);

            return theFactory.InnerFactory.Encoder.ReadMessage(buffer, bufferManager, contentType);
        }

        public override Message ReadMessage(System.IO.Stream stream, int maxSizeOfHeaders, string contentType)
        {
            return theFactory.InnerFactory.Encoder.ReadMessage(stream, maxSizeOfHeaders, contentType);
        }

        public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
        {
            var arraySegment = theFactory.InnerFactory.Encoder.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
            var buffer1 = new byte[arraySegment.Count];
            Array.Copy(arraySegment.Array, arraySegment.Offset, buffer1, 0, buffer1.Length);

            var xmlDocument = new XmlDocument();
            using (var reader = XmlReader.Create(new MemoryStream(buffer1)))
                xmlDocument.Load(reader);

            var prefixOfNamespace = xmlDocument.FirstChild.GetPrefixOfNamespace(Soap11Namespace);
            if (string.IsNullOrEmpty(prefixOfNamespace))
                throw new XmlException(string.Format("Не найден префикс пространста имен {0}", Soap11Namespace));
            var elementsByTagName = xmlDocument.GetElementsByTagName("Security", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
            if (elementsByTagName.Count == 1)
            {
                // Добавляем actor в исходящие сообщение
                var attribute = xmlDocument.CreateAttribute(prefixOfNamespace + ":actor", Soap11Namespace);
                attribute.Value = theFactory.SenderActor;
                var xmlAttributeCollection = elementsByTagName[0].Attributes;
                if (xmlAttributeCollection != null)
                {
                    xmlAttributeCollection.Append(attribute);
                }
            }
            var memoryStream = new MemoryStream();
            using (var w = XmlWriter.Create(memoryStream, new XmlWriterSettings()
                {
                    OmitXmlDeclaration = true,
                    Encoding = new UTF8Encoding(false)
                }))
                xmlDocument.Save(w);

            #region логирование

            try
            {
                if (LoggerInstance != null)
                {
                    if (CorrelationObject == null)
                    {
                        CorrelationObject = new Correlation();
                        CorrelationObject.Direction = Dir.ClientDirection;
                        CorrelationObject.MessageID = Guid.NewGuid();
                        CorrelationObject.From = "Client";
                        CorrelationObject.To = message.Properties.Via;
                        CorrelationObject.Action = OperationAction.Action;
                    }

                    StringBuilder sb = new StringBuilder();
                    using (var xw = XmlWriter.Create(sb, new XmlWriterSettings() { OmitXmlDeclaration = true }))
                        xmlDocument.WriteTo(xw);

                    var sp = new SoapPackage(
                        Action: CorrelationObject.Action,
                        Message: sb.ToString(),
                        Direction: DirectionMessage.Send,
                        To: CorrelationObject.To,
                        From: CorrelationObject.From,
                        MessageID: CorrelationObject.MessageID);

                    ExecLogThreadHelper.WriteLog(LoggerInstance, sp);
                }
            }
            catch
            {
                // TODO залогировать исключение
            }
            finally
            {
                if (CorrelationObject != null && CorrelationObject.Direction == Dir.ServiceDirection)
                    CorrelationObject = null;
            }

            #endregion

            var buffer2 = memoryStream.GetBuffer();
            var num = (int)memoryStream.Position;
            memoryStream.Close();
            var bufferSize = num + messageOffset;
            var array = bufferManager.TakeBuffer(bufferSize);
            Array.Copy(buffer2, 0, array, messageOffset, num);
            return new ArraySegment<byte>(array, messageOffset, num);
        }

        public override void WriteMessage(Message message, System.IO.Stream stream)
        {
            theFactory.InnerFactory.Encoder.WriteMessage(message, stream);
        }

        public override bool IsContentTypeSupported(string contentType)
        {
            return base.IsContentTypeSupported(contentType);
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

        internal SMEVMessageEncoderFactory(string mediaType, string charSet, MessageVersion version, MessageEncoderFactory messageFactory, string SenderActor, string RecipientActor)
        {
            theFactory = messageFactory;
            theVersion = version;
            theMediaType = mediaType;
            theCharSet = charSet;
            this.SenderActor = SenderActor;
            this.RecipientActor = RecipientActor;
            theEncoder = new SMEVMessageEncoder(this);
        }

        public void SetEvents(ISoapPackageLog LoggerInstance)
        {
            theEncoder.LoggerInstance = LoggerInstance;
        }

        private MessageEncoderFactory theFactory;
        private SMEVMessageEncoder theEncoder;
        private MessageVersion theVersion;
        private string theMediaType;
        private string theCharSet;

        internal MessageEncoderFactory InnerFactory
        {
            get
            {
                return this.theFactory;
            }
        }

        public override MessageEncoder Encoder
        {
            get
            {
                return theEncoder;
            }
        }

        public override MessageVersion MessageVersion
        {
            get
            {
                return theVersion;
            }
        }

        public string SenderActor { get; private set; }
        public string RecipientActor { get; private set; }



        public string MediaType
        {
            get
            {
                return theMediaType;
            }
        }

        public string CharSet
        {
            get
            {
                return theCharSet;
            }
        }
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
        protected Type ImplemetationServiceType;
        /// <summary>
        /// Сертификат сервера
        /// </summary>
        protected X509Certificate2 CertificateServer;
        /// <summary>
        /// Набор алгоритмов (Типа криптопрошных...)
        /// </summary>
        protected SecurityAlgorithmSuite AlgorithmSuite;
        /// <summary>
        /// actor в исходящем сообщении
        /// </summary>
        protected String SenderActor = null;
        /// <summary>
        /// actor в входящем сообщении
        /// </summary>
        protected String RecipientActor = null;

        /// <summary>
        /// Создание нового экземпляра хоста службы
        /// </summary>
        /// <param name="t"></param>
        /// <param name="baseAddresses"></param>
        /// <returns></returns>
        protected override ServiceHost CreateServiceHost(Type t, Uri[] baseAddresses)
        {
            if (ImplemetationServiceType == null)
                throw new Exception("Не определен класс реализации сервиса");
            if (CertificateServer == null)
                throw new Exception("Не определен сертификат сервера");
            if (AlgorithmSuite == null)
                throw new Exception("Не определен набор алгоритмов");

            return new SmevServiceHost(ImplemetationServiceType, baseAddresses, AlgorithmSuite, CertificateServer, SenderActor, RecipientActor);
        }

        /// <summary>
        /// Создание нового экземпляра хоста службы
        /// </summary>
        /// <param name="constructorString"></param>
        /// <param name="baseAddresses"></param>
        /// <returns></returns>
        public override ServiceHostBase CreateServiceHost(string constructorString, Uri[] baseAddresses)
        {
            return this.CreateServiceHost((Type)null, baseAddresses);
        }
    }

    /// <summary>
    /// Кастомизированный хост сервиса
    /// </summary>
    internal class SmevServiceHost : ServiceHost
    {
        /// <summary>
        /// Экземпляр хоста
        /// </summary>
        /// <param name="IS">Тип класа, реализующего сервис</param>
        /// <param name="uri">Uri базовых адресов</param>
        /// <param name="AlgorithmSuite">Набор алгоритмов</param>
        /// <param name="CertificateServer">Сертификат сервера</param>
        /// <param name="SenderActor">actor отправителя сообщения</param>
        /// <param name="RecipientActor">actor получателя сообщения</param>
        internal SmevServiceHost(Type IS, Uri[] uri, SecurityAlgorithmSuite AlgorithmSuite, X509Certificate2 CertificateServer, String SenderActor, String RecipientActor)
            : base(IS, uri)
        {
            algorithmSuite = AlgorithmSuite;
            сertificateServer = CertificateServer;
            senderActor = SenderActor;
            recipientActor = RecipientActor;
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
        private String senderActor = null;
        /// <summary>
        /// actor в принимаемом сообщении
        /// </summary>
        private String recipientActor = null;

        /// <summary>
        /// Добавление конечной точки по умолчанию. Вызывается IIS, если в конфиге не указать конечную точку
        /// </summary>
        /// <returns></returns>
        public override System.Collections.ObjectModel.ReadOnlyCollection<ServiceEndpoint> AddDefaultEndpoints()
        {
            List<ServiceEndpoint> lse = new List<ServiceEndpoint>();

            var cd = this.ImplementedContracts.Values.First();

            ISoapPackageLog logger = null;
            try
            {
                if (this.Description.ServiceType.GetInterfaces().Any(x => x == typeof(ISoapPackageLog)))
                    logger = (ISoapPackageLog)Activator.CreateInstance(this.Description.ServiceType);
            }
            catch { } // Ну, не судьба....

            foreach (var item in this.BaseAddresses)
            {
                var sep = this.AddServiceEndpoint(cd.ContractType, SmevBinding.Create(
                    AlgorithmSuite: algorithmSuite,
                    LoggerInstance: logger,
                    SenderActor: senderActor,
                    RecipientActor: recipientActor), "");
                sep.Contract.ProtectionLevel = System.Net.Security.ProtectionLevel.Sign;
                sep.Behaviors.Add(new SoapLogEndpointBehavior());
                lse.Add(sep);
            }

            this.Credentials.ServiceCertificate.Certificate = сertificateServer;
            this.Credentials.ClientCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.None;
            this.Credentials.ClientCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;

            this.Description.Behaviors.Add(new ServiceMetadataBehavior() { HttpGetEnabled = true });

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

