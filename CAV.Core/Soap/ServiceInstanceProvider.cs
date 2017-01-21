﻿using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Cav.Soap
{
    internal class ServiceInstanceProvider : IServiceBehavior
    {
        public ServiceInstanceProvider(IInstanceProvider instanceProvider)
        {
            this.instanceProvider = instanceProvider;
        }

        private IInstanceProvider instanceProvider = null;

        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
        {

        }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (ChannelDispatcher cd in serviceHostBase.ChannelDispatchers)
                foreach (EndpointDispatcher ed in cd.Endpoints)
                    if (!ed.IsSystemEndpoint)
                        ed.DispatchRuntime.InstanceProvider = instanceProvider;
        }
        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) { }
    }
}