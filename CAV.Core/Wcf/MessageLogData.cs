using System;

namespace Cav.Wcf
{
    /// <summary>
    /// Направление
    /// </summary>
    public enum Direction
    {
        /// <summary>
        /// Входящий
        /// </summary>
        Incoming,
        /// <summary>
        /// Исходящий
        /// </summary> 
        Outgoing
    }

    /// <summary>
    /// Инкапсуляция данных о сообщениях для лога
    /// </summary>
    public sealed class MessageLogData
    {
        /// <summary>
        /// Имя операции
        /// </summary>
        public string OperationName { get; set; }
        /// <summary>
        /// Имя сервиса (Имя контракта или имя имплементирующего класса).
        /// </summary>
        public String ServiceName { get; set; }
        /// <summary>
        /// Http метод
        /// </summary>
        public string Method { get; set; }
        /// <summary>
        /// Вызываемый метод контракта
        /// </summary>
        public String Action { get; set; }
        /// <summary>
        /// Тело
        /// </summary>
        public String Message { get; set; }
        /// <summary>
        /// Направление
        /// </summary>
        public Direction Direction { get; set; }
        /// <summary>
        /// Целевая конечная точка сообщения
        /// </summary>
        public Uri To { get; set; }
        /// <summary>
        /// Адрес узла, отправившего сообщение
        /// </summary>
        public String From { get; set; }
        /// <summary>
        /// Уникальный ИД для связывания двух сообщений запрос-ответ
        /// </summary>
        public Guid MessageID { get; set; }
    }
}
