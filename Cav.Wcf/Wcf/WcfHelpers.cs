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
using Cav.ReflectHelpers;

namespace Cav.Wcf
{
    /// <summary>
    /// Хелпер для работы с сервисами/клиентами веб-служб
    /// </summary>
    public static class WcfHelpers
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
                throw new ArgumentNullException(nameof(xmlNamespaces));

            if (!xmlNamespaces.Any())
                throw new ArgumentException("xmlNamespaces empty");

            if (communicationObject == null)
                throw new ArgumentNullException(nameof(communicationObject));

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
                ServiceEndpoint ep = (ServiceEndpoint)communicationObject.GetPropertyValue("Endpoint");
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
                throw new ArgumentNullException(nameof(errorHandler));

            if (servhost == null)
                throw new ArgumentNullException(nameof(servhost));

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
                throw new ArgumentNullException(nameof(communicationObject));

            if (beforeCall == null & afterCall == null)
                throw new ArgumentNullException("Parametrs 'beforeCall' and 'afterCall' is null ");

            var psi = new ServiceParameterInspector(beforeCall, afterCall);

            if ((communicationObject as ServiceHostBase) != null)
            {
                var servhost = (communicationObject as ServiceHostBase);
                foreach (var ep in servhost.Description.Endpoints)
                    ep.Behaviors.Add(psi);
            }
            else
            {
                ServiceEndpoint ep = (ServiceEndpoint)communicationObject.GetPropertyValue("Endpoint");
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
                throw new ArgumentNullException(nameof(servHost));
            if (instanceProvider == null)
                throw new ArgumentNullException(nameof(instanceProvider));
            servHost.Description.Behaviors.Add(new ServiceInstanceProvider(instanceProvider));
        }

        internal const string Soap11Namespace = "http://schemas.xmlsoap.org/soap/envelope/";

        /// <summary>
        /// Добавление логрера сообщений к комуникационным объектам WCF
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="communicationObject"></param>
        /// <param name="actionLog"></param>
        public static void AddMessageLogger<T>(
            this T communicationObject,
            Action<MessageLogData> actionLog)
            where T : ICommunicationObject
        {
            if (communicationObject == null)
                throw new ArgumentNullException(nameof(communicationObject));

            if (actionLog == null)
                throw new ArgumentNullException(nameof(actionLog));

            var endPoints = new List<ServiceEndpoint>();

            var sh = communicationObject as ServiceHost;
            if (sh != null)
                endPoints.AddRange(sh.Description.Endpoints);
            else
                endPoints.Add((ServiceEndpoint)communicationObject.GetType().GetProperty(nameof(ClientBase<Object>.Endpoint)).GetValue(communicationObject));

            foreach (var ep in endPoints)
                ep.EndpointBehaviors.Add(new LogMessageCaller(actionLog));
        }

        /// <summary>
        /// Создание клиента в обертке в котором сообщение подписывается(На базе биндинга для СМЭВ)
        /// </summary>
        /// <typeparam name="T">Тип клиента, наследник от <c>ClientBase&lt;TChannel&gt;</c></typeparam>
        /// <param name="uri">Uri сервиса</param>
        /// <param name="clientSert">Клиентский сертификат</param>
        /// <param name="serverSert">Серверный сертификат</param>
        /// <param name="algorithmSuite">Если null, то берется <see cref="CryptoProRutine.CriptoProBasicGostObsolete"/></param>
        /// <param name="loggerType">Тип логгера</param>        
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

            if (algorithmSuite == null)
                algorithmSuite = CryptoProRutine.CriptoProBasicGostObsolete;

            var binding = SmevBinding.Create(
                algorithmSuite: algorithmSuite,
                proxy: proxy,
                senderActor: senderActor,
                recipientActor: recipientActor,
                enableUnsecuredResponse: enableUnsecuredResponse,
                allowInsecureTransport: allowInsecureTransport);

            if (sendTimeout.HasValue)
                binding.SendTimeout = sendTimeout.Value;

            T client = (T)Activator.CreateInstance(typeof(T), binding, ea);

            ClientCredentials cc = (ClientCredentials)client.GetPropertyValue("ClientCredentials");
            if (clientSert == null)
                throw new NullReferenceException("Не указан клиентский сертификат. Либо не найден в хранилище, либо строка с BASE64 невалидна.");
            cc.ClientCertificate.Certificate = clientSert;
            if (serverSert == null)
                throw new NullReferenceException("Не указан серверный сертификат. Либо не найден в хранилище, либо строка с BASE64 невалидна.");
            cc.ServiceCertificate.DefaultCertificate = serverSert;
            cc.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.None;
            cc.ServiceCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;

            // сообщение только подписываем. не шифруя(по умолчанию - и шифруется)

            ChannelFactory channelFactory = (ChannelFactory)client.GetPropertyValue("ChannelFactory");
            channelFactory.Endpoint.Contract.ProtectionLevel = System.Net.Security.ProtectionLevel.Sign;

            // в NET461 по умолчанию отключили доверенность в многоименным сертификатам и включили проверку. Как то так.
            AppContext.SetSwitch("Switch.System.IdentityModel.DisableMultipleDNSEntriesInSANCertificate", true);


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
        /// <param name="sendTimeout">Таймаут посыла сообщения</param>
        /// <param name="credentialsUserName">Логин</param>
        /// <param name="credentialsUserPass">Пароль</param>
        /// <returns></returns>
        public static WrapClient<T> CreateBasicClient<T>(
            String uri,
            String proxy = null,
            Type loggerType = null,
            TimeSpan? sendTimeout = null,
            string credentialsUserName = null,
            string credentialsUserPass = null)
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

            if (!credentialsUserName.IsNullOrWhiteSpace())
            {
                var сlientCredentials = (ClientCredentials)client.GetPropertyValue("ClientCredentials");

                сlientCredentials.UserName.UserName = credentialsUserName;
                сlientCredentials.UserName.Password = credentialsUserPass;
            }

            return new WrapClient<T>(client);
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
