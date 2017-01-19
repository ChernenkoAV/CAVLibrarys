using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Security;

namespace Cav.Soap
{
    /// <summary>
    /// Хелпер для работы с сервисами/клиентами веб-служб
    /// </summary>
    public static class SoapHelper
    {
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
        /// <param name="Uri">Uri сервиса</param>
        /// <param name="ClientSert">Клиентский сертификат</param>
        /// <param name="ServerSert">Серверный сертификат</param>
        /// <param name="AlgorithmSuite">Алгоритм подписи</param>
        /// <param name="LoggerType">Тип логгера</param>
        /// <param name="LoggerInstanse">Экземпляр логгера(приоритетнее при указании и экземпляра и типа логгера)</param>
        /// <param name="EnableUnsecuredResponse">Задает значение, указывающее, может ли отправлять и получать небезопасные ответы или безопасные запросы.</param>
        /// <param name="SenderActor">Actor отправителя</param>
        /// <param name="RecipientActor">Actor получателя</param>
        /// <param name="Proxy">Прокси для клиента</param>
        /// <param name="SendTimeout">Таймаут работы клиента</param>
        /// <param name="AllowInsecureTransport">Можно ли отправлять сообщения в смешанном режиме безопасности</param>
        /// <returns>Обертка с клиентом</returns>
        public static WrapClient<T> CreateSmevClient<T>(
            String Uri,
            SecurityAlgorithmSuite AlgorithmSuite,
            X509Certificate2 ClientSert,
            X509Certificate2 ServerSert = null,
            Type LoggerType = null,
            ISoapPackageLog LoggerInstanse = null,
            Boolean EnableUnsecuredResponse = false,
            String SenderActor = null,
            String RecipientActor = null,
            String Proxy = null,
            TimeSpan? SendTimeout = null,
            Boolean AllowInsecureTransport = false)
            where T : class, ICommunicationObject, IDisposable
        {

            if (ServerSert == null)
                ServerSert = ClientSert;

            EndpointAddress ea = new EndpointAddress(new Uri(Uri), DnsEndpointIdentity.CreateDnsIdentity(ServerSert.GetNameInfo(X509NameType.SimpleName, false)));

            ISoapPackageLog logger = LoggerInstanse;

            if (logger == null)
                try
                {
                    logger = (ISoapPackageLog)Activator.CreateInstance(LoggerType);
                }
                catch { }

            var binding = SmevBinding.Create(
                AlgorithmSuite: AlgorithmSuite,
                Proxy: Proxy,
                LoggerInstance: logger,
                SenderActor: SenderActor,
                RecipientActor: RecipientActor,
                EnableUnsecuredResponse: EnableUnsecuredResponse,
                AllowInsecureTransport: AllowInsecureTransport);

            if (SendTimeout.HasValue)
                binding.SendTimeout = SendTimeout.Value;

            T client = (T)Activator.CreateInstance(typeof(T), binding, ea);

            ClientCredentials cc = (ClientCredentials)client.GetType().GetProperty("ClientCredentials").GetValue(client, null);
            if (ClientSert == null)
                throw new NullReferenceException("Не указан клиентский сертификат. Либо не найден в хранилище, либо строка с BASE64 невалидна.");
            cc.ClientCertificate.Certificate = ClientSert;
            if (ServerSert == null)
                throw new NullReferenceException("Не указан серверный сертификат. Либо не найден в хранилище, либо строка с BASE64 невалидна.");
            cc.ServiceCertificate.DefaultCertificate = ServerSert;
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
        /// <param name="Uri">Uri сервиса</param>
        /// <param name="Proxy">Прокси для клиента</param>
        /// <param name="LoggerType">Тип логгера</param>
        /// <param name="LoggerInstanse">Экземпляр логгера</param>
        /// <param name="SendTimeout">Таймаут посыла сообщения</param>
        /// <returns></returns>
        public static WrapClient<T> CreateBasicClient<T>(
            String Uri,
            String Proxy = null,
            Type LoggerType = null,
            ISoapPackageLog LoggerInstanse = null,
            TimeSpan? SendTimeout = null)
            where T : class, ICommunicationObject, IDisposable
        {
            var ea = new EndpointAddress(Uri);

            var binding = new BasicHttpBinding() { MaxReceivedMessageSize = int.MaxValue };
            if (!Proxy.IsNullOrWhiteSpace())
            {
                binding.ProxyAddress = new Uri(Proxy);
                binding.UseDefaultWebProxy = false;
            }

            if (SendTimeout.HasValue)
                binding.SendTimeout = SendTimeout.Value;

            T client = (T)Activator.CreateInstance(typeof(T), binding, ea);

            ISoapPackageLog logger = LoggerInstanse;
            if (logger == null)
                logger = client as ISoapPackageLog;
            if (logger == null)
                try
                {
                    logger = (ISoapPackageLog)Activator.CreateInstance(LoggerType);
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
    }
}
