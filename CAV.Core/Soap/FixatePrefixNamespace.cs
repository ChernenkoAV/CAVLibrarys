using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Xml;

namespace Cav.Soap
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
        public override MessageHeaders Headers
        {
            get { return this.message.Headers; }
        }
        public override MessageProperties Properties
        {
            get { return this.message.Properties; }
        }
        public override MessageVersion Version
        {
            get { return this.message.Version; }
        }

        protected override void OnWriteStartBody(XmlDictionaryWriter writer)
        {
            var soapNS = this.formatter.Namespaces.FirstOrDefault(x => x.Value == SoapHelper.Soap11Namespace);
            if (soapNS.Value.IsNullOrWhiteSpace())
                soapNS = new KeyValuePair<string, string>("s", SoapHelper.Soap11Namespace);

            writer.WriteStartElement(soapNS.Key, "Body", soapNS.Value);

            foreach (var item in this.formatter.Namespaces.Where(x => x.Value != SoapHelper.Soap11Namespace).ToArray())
                writer.WriteAttributeString("xmlns", item.Key, null, item.Value);
        }
        protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
        {
            this.message.WriteBodyContents(writer);
        }
        protected override void OnWriteStartEnvelope(XmlDictionaryWriter writer)
        {
            var soapNS = this.formatter.Namespaces.FirstOrDefault(x => x.Value == SoapHelper.Soap11Namespace);
            if (soapNS.Value.IsNullOrWhiteSpace())
                soapNS = new KeyValuePair<string, string>("s", SoapHelper.Soap11Namespace);

            writer.WriteStartElement(soapNS.Key, "Envelope", soapNS.Value);

            foreach (var item in this.formatter.Namespaces.Where(x => x.Value != SoapHelper.Soap11Namespace).ToArray())
                writer.WriteAttributeString("xmlns", item.Key, null, item.Value);
        }
    }

    internal class FixatePrefixMessageFormatter : IClientMessageFormatter, IDispatchMessageFormatter, IOperationBehavior, IEndpointBehavior
    {
        private readonly IClientMessageFormatter clientFormatter;
        private readonly IDispatchMessageFormatter serverFormatter;
        private readonly Dictionary<String, String> prefNamespace;

        public Dictionary<String, String> Namespaces { get { return prefNamespace; } }

        #region ctor

        public FixatePrefixMessageFormatter(Dictionary<String, String> namespaces)
        {
            this.prefNamespace = namespaces;
        }

        public FixatePrefixMessageFormatter(IClientMessageFormatter formatter, Dictionary<String, String> namespaces)
        {
            this.clientFormatter = formatter;
            this.prefNamespace = namespaces;
        }

        public FixatePrefixMessageFormatter(IDispatchMessageFormatter formatter, Dictionary<String, String> namespaces)
        {
            this.serverFormatter = formatter;
            this.prefNamespace = namespaces;
        }

        #endregion

        #region IClientMessageFormatter

        public Message SerializeRequest(MessageVersion messageVersion, object[] parameters)
        {
            var message = this.clientFormatter.SerializeRequest(messageVersion, parameters);
            return new FixatePrefixMessage(message, this);
        }

        public object DeserializeReply(Message message, object[] parameters)
        {
            return this.clientFormatter.DeserializeReply(message, parameters);
        }

        #endregion

        #region IDispatchMessageFormatter

        public void DeserializeRequest(Message message, object[] parameters)
        {
            this.serverFormatter.DeserializeRequest(message, parameters);
        }

        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            var message = this.serverFormatter.SerializeReply(messageVersion, parameters, result);
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

            dispatchOperation.Formatter = new FixatePrefixMessageFormatter(dispatchOperation.Formatter, this.Namespaces);

        }

        public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
        {
            var serializerBehavior = operationDescription.Behaviors.Find<XmlSerializerOperationBehavior>();

            if (clientOperation.Formatter == null)
                ((IOperationBehavior)serializerBehavior).ApplyClientBehavior(operationDescription, clientOperation);

            clientOperation.Formatter = new FixatePrefixMessageFormatter(clientOperation.Formatter, this.Namespaces);
        }

        public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters) { }

        public void Validate(ServiceEndpoint endpoint) { }

        #endregion 

        #region IEndpointBehavior

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
            foreach (var po in endpoint.Contract.Operations)
            {
#if NET40
                po.Behaviors.Add(this);
#else
                po.OperationBehaviors.Add(this);
#endif
            }
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }
        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime) { }

        #endregion
    }
}
