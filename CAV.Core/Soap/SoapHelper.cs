using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Security;
using System.Xml.Linq;
using Cav.DigitalSignature;

namespace Cav.Soap
{
    /// <summary>
    /// Хелпер для работы с сервисами/клиентами веб-служб
    /// </summary>
    public static class SoapHelper
    {
        /// <summary>
        /// Фиксация префиксов постранств имен в сообщении SOAP. Прописываются в элементе пакета и тела
        /// </summary>
        /// <param name="communicationObject">Объект коомуникации</param>
        /// <param name="xmlNamespaces">Набор значений "префикс - пространство имен"</param>
        public static void FixatePrefixNamespace<T>(
                this T communicationObject,
                Dictionary<String, XNamespace> xmlNamespaces)
                where T : class, ICommunicationObject, IDisposable
        {
            if (xmlNamespaces == null)
                throw new ArgumentNullException("xmlNamespaces");
            if (!xmlNamespaces.Any())
                throw new ArgumentException("xmlNamespaces empty");
            if (communicationObject == null)
                throw new ArgumentNullException("communicationObject");

            Dictionary<String, String> nss = new Dictionary<string, string>();

            foreach (var item in xmlNamespaces)
            {
                if (item.Key == null)
                    throw new ArgumentException("prefix is null");
                if (item.Value == null)
                    throw new ArgumentException("namespace is null");
                nss.Add(item.Key, item.Value.NamespaceName);
            }

            var fmr = new FixatePrefixMessageFormatter(nss);

            if ((communicationObject as ServiceHostBase) != null)
            {
                var servhost = (communicationObject as ServiceHostBase);
                foreach (var ep in servhost.Description.Endpoints)
                    ep.Behaviors.Add(fmr);
            }
            else
            {
                ServiceEndpoint ep = (ServiceEndpoint)communicationObject.GetType().GetProperty("Endpoint").GetValue(communicationObject, null);
                ep.Behaviors.Add(fmr);
            }
        }


        /// <summary>
        /// Добавить обработчик ошибок выполнения методов сервиса
        /// </summary>
        /// <param name="servhost">Хост службы</param>
        /// <param name="errorHandler">Обработчик ошибок</param>
        public static void AddErrorHandler(this ServiceHostBase servhost, Action<Exception> errorHandler)
        {
            if (errorHandler == null)
                throw new ArgumentNullException("Parametr 'errorHandler' is null");
            if (servhost == null)
                throw new ArgumentNullException("Parametr 'servhost' is null");
            if (servhost.Description.Behaviors.Any(x => x.GetType() == typeof(ServiceErrorHandler)))
                return;
            servhost.Description.Behaviors.Add(new ServiceErrorHandler(errorHandler));
        }

        /// <summary>
        /// Добавление обработчиков "инспектора параметров".
        /// </summary>
        /// <param name="communicationObject">Экземпляр объекта комуникации</param>
        /// <param name="beforeCall">Функтор. Выполняется снхронно. Принимает входные параметры вызываемого метода. Должен вернуть объект кореляции между вызовами BeforeCall и AfterCall (correlationState) </param>
        /// <param name="afterCall">Делегат обработки, вызываемый после отработки метода сервиса. Принимает имя метода и объект кореляции (correlationState)</param>
        public static void AddOperationInspector<T>(
            this T communicationObject,
            Func<object[], object> beforeCall,
            Action<string, object> afterCall)
            where T : class, ICommunicationObject, IDisposable
        {
            if (communicationObject == null)
                throw new ArgumentNullException("Parametr 'communicationObject' is null");
            if (beforeCall == null & afterCall == null)
                throw new ArgumentNullException("Parametrs 'beforeCall' and 'afterCall' is null ");

            var psi = new SoapParameterInspector(beforeCall, afterCall);

            if ((communicationObject as ServiceHostBase) != null)
            {
                var servhost = (communicationObject as ServiceHostBase);
                foreach (var ep in servhost.Description.Endpoints)
                    ep.Behaviors.Add(psi);
            }
            else
            {
                ServiceEndpoint ep = (ServiceEndpoint)communicationObject.GetType().GetProperty("Endpoint").GetValue(communicationObject, null);
                ep.Behaviors.Add(psi);
            }
        }

        /// <summary>
        /// Добавление к службе провайдера экземпляров обектов обработчика службы
        /// </summary>
        /// <param name="servHost">Хост службы</param>
        /// <param name="instanceProvider">Экземпляр провайдера</param>
        public static void AddInstanceProvider(this ServiceHostBase servHost, IInstanceProvider instanceProvider)
        {
            if (servHost == null)
                throw new ArgumentNullException("Parametr 'servHost' is null");
            if (instanceProvider == null)
                throw new ArgumentNullException("Parametr 'instanceProvider' is null");
            servHost.Description.Behaviors.Add(new ServiceInstanceProvider(instanceProvider));
        }

