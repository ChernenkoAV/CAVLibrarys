using System;
using System.ServiceModel;

namespace Cav.Wcf
{
    /// <summary>
    /// Обертка для клиентов на базе ICommunicationObject для корректного закрытия канала.
    /// </summary>
    /// <typeparam name="T"></typeparam>
#pragma warning disable CA1063 // Правильно реализуйте IDisposable
    public class WrapClient<T> : IDisposable
#pragma warning restore CA1063 // Правильно реализуйте IDisposable
        where T : class, ICommunicationObject, IDisposable
    {
        /// <summary>
        /// Создание обертки на базе клиента
        /// </summary>
        /// <param name="client">Экземпляр клиента</param>
        public WrapClient(T client) => Client = client;

        /// <summary>
        /// Экземпляр клиента
        /// </summary>
        public T Client { get; }

        #region Члены IDisposable

        /// <summary>
        /// Dispose()
        /// </summary>
#pragma warning disable CA1063 // Правильно реализуйте IDisposable
#pragma warning disable CA1816 // Методы Dispose должны вызывать SuppressFinalize
        public void Dispose()
#pragma warning restore CA1816 // Методы Dispose должны вызывать SuppressFinalize
#pragma warning restore CA1063 // Правильно реализуйте IDisposable
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
