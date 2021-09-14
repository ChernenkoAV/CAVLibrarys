using System;
using System.ServiceModel;

namespace Cav.Wcf
{
    /// <summary>
    /// Обертка для клиентов на базе ICommunicationObject для корректного закрытия канала.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WrapClient<T> : IDisposable
        where T : class, ICommunicationObject, IDisposable
    {
        /// <summary>
        /// Создание обертки на базе клиента
        /// </summary>
        /// <param name="Client">Экземпляр клиента</param>
        public WrapClient(T Client)
        {
            this.Client = Client;
        }

        /// <summary>
        /// Экземпляр клиента
        /// </summary>
        public T Client { get; }

        #region Члены IDisposable

        /// <summary>
        /// Dispose()
        /// </summary>
        public void Dispose()
        {
            if (Client == null)
                return;

            if (Client.State == CommunicationState.Faulted)
            {
                Client.Abort();
            }
            else if (Client.State != CommunicationState.Closed)
            {
                Client.Close();
            }

            Client.Dispose();
        }

        #endregion
    }
}
