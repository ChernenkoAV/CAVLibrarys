using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Xml;

namespace Cav.Wcf
{
    internal class LogMessageCaller : IDispatchMessageInspector, IClientMessageInspector, IEndpointBehavior
    {
        internal LogMessageCaller(Action<MessageLogData> logger)
        {
            this.logger = logger;
        }

        readonly Action<MessageLogData> logger = null;

        #region IEndpointBehavior

        void IEndpointBehavior.ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(this);
        }

        void IEndpointBehavior.AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {

        }

        void IEndpointBehavior.ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(this);
        }

        void IEndpointBehavior.Validate(ServiceEndpoint endpoint) { }

        #endregion

        #region IDispatchMessageInspector

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            var buff = request.CreateBufferedCopy(int.MaxValue);
            request = buff.CreateMessage();
            var msg = buff.CreateMessage();
            buff.Close();

            var to = msg.Headers.To;

            string msgBody = null;

            if (!msg.IsEmpty)
            {
                WebBodyFormatMessageProperty wbfmp = null;

                if (msg.Properties.ContainsKey(WebBodyFormatMessageProperty.Name))
                    wbfmp = (WebBodyFormatMessageProperty)msg.Properties[WebBodyFormatMessageProperty.Name];

                WebContentFormat format = WebContentFormat.Xml;

                if (wbfmp != null)
                    format = wbfmp.Format;

                switch (format)
                {
                    case WebContentFormat.Default:
                        msgBody = msg.ToString();
                        break;
                    case WebContentFormat.Xml:
                        var sb = new StringBuilder();
                        using (var sw = new StringWriter(sb))
                        using (var xtw = new XmlTextWriter(sw))
                            msg.WriteMessage(xtw);

                        msgBody = sb.ToString();
                        break;
                    case WebContentFormat.Json:
                        using (MemoryStream ms = new MemoryStream())
                        using (var writer = JsonReaderWriterFactory.CreateJsonWriter(ms))
                        {
                            msg.WriteMessage(writer);
                            writer.Flush();
                            msgBody = Encoding.UTF8.GetString(ms.ToArray());
                        }
                        break;
                    case WebContentFormat.Raw:
                        msgBody = Encoding.UTF8.GetString(msg.GetBody<byte[]>());
                        break;
                    default:
                        break;
                }
            }

            msg.Close();

            var curOpContext = OperationContext.Current;

            var serviceName = curOpContext.Host.Description.Name;
            var method = ((HttpRequestMessageProperty)curOpContext.IncomingMessageProperties[HttpRequestMessageProperty.Name])?.Method;

            string action = curOpContext.IncomingMessageHeaders.Action;

            string operationName = null;
            if (curOpContext.IncomingMessageProperties.Keys.Contains("HttpOperationName"))
                operationName = ((String)curOpContext.IncomingMessageProperties["HttpOperationName"]);
            else
            {
                var operations = curOpContext.EndpointDispatcher.DispatchRuntime.Operations;
                operationName = operations.FirstOrDefault(x => x.Action == action)?.Name;
            }

            var remp = (RemoteEndpointMessageProperty)OperationContext.Current.IncomingMessageProperties[RemoteEndpointMessageProperty.Name];
            var from = $"{remp.Address}:{remp.Port}";

            var mID = Guid.NewGuid();

            var actionFull = $"[{method}] ({serviceName}/{operationName})";

            if (!action.IsNullOrWhiteSpace())
                actionFull += $" \"{action}\"";

            var sp = new MessageLogData()
            {
                Action = actionFull,
                Message = msgBody,
                MessageID = mID,
                To = to,
                From = from,
                Direction = Direction.Incoming
            };

            ExecLogThreadHelper.WriteLog(logger, sp);

            return mID;
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            var buff = reply.CreateBufferedCopy(int.MaxValue);
            reply = buff.CreateMessage();
            var msg = buff.CreateMessage();
            buff.Close();

            string msgBody = null;

            if (!msg.IsEmpty)
            {
                WebBodyFormatMessageProperty wbfmp = null;

                if (msg.Properties.ContainsKey(WebBodyFormatMessageProperty.Name))
                    wbfmp = (WebBodyFormatMessageProperty)msg.Properties[WebBodyFormatMessageProperty.Name];

                WebContentFormat format = WebContentFormat.Xml;

                if (wbfmp != null)
                    format = wbfmp.Format;

                switch (format)
                {
                    case WebContentFormat.Default:
                        msgBody = msg.ToString();
                        break;
                    case WebContentFormat.Xml:
                        var sb = new StringBuilder();
                        using (var sw = new StringWriter(sb))
                        using (var xtw = new XmlTextWriter(sw))
                            msg.WriteMessage(xtw);

                        msgBody = sb.ToString();
                        break;
                    case WebContentFormat.Json:
                        using (MemoryStream ms = new MemoryStream())
                        using (var writer = JsonReaderWriterFactory.CreateJsonWriter(ms))
                        {
                            msg.WriteMessage(writer);
                            writer.Flush();
                            msgBody = Encoding.UTF8.GetString(ms.ToArray());
                        }
                        break;
                    case WebContentFormat.Raw:
                        msgBody = Encoding.UTF8.GetString(msg.GetBody<byte[]>());
                        break;
                    default:
                        break;
                }
            }

            msg.Close();

            if (!msgBody.IsNullOrWhiteSpace())
                msgBody = msgBody.Replace(@"\u000d\u000a", Environment.NewLine);

            var mID = (Guid)correlationState;

            var sp = new MessageLogData()
            {
                Message = msgBody,
                MessageID = mID,
                Direction = Direction.Outgoing
            };

            ExecLogThreadHelper.WriteLog(logger, sp);
        }

        #endregion

        #region IClientMessageInspector

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            var buff = request.CreateBufferedCopy(int.MaxValue);
            request = buff.CreateMessage();
            var msg = buff.CreateMessage();
            buff.Close();

            string msgBody = null;

            if (!msg.IsEmpty)
            {
                WebBodyFormatMessageProperty wbfmp = null;

                if (msg.Properties.ContainsKey(WebBodyFormatMessageProperty.Name))
                    wbfmp = (WebBodyFormatMessageProperty)msg.Properties[WebBodyFormatMessageProperty.Name];

                WebContentFormat format = WebContentFormat.Xml;

                if (wbfmp != null)
                    format = wbfmp.Format;

                switch (format)
                {
                    case WebContentFormat.Default:
                        msgBody = msg.ToString();
                        break;
                    case WebContentFormat.Xml:
                        var sb = new StringBuilder();
                        using (var sw = new StringWriter(sb))
                        using (var xtw = new XmlTextWriter(sw))
                            msg.WriteMessage(xtw);

                        msgBody = sb.ToString();
                        break;
                    case WebContentFormat.Json:
                        using (MemoryStream ms = new MemoryStream())
                        using (var writer = JsonReaderWriterFactory.CreateJsonWriter(ms))
                        {
                            msg.WriteMessage(writer);
                            writer.Flush();
                            msgBody = Encoding.UTF8.GetString(ms.ToArray());
                        }
                        break;
                    case WebContentFormat.Raw:
                        msgBody = Encoding.UTF8.GetString(msg.GetBody<byte[]>());
                        break;
                    default:
                        break;
                }
            }

            msg.Close();

            var mID = Guid.NewGuid();

            var ch = channel;
            var ff = ch.RemoteAddress;


            /*
            var serviceName = curOpContext.Host.Description.Name;
            var method = ((HttpRequestMessageProperty)curOpContext.IncomingMessageProperties[HttpRequestMessageProperty.Name])?.Method;

            string action = curOpContext.IncomingMessageHeaders.Action;

            string operationName = null;
            if (curOpContext.IncomingMessageProperties.Keys.Contains("HttpOperationName"))
                operationName = ((String)curOpContext.IncomingMessageProperties["HttpOperationName"]);
            else
            {
                var operations = curOpContext.EndpointDispatcher.DispatchRuntime.Operations;
                operationName = operations.FirstOrDefault(x => x.Action == action)?.Name;
            }

            var remp = (RemoteEndpointMessageProperty)OperationContext.Current.IncomingMessageProperties[RemoteEndpointMessageProperty.Name];
            var from = $"{remp.Address}:{remp.Port}";

            var actionFull = $"[{method}] ({serviceName}/{operationName})";

            if (!action.IsNullOrWhiteSpace())
                actionFull += $" \"{action}\"";

            var sp = new MessageLogData()
            {
                Action = actionFull,
                Message = msgBody,
                MessageID = mID,
                To = to,
                From = from,
                Direction = Direction.Incoming
            };

            ExecLogThreadHelper.WriteLog(logger, sp);
            
            */

            return mID;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            var buff = reply.CreateBufferedCopy(int.MaxValue);
            reply = buff.CreateMessage();
            var msg = buff.CreateMessage();

            Guid mID = (Guid)correlationState;

            string msgBody = null;

            if (!msg.IsEmpty)
            {
                WebBodyFormatMessageProperty wbfmp = null;

                if (msg.Properties.ContainsKey(WebBodyFormatMessageProperty.Name))
                    wbfmp = (WebBodyFormatMessageProperty)msg.Properties[WebBodyFormatMessageProperty.Name];

                WebContentFormat format = WebContentFormat.Xml;

                if (wbfmp != null)
                    format = wbfmp.Format;

                switch (format)
                {
                    case WebContentFormat.Default:
                        msgBody = msg.ToString();
                        break;
                    case WebContentFormat.Xml:
                        var sb = new StringBuilder();
                        using (var sw = new StringWriter(sb))
                        using (var xtw = new XmlTextWriter(sw))
                            msg.WriteMessage(xtw);

                        msgBody = sb.ToString();
                        break;
                    case WebContentFormat.Json:
                        using (MemoryStream ms = new MemoryStream())
                        using (var writer = JsonReaderWriterFactory.CreateJsonWriter(ms))
                        {
                            msg.WriteMessage(writer);
                            writer.Flush();
                            msgBody = Encoding.UTF8.GetString(ms.ToArray());
                        }
                        break;
                    case WebContentFormat.Raw:
                        msgBody = Encoding.UTF8.GetString(msg.GetBody<byte[]>());
                        break;
                    default:
                        break;
                }
            }

            msg.Close();

            var sp = new MessageLogData()
            {
                Message = msgBody,
                MessageID = mID,
                Direction = Direction.Incoming
            };

            ExecLogThreadHelper.WriteLog(logger, sp);
        }

        #endregion
    }
}
