using System;
using System.Collections.ObjectModel;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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

    /// <summary>
    /// Хелпер для запуска метода логирования в потоке
    /// </summary>
    internal static class ExecLogThreadHelper
    {
        public static void WriteLog(ISoapPackageLog logger, SoapPackage p)
        {
            if (logger == null)
                return;
            (new Task(ac, Tuple.Create(logger, p))).Start();
        }

        private static void ac(Object a)
        {
            try
            {
                var par = (Tuple<ISoapPackageLog, SoapPackage>)a;
                par.Item1.ActionLog(par.Item2);
            }
            catch
            { }
        }
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
        [ThreadStatic]
        public static Correlation Headers;

        internal SoapMessageInspector(ServiceHostBase ServiceHost)
        {
            try // на тот случай, если умудрились сделать конструктор службы с параметрами...
            {
                implementationLog = Activator.CreateInstance(ServiceHost.Description.ServiceType) as ISoapPackageLog;
            }
            catch { }
        }

        internal SoapMessageInspector(ISoapPackageLog LoggerInstanse)
        {
            implementationLog = LoggerInstanse;
        }

        ISoapPackageLog implementationLog = null;

        #region IDispatchMessageInspector

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            //File.AppendAllText(@"d:\br.txt", " request != null" + (request != null).ToString() + Environment.NewLine);


            Headers = new Correlation();
            Headers.Action = request.Headers.Action;
            Headers.To = request.Headers.To;
            try
            {
                Headers.From = ((RemoteEndpointMessageProperty)OperationContext.Current.IncomingMessageProperties["RemoteEndpointMessageProperty.Name"]).Address;
            }
            catch // Не получилось.. Ну и ладно. а жаль...
            { }

            //if (implementationLog == null)
            return null;

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
            //    //TODO Сделать логгер ошибок
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
            //    // TODO Вставить логгер ошибок
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

            Correlation CorrelationObject = new Correlation();
            MessageBuffer buff = null;

            try
            {

                buff = request.CreateBufferedCopy(int.MaxValue);

                CorrelationObject.MessageID = Guid.NewGuid();
                CorrelationObject.Action = OperationAction.Action;
                CorrelationObject.To = channel.RemoteAddress.Uri;
                CorrelationObject.From = "Client";

                StringBuilder sb = new StringBuilder();

                using (var sw = new StringWriter(sb))
                using (var xtw = new XmlTextWriter(sw))
                    buff.CreateMessage().WriteMessage(xtw);

                var sp = new SoapPackage(
                    Action: CorrelationObject.Action,
                    Message: sb.ToString(),
                    Direction: DirectionMessage.Send,
                    To: CorrelationObject.To,
                    From: CorrelationObject.From,
                    MessageID: CorrelationObject.MessageID);

                ExecLogThreadHelper.WriteLog(implementationLog, sp);
            }
            catch
            {
                // TODO залогировать исключение
            }
            finally
            {
                request = buff.CreateMessage();
            }

            return CorrelationObject;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            if (implementationLog == null || correlationState == null)
                return;

            Correlation CorrelationObject = (Correlation)correlationState;
            MessageBuffer buff = null;

            try
            {

                buff = reply.CreateBufferedCopy(int.MaxValue);

                StringBuilder sb = new StringBuilder();

                using (var sw = new StringWriter(sb))
                using (var xtw = new XmlTextWriter(sw))
                    buff.CreateMessage().WriteMessage(xtw);

                var sp = new SoapPackage(
                        Action: CorrelationObject.Action,
                        Message: sb.ToString(),
                        Direction: DirectionMessage.Receive,
                        To: CorrelationObject.To,
                        From: CorrelationObject.From,
                        MessageID: CorrelationObject.MessageID);

                ExecLogThreadHelper.WriteLog(implementationLog, sp);
            }
            catch
            {
                // TODO залогировать исключение
            }
            finally
            {
                reply = buff.CreateMessage();
            }
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
