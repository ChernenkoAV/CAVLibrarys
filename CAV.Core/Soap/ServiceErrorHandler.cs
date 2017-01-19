using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Threading.Tasks;

namespace Cav.Soap
{
    /// <summary>
    /// Подключение обработчика ошибок, произошедших во времы выполнения методов службы (для обработки и логирования)
    /// </summary>
    internal class ServiceErrorHandler : IErrorHandler, IServiceBehavior
    {
        public ServiceErrorHandler(Action<Exception> handler)
        {
            this.handler = handler;
        }

        private Action<Exception> handler = null;

        public bool HandleError(Exception error)
        {
            if (handler == null)
                return false;

            (new Task(ServiceErrorHandler.runhandler, Tuple.Create(handler, error))).Start();

            return false;
        }

        private static void runhandler(object o)
        {
            try
            {
                Tuple<Action<Exception>, Exception> p = (Tuple<Action<Exception>, Exception>)o;
                p.Item1(p.Item2);
            }
            catch { }
        }

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault) { }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) { }

        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters) { }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (ChannelDispatcher chanDisp in serviceHostBase.ChannelDispatchers)
                chanDisp.ErrorHandlers.Add(this);
        }

    }
}
