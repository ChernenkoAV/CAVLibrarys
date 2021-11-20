using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Xml;

namespace Cav.Wcf
{
    internal class FixatePrefixMessage : Message
    {
        private readonly Message message;
        private readonly FixatePrefixMessageFormatter formatter;
        public FixatePrefixMessage(Message message, FixatePrefixMessageFormatter formatter)
        {
            this.message = message;
            this.formatter = formatter;
        }
        public override MessageHeaders Headers => message.Headers;
        public override MessageProperties Properties => message.Properties;
        public override MessageVersion Version => message.Version;

        protected override void OnWriteStartBody(XmlDictionaryWriter writer)
        {
            var soapNS = formatter.Namespaces.FirstOrDefault(x => x.Value == WcfHelpers.soap11Namespace);
            if (soapNS.Value.IsNullOrWhiteSpace())
                soapNS = new KeyValuePair<string, string>("s", WcfHelpers.soap11Namespace);

            writer.WriteStartElement(soapNS.Key, "Body", soapNS.Value);

            foreach (var item in formatter.Namespaces.Where(x => x.Value != WcfHelpers.soap11Namespace).ToArray())
                writer.WriteAttributeString("xmlns", item.Key, null, item.Value);
        }
        protected override void OnWriteBodyContents(XmlDictionaryWriter writer) => message.WriteBodyContents(writer);
        protected override void OnWriteStartEnvelope(XmlDictionaryWriter writer)
        {
            var soapNS = formatter.Namespaces.FirstOrDefault(x => x.Value == WcfHelpers.soap11Namespace);
            if (soapNS.Value.IsNullOrWhiteSpace())
                soapNS = new KeyValuePair<string, string>("s", WcfHelpers.soap11Namespace);

            writer.WriteStartElement(soapNS.Key, "Envelope", soapNS.Value);

            foreach (var item in formatter.Namespaces.Where(x => x.Value != WcfHelpers.soap11Namespace).ToArray())
                writer.WriteAttributeString("xmlns", item.Key, null, item.Value);
        }
    }

    internal class FixatePrefixMessageFormatter : IClientMessageFormatter, IDispatchMessageFormatter, IOperationBehavior, IEndpointBehavior
    {
        private readonly IClientMessageFormatter clientFormatter;
        private readonly IDispatchMessageFormatter serverFormatter;

        public Dictionary<String, String> Namespaces { get; }

        #region ctor

        public FixatePrefixMessageFormatter(Dictionary<String, String> namespaces) => Namespaces = namespaces;

        public FixatePrefixMessageFormatter(IClientMessageFormatter formatter, Dictionary<String, String> namespaces)
        {
            clientFormatter = formatter;
            Namespaces = namespaces;
        }

        public FixatePrefixMessageFormatter(IDispatchMessageFormatter formatter, Dictionary<String, String> namespaces)
        {
            serverFormatter = formatter;
            Namespaces = namespaces;
        }

        #endregion

        #region IClientMessageFormatter

        public Message SerializeRequest(MessageVersion messageVersion, object[] parameters)
        {
            var message = clientFormatter.SerializeRequest(messageVersion, parameters);
            return new FixatePrefixMessage(message, this);
        }

        public object DeserializeReply(Message message, object[] parameters) => clientFormatter.DeserializeReply(message, parameters);

        #endregion

        #region IDispatchMessageFormatter

        public void DeserializeRequest(Message message, object[] parameters) => serverFormatter.DeserializeRequest(message, parameters);

        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            var message = serverFormatter.SerializeReply(messageVersion, parameters, result);
            return new FixatePrefixMessage(message, this);
        }

        #endregion

        #region IOperationBehavior
        public void Validate(OperationDescription operationDescription) { }

        public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
            var serializerBehavior = operationDescription.Behaviors.Find<XmlSerializerOperationBehavior>();

            if (dispatchOperation.Formatter == null)
                ((IOperationBehavior)serializerBehavior).ApplyDispatchBehavior(operationDescription, dispatchOperation);

            dispatchOperation.Formatter = new FixatePrefixMessageFormatter(dispatchOperation.Formatter, Namespaces);

        }

        public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
        {
            var serializerBehavior = operationDescription.Behaviors.Find<XmlSerializerOperationBehavior>();

            if (clientOperation.Formatter == null)
                ((IOperationBehavior)serializerBehavior).ApplyClientBehavior(operationDescription, clientOperation);

            clientOperation.Formatter = new FixatePrefixMessageFormatter(clientOperation.Formatter, Namespaces);
        }

        public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters) { }

        public void Validate(ServiceEndpoint endpoint) { }

        #endregion 

        #region IEndpointBehavior

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
            foreach (var po in endpoint.Contract.Operations)
            {
                po.OperationBehaviors.Add(this);
            }
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }
        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime) { }

        #endregion
    }
}
