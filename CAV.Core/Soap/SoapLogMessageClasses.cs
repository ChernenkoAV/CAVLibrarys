using System;
using System.Collections.ObjectModel;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Xml;
using Cav.Wcf;

namespace Cav.Soap
{
    /// <summary>
    /// Передача данных между вызовами 
    /// </summary>
    internal class Correlation
    {
        public String Action { get; set; }
        public Uri To { get; set; }
        public String From { get; set; }
        public Guid MessageID { get; set; }
        public Dir Direction { get; set; }
    }

    internal enum Dir
    {
        Hz = 0,
        ClientDirection = 1,
        ServiceDirection = 2
    }

    internal class OperationAction : IParameterInspector
    {

        public OperationAction(ClientOperation Operation)
        {
            action = Operation.Action;
            if (action.IsNullOrWhiteSpace())
                action = Operation.Name;
        }

        public OperationAction(DispatchOperation Operation)
        {
            action = Operation.Action;
            if (action.IsNullOrWhiteSpace())
                action = Operation.Name;
        }


        [ThreadStatic]
        public static String Action;

        private String action;

        #region Члены IParameterInspector

        public void AfterCall(string operationName, object[] outputs, object returnValue, object correlationState)
        {
            Action = action;
        }

        public object BeforeCall(string operationName, object[] inputs)
        {
            Action = action;
            return null;
        }

        #endregion
    }


    internal class SoapMessageInspector : IDispatchMessageInspector, IClientMessageInspector
    {
        internal SoapMessageInspector(ServiceHostBase serviceHost)
        {
            try // на тот случай, если умудрились сделать конструктор службы с параметрами...
            {
                implementationLog = Activator.CreateInstance(serviceHost.Description.ServiceType) as ISoapPackageLog;
            }
            catch { }
        }

        internal SoapMessageInspector(ISoapPackageLog loggerInstanse)
        {
            implementationLog = loggerInstanse;
        }

        ISoapPackageLog implementationLog = null;

        #region IDispatchMessageInspector

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {

            var corObj = new Correlation();

            // посмотреть, что за хост в instanceContext.
            // если WebServiceHost, то вытянуть тип операции (GET, POST и тд.), а также uri запроса
            // Иначе выдернуть в ServiceHost имя орепации.

            // Тело Message получить через GetBody<String>()

            corObj.Action = request.Headers.Action;
            corObj.To = request.Headers.To;
            try
            {
                corObj.From = ((RemoteEndpointMessageProperty)OperationContext.Current.IncomingMessageProperties["RemoteEndpointMessageProperty.Name"]).Address;
            }
            catch // Не получилось.. Ну и ладно. а жаль...
            { }

            //if (implementationLog == null)
            return corObj;

            //Correlation CorrelationObject = Headers;
            //MessageBuffer buff = null;

            //try
            //{

            //    buff = request.CreateBufferedCopy(int.MaxValue);

            //    CorrelationObject.MessageID = Guid.NewGuid();               

            //    StringBuilder sb = new StringBuilder();

            //    using (var sw = new StringWriter(sb))
            //    using (var xtw = new XmlTextWriter(sw))
            //        buff.CreateMessage().WriteMessage(xtw);

            //    var sp = new SoapPackage(
            //        Action: CorrelationObject.Action,
            //        Message: sb.ToString(),
            //        Direction: DirectionMessage.Receive,
            //        To: CorrelationObject.To,
            //        From: CorrelationObject.From,
            //        MessageID: CorrelationObject.MessageID);

            //    ExecLogThreadHelper.WriteLog(implementationLog, sp);
            //}
            //catch
            //{
            //    
            //    // прячем ошибку
            //}
            //finally
            //{
            //    request = buff.CreateMessage();
            //}

            //return CorrelationObject;
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            //File.AppendAllText(@"d:\br.txt", "reply != null" + (reply != null).ToString() + Environment.NewLine);


            //if (implementationLog == null || correlationState == null)
            //    return;

            //Correlation CorrelationObject = (Correlation)correlationState;
            //MessageBuffer buff = null;

            //try
            //{

            //    buff = reply.CreateBufferedCopy(int.MaxValue);

            //    StringBuilder sb = new StringBuilder();

            //    using (var sw = new StringWriter(sb))
            //    using (var xtw = new XmlTextWriter(sw))
            //        buff.CreateMessage().WriteMessage(xtw);

            //    var sp = new SoapPackage(
            //            Action: CorrelationObject.Action,
            //            Message: sb.ToString(),
            //            Direction: DirectionMessage.Send,
            //            To: CorrelationObject.To,
            //            From: CorrelationObject.From,
            //            MessageID: CorrelationObject.MessageID);

            //    ExecLogThreadHelper.WriteLog(implementationLog, sp);
            //}
            //catch
            //{
            //    
            //}
            //finally
            //{
            //    reply = buff.CreateMessage();
            //}
        }

