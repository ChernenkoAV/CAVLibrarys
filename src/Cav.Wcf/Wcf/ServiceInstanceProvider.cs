using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Cav.Wcf
{
    internal class ServiceInstanceProvider : IServiceBehavior
    {
        public ServiceInstanceProvider(IInstanceProvider instanceProvider) => this.instanceProvider = instanceProvider;

        private IInstanceProvider instanceProvider;

        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
        {

        }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (ChannelDispatcher cd in serviceHostBase.ChannelDispatchers)
                foreach (var ed in cd.Endpoints)
                    if (!ed.IsSystemEndpoint)
                        ed.DispatchRuntime.InstanceProvider = instanceProvider;
        }
        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) { }
    }
}
