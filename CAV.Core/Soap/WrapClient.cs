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
        public T Client { get { return client; } }

        #region Члены IDisposable

        /// <summary>
        /// Dispose()
        /// </summary>
        public void Dispose()
        {
            client.CloseClient();
        }

        #endregion
    }
}
