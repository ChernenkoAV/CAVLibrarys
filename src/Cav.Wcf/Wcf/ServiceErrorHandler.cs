using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Cav.Wcf
{
    /// <summary>
    /// Подключение обработчика ошибок, произошедших во времы выполнения методов службы (для обработки и логирования)
    /// </summary>
    internal sealed class ServiceErrorHandler : IErrorHandler, IServiceBehavior
    {
        public ServiceErrorHandler(Action<Exception> handler) => this.handler = handler;

        private Action<Exception> handler;

        public bool HandleError(Exception error)
        {
            ExecLogThreadHelper.WriteLog(handler, error);

            return false;
        }

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault) { }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) { }

        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters) { }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (var chanDisp in serviceHostBase.ChannelDispatchers.Cast<ChannelDispatcher>())
                chanDisp.ErrorHandlers.Add(this);
        }
    }
}
