using System;
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
        /// Приведение к строке для записи в БД. А то там вообще не понятно
        /// </summary>
        /// <param name="dm"></param>
        /// <returns></returns>
        public static String ToStr(this DirectionMessage dm)
        {
            if (dm == DirectionMessage.Receive)
                return "R";
            else
                return "S";
        }

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
        /// <returns>Обертка с клиентом</returns>
        public static WrapClient<T> CreateClient<T>(
            String Uri,
            SecurityAlgorithmSuite AlgorithmSuite,
            X509Certificate2 ClientSert,
            X509Certificate2 ServerSert = null,
            Type LoggerType = null,
            ISoapPackageLog LoggerInstanse = null,
            Boolean EnableUnsecuredResponse = false,
            String SenderActor = null,
            String RecipientActor = null)
            where T : ICommunicationObject, IDisposable
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
                LoggerInstance: logger,
                EnableUnsecuredResponse: EnableUnsecuredResponse,
                SenderActor: SenderActor,
                RecipientActor: RecipientActor);

            T client = (T)Activator.CreateInstance(typeof(T), binding, ea);

            ClientCredentials cc = (ClientCredentials)client.GetType().GetProperty("ClientCredentials").GetValue(client, null);
            cc.ClientCertificate.Certificate = ClientSert;
            cc.ServiceCertificate.DefaultCertificate = ServerSert;
            cc.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.None;
            cc.ServiceCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;

            // сообщение только подписываем. не шифруя(по умолчанию - и шифруется)

            ChannelFactory channelFactory = (ChannelFactory)client.GetType().GetProperty("ChannelFactory").GetValue(client, null);
            channelFactory.Endpoint.Contract.ProtectionLevel = System.Net.Security.ProtectionLevel.Sign;
            channelFactory.Endpoint.Behaviors.Add(new SoapLogEndpointBehavior());

            return new WrapClient<T>(client);
        }

        /// <summary>
        /// Создание клиента в обертке. На базе BasicHttpBinding. 
        /// Если клиент реализует ISoapPackageLog, то его экземпляр используется для логгирования.
        /// </summary>
        /// <typeparam name="T">Тип клиента, наследник от <c>ClientBase&lt;TChannel&gt;</c></typeparam>
        /// <param name="Uri">Uri сервиса</param>
        /// <param name="LoggerType">Тип логгера</param>
        /// <param name="LoggerInstanse">Экземпляр логгера</param>
        /// <returns></returns>
        public static WrapClient<T> CreateClient<T>(
            String Uri,
            Type LoggerType = null,
            ISoapPackageLog LoggerInstanse = null)
            where T : ICommunicationObject, IDisposable
        {
            var ea = new EndpointAddress(Uri);
            var binding = new BasicHttpBinding() { MaxReceivedMessageSize = int.MaxValue };
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

            ServiceEndpoint se = (ServiceEndpoint)client.GetType().GetProperty("Endpoint").GetValue(client, null);
            se.Behaviors.Add(new SoapLogEndpointBehavior(logger));

            return new WrapClient<T>(client);
        }
    }
}