        #endregion

        #region IClientMessageInspector

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            if (implementationLog == null)
                return null;

            Correlation correlationObject = new Correlation();
            var buff = request.CreateBufferedCopy(int.MaxValue);
            request = buff.CreateMessage();
            var prRequest = buff.CreateMessage();
            buff.Close();

            try
            {
                correlationObject.MessageID = Guid.NewGuid();
                correlationObject.Action = OperationAction.Action;
                correlationObject.To = channel.RemoteAddress.Uri;
                correlationObject.From = "Client";

                StringBuilder sb = new StringBuilder();

                using (var sw = new StringWriter(sb))
                using (var xtw = new XmlTextWriter(sw))
                    prRequest.WriteMessage(xtw);

                var sp = new SoapPackage(
                    Action: correlationObject.Action,
                    Message: sb.ToString(),
                    Direction: DirectionMessage.Send,
                    To: correlationObject.To,
                    From: correlationObject.From,
                    MessageID: correlationObject.MessageID);

                ExecLogThreadHelper.WriteLog(implementationLog, sp);
            }
            catch { }

            return correlationObject;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            if (implementationLog == null || correlationState == null)
                return;

            Correlation CorrelationObject = (Correlation)correlationState;
            MessageBuffer buff = reply.CreateBufferedCopy(int.MaxValue);
            reply = buff.CreateMessage();
            var prRelpy = buff.CreateMessage();
            buff.Close();

            try
            {
                StringBuilder sb = new StringBuilder();

                using (var sw = new StringWriter(sb))
                using (var xtw = new XmlTextWriter(sw))
                    prRelpy.WriteMessage(xtw);

                var sp = new SoapPackage(
                        Action: CorrelationObject.Action,
                        Message: sb.ToString(),
                        Direction: DirectionMessage.Receive,
                        To: CorrelationObject.To,
                        From: CorrelationObject.From,
                        MessageID: CorrelationObject.MessageID);

                ExecLogThreadHelper.WriteLog(implementationLog, sp);
            }
            catch { }
        }

        #endregion
    }

    /// <summary>
    /// Выполняет привязку инспектора сообщений к конкретной реализации сервиса
    /// </summary>
    public class SoapPackageLoggingBehaviorAttribute : Attribute, IServiceBehavior
    {
        #region IServiceBehavior

        void IServiceBehavior.ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (ChannelDispatcher cDispatcher in serviceHostBase.ChannelDispatchers)
                foreach (EndpointDispatcher eDispatcher in cDispatcher.Endpoints)
                {
                    var edp = serviceDescription.Endpoints.Find(eDispatcher.EndpointAddress.Uri);
                    if (edp != null && edp.Binding is SmevBinding)
                        continue;

                    eDispatcher.DispatchRuntime.MessageInspectors.Add(new SoapMessageInspector(serviceHostBase));
                }
        }

        void IServiceBehavior.AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
        {

        }

        void IServiceBehavior.Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {

        }

        #endregion
    }

    /// <summary>
    /// Кастомный обработчик поведения конечной точки. Для добавления испектора
    /// </summary>
    internal class SoapLogEndpointBehavior : IEndpointBehavior
    {
        public SoapLogEndpointBehavior(ISoapPackageLog Logger = null)
        {
            logger = Logger;
        }

        ISoapPackageLog logger = null;

        #region IEndpointBehavior

        void IEndpointBehavior.ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            if (logger != null)
            {
                var inspector = new SoapMessageInspector(logger);
                endpointDispatcher.DispatchRuntime.MessageInspectors.Add(inspector);
            }

            foreach (DispatchOperation op in endpointDispatcher.DispatchRuntime.Operations)
                op.ParameterInspectors.Add(new OperationAction(op));
        }

        void IEndpointBehavior.AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {

        }

        void IEndpointBehavior.ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            foreach (var operation in clientRuntime.Operations)
                operation.ParameterInspectors.Add(new OperationAction(operation));

            if (logger != null)
                clientRuntime.MessageInspectors.Add(new SoapMessageInspector(logger));
        }

        void IEndpointBehavior.Validate(ServiceEndpoint endpoint)
        {

        }

        #endregion
    }
}