        #region Константы

        internal const string Soap11Namespace = "http://schemas.xmlsoap.org/soap/envelope/";

        #endregion

        ///// <summary>
        ///// Для клиентов, написаных на базе System.Web.Services.Protocols.SoapHttpClientProtocol
        ///// </summary>
        //public static void Add(SoapHttpClientProtocol InstanseClient, Type TypeLogger)
        //{ 

        //}      

        /// <summary>
        /// Создание клиента в обертке в котором сообщение подписывается(На базе биндинга для СМЭВ)
        /// </summary>
        /// <typeparam name="T">Тип клиента, наследник от <c>ClientBase&lt;TChannel&gt;</c></typeparam>
        /// <param name="uri">Uri сервиса</param>
        /// <param name="clientSert">Клиентский сертификат</param>
        /// <param name="serverSert">Серверный сертификат</param>
        /// <param name="algorithmSuite">Если null, то берется <see cref="CryptoProRutine.CriptoProBasicGostObsolete"/></param>
        /// <param name="loggerType">Тип логгера</param>
        /// <param name="loggerInstanse">Экземпляр логгера(приоритетнее при указании и экземпляра и типа логгера)</param>
        /// <param name="enableUnsecuredResponse">Задает значение, указывающее, может ли отправлять и получать небезопасные ответы или безопасные запросы.</param>
        /// <param name="senderActor">Actor отправителя</param>
        /// <param name="recipientActor">Actor получателя</param>
        /// <param name="proxy">Прокси для клиента</param>
        /// <param name="sendTimeout">Таймаут работы клиента</param>
        /// <param name="allowInsecureTransport">Можно ли отправлять сообщения в смешанном режиме безопасности</param>
        /// <returns>Обертка с клиентом</returns>
        public static WrapClient<T> CreateSmevClient<T>(
            String uri,
            X509Certificate2 clientSert,
            X509Certificate2 serverSert = null,
            SecurityAlgorithmSuite algorithmSuite = null,
            Type loggerType = null,
            ISoapPackageLog loggerInstanse = null,
            Boolean enableUnsecuredResponse = false,
            String senderActor = null,
            String recipientActor = null,
            String proxy = null,
            TimeSpan? sendTimeout = null,
            Boolean allowInsecureTransport = false)
            where T : class, ICommunicationObject, IDisposable
        {

            if (serverSert == null)
                serverSert = clientSert;

            EndpointAddress ea = new EndpointAddress(new Uri(uri), DnsEndpointIdentity.CreateDnsIdentity(serverSert.GetNameInfo(X509NameType.SimpleName, false)));

            ISoapPackageLog logger = loggerInstanse;

            if (logger == null)
                try
                {
                    logger = (ISoapPackageLog)Activator.CreateInstance(loggerType);
                }
                catch { }

            if (algorithmSuite == null)
                algorithmSuite = CryptoProRutine.CriptoProBasicGostObsolete;

            var binding = SmevBinding.Create(
                algorithmSuite: algorithmSuite,
                proxy: proxy,
                loggerInstance: logger,
                senderActor: senderActor,
                recipientActor: recipientActor,
                enableUnsecuredResponse: enableUnsecuredResponse,
                allowInsecureTransport: allowInsecureTransport);

            if (sendTimeout.HasValue)
                binding.SendTimeout = sendTimeout.Value;

            T client = (T)Activator.CreateInstance(typeof(T), binding, ea);

            ClientCredentials cc = (ClientCredentials)client.GetType().GetProperty("ClientCredentials").GetValue(client, null);
            if (clientSert == null)
                throw new NullReferenceException("Не указан клиентский сертификат. Либо не найден в хранилище, либо строка с BASE64 невалидна.");
            cc.ClientCertificate.Certificate = clientSert;
            if (serverSert == null)
                throw new NullReferenceException("Не указан серверный сертификат. Либо не найден в хранилище, либо строка с BASE64 невалидна.");
            cc.ServiceCertificate.DefaultCertificate = serverSert;
            cc.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.None;
            cc.ServiceCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;

            // сообщение только подписываем. не шифруя(по умолчанию - и шифруется)

            ChannelFactory channelFactory = (ChannelFactory)client.GetType().GetProperty("ChannelFactory").GetValue(client, null);
            channelFactory.Endpoint.Contract.ProtectionLevel = System.Net.Security.ProtectionLevel.Sign;
            channelFactory.Endpoint.Behaviors.Add(new SoapLogEndpointBehavior());

#if NET461
            // в NET461 по умолчанию отключили доверенность в многоименным сертификатам и включили проверку. Как то так.
            AppContext.SetSwitch("Switch.System.IdentityModel.DisableMultipleDNSEntriesInSANCertificate", true);
#endif

            return new WrapClient<T>(client);
        }

