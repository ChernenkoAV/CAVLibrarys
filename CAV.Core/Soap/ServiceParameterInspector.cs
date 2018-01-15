﻿using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Threading.Tasks;

namespace Cav.Soap
{
    internal class SoapParameterInspector : IParameterInspector, IEndpointBehavior
    {
        public SoapParameterInspector(Func<object[], object> beforeCall, Action<string, object> afterCall)
        {
            this.beforeCall = beforeCall;
            this.afterCall = afterCall;
        }

        private Func<object[], object> beforeCall = null;
        private Action<string, object> afterCall = null;

        private static void prafcall(object o)
        {
            try
            {
                Tuple<Action<string, object>, String, object> tl = (Tuple<Action<string, object>, String, object>)o;
                tl.Item1(tl.Item2, tl.Item3);
            }
            catch { }
        }

        public void AfterCall(string operationName, object[] outputs, object returnValue, object correlationState)
        {
            if (afterCall == null)
                return;

            (new Task(prafcall, Tuple.Create(afterCall, operationName, correlationState))).Start();

        }

        public object BeforeCall(string operationName, object[] inputs)
        {
            if (beforeCall == null)
                return null;
            try
            {
                return beforeCall(inputs);
            }
            catch { }

            return null;
        }

        public void Validate(ServiceEndpoint endpoint)
        {

        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {

        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            foreach (var operation in endpointDispatcher.DispatchRuntime.Operations)
            {
                operation.ParameterInspectors.Add(this);
            }
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            foreach (var operation in clientRuntime.ClientOperations)
            {
                operation.ParameterInspectors.Add(this);
            }
        }
    }
}
