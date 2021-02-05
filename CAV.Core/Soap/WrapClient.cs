using System;
using System.ServiceModel;

namespace Cav.Soap
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
            this.client = Client;
        }

        private readonly T client;

        /// <summary>
        /// Экземпляр клиента
        /// </summary>
        public T Client => client;

        #region Члены IDisposable

        /// <summary>
        /// Dispose()
        /// </summary>
        public void Dispose()
        {
            if (client == null)
                return;

            if (client.State == CommunicationState.Faulted)
            {
                client.Abort();
            }
            else if (client.State != CommunicationState.Closed)
            {
                client.Close();
            }

            client.Dispose();
        }

        #endregion
    }
}