        /// <summary>
        /// Создание клиента в обертке. На базе BasicHttpBinding. 
        /// Если клиент реализует ISoapPackageLog, то его экземпляр используется для логгирования.
        /// </summary>
        /// <typeparam name="T">Тип клиента, наследник от <c>ClientBase&lt;TChannel&gt;</c></typeparam>
        /// <param name="uri">Uri сервиса</param>
        /// <param name="proxy">Прокси для клиента</param>
        /// <param name="loggerType">Тип логгера</param>
        /// <param name="loggerInstanse">Экземпляр логгера</param>
        /// <param name="sendTimeout">Таймаут посыла сообщения</param>
        /// <returns></returns>
        public static WrapClient<T> CreateBasicClient<T>(
            String uri,
            String proxy = null,
            Type loggerType = null,
            ISoapPackageLog loggerInstanse = null,
            TimeSpan? sendTimeout = null)
            where T : class, ICommunicationObject, IDisposable
        {
            var ea = new EndpointAddress(uri);

            var binding = new BasicHttpBinding() { MaxReceivedMessageSize = int.MaxValue };
            if (!proxy.IsNullOrWhiteSpace())
            {
                binding.ProxyAddress = new Uri(proxy);
                binding.UseDefaultWebProxy = false;
            }

            if (sendTimeout.HasValue)
                binding.SendTimeout = sendTimeout.Value;

            T client = (T)Activator.CreateInstance(typeof(T), binding, ea);

            ISoapPackageLog logger = loggerInstanse;
            if (logger == null)
                logger = client as ISoapPackageLog;
            if (logger == null)
                try
                {
                    logger = (ISoapPackageLog)Activator.CreateInstance(loggerType);
                }
                catch { } // ну, не судьба

            if (logger != null)
            {
                ServiceEndpoint se = (ServiceEndpoint)client.GetType().GetProperty("Endpoint").GetValue(client, null);
                se.Behaviors.Add(new SoapLogEndpointBehavior(logger));
            }

            return new WrapClient<T>(client);
        }

        /// <summary>
        /// Корректное закрытие клиента
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        public static void CloseClient<T>(this T client) where T : class, ICommunicationObject, IDisposable
        {
            if (client == null)
                return;

            if (client.State == CommunicationState.Faulted)
            {
                client.Abort();
            }
            else if (client.State != CommunicationState.Closed)
            {
                client.Close();
            }

            ((IDisposable)client).Dispose();
        }

        /// <summary>
        /// Создание хоста web-службы СМЭВ
        /// </summary>
        /// <param name="implementType">Класс, реализующий контракт службы</param>
        /// <param name="endpointUri">Конечная точка службы</param>
        /// <param name="servSert">Серверный сертификат</param>
        /// <param name="contractType">Тип, определяющий контракт службы </param>        
        /// <param name="senderActor">Актор сервиса</param>
        /// <param name="recipientActor">Актор клиента</param>
        /// <param name="algorithmSuite">Если null, то берется <see cref="CryptoProRutine.CriptoProBasicGostObsolete"/></param>
        /// <returns></returns>
        public static ServiceHost CreateSmevHost(
            Type implementType,
            Uri endpointUri,
            String servSert,
            Type contractType = null,
            String senderActor = "http://smev.gosuslugi.ru/actors/recipient",
            String recipientActor = "http://smev.gosuslugi.ru/actors/smev",
            SecurityAlgorithmSuite algorithmSuite = null)
        {
            var cert = DSGeneric.FindCertByThumbprint(servSert);
            if (cert == null)
                throw new InvalidOperationException("Не найден сертификат");

            if (contractType == null)
                contractType = implementType;

            if (algorithmSuite == null)
                algorithmSuite = CryptoProRutine.CriptoProBasicGostObsolete;

            ServiceHost res = new ServiceHost(implementType);

            var sep = res.AddServiceEndpoint(
                implementedContract: contractType,
                binding: SmevBinding.Create(
                    algorithmSuite: algorithmSuite,
                    senderActor: senderActor,
                    recipientActor: recipientActor),
                address: endpointUri);

            res.Credentials.ServiceCertificate.Certificate = cert;
            res.Credentials.ClientCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.None;

            sep.Contract.ProtectionLevel = ProtectionLevel.Sign;

            return res;
        }
    }
}
