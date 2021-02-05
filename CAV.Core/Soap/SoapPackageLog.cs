using System;

namespace Cav.Soap
{
    /// <summary>
    /// Интерфейс для реализации логирования пакетов SOAP. 
    /// Реализовывать: в сервисе - в реализации интерфеса контракта
    /// 
    /// </summary>
    public interface ISoapPackageLog
    {
        /// <summary>
        /// Необходимые действия логирования
        /// </summary>
        void ActionLog(SoapPackage Package);
    }

    /// <summary>
    /// Инкапсуляция параметров пакета
    /// </summary>
    public class SoapPackage
    {
        internal SoapPackage(
            String Action,
            String Message,
            DirectionMessage Direction,
            Uri To,
            String From,
            Guid MessageID)
        {
            this.Action = Action;
            this.Message = Message;
            this.Direction = Direction;
            this.To = To;
            this.From = From;
            this.MessageID = MessageID;
        }

        /// <summary>
        /// Вызываемый метод
        /// </summary>
        public String Action { get; private set; }
        /// <summary>
        /// Тело пакета. 
        /// </summary>
        public String Message { get; private set; }
        /// <summary>
        /// Направление пакета
        /// </summary>
        public DirectionMessage Direction { get; private set; }
        /// <summary>
        /// Целевая конечная точка сообщения
        /// </summary>
        public Uri To { get; private set; }
        /// <summary>
        /// Адрес узла, отправившего сообщение
        /// </summary>
        public String From { get; private set; }
        /// <summary>
        /// Уникальный ИД для связывания двух пакетов запрос-ответ
        /// </summary>
        public Guid MessageID { get; private set; }

        /// <summary>
        /// Сцепление значений From, To и Action
        /// </summary>
        /// <returns></returns>
        public String FromToAction()
        {
            return "From:" + From + " To:" + To + " Action:" + Action;
        }
    }

    /// <summary>
    /// Направление сообщения
    /// </summary>
    public enum DirectionMessage
    {
        /// <summary>
        /// Полученное
        /// </summary>
        Receive,
        /// <summary>
        /// Отправленое
        /// </summary> 
        Send
    }


}
