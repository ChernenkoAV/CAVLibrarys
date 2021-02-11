using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Xml.Linq;
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

            if (typeof(ClientBase<>).IsAssignableFrom(communicationObject.GetType()))
                endPoints.Add((ServiceEndpoint)communicationObject.GetType().GetProperty(nameof(ClientBase<Object>.Endpoint)).GetValue(communicationObject));

            foreach (var ep in endPoints)
                ep.EndpointBehaviors.Add(new LogMessageCaller(actionLog));
        }
    }
}
