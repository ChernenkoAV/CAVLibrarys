using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;

namespace Cav.DataAcces
{
    /// <summary>
    /// Интерфейс для тестирования слоя доступа к данным
    /// </summary>
    public interface IDataAcces
    {
        /// <summary>
        /// Обработчик исключения пры запуске <see cref="DbCommand"/>. Должен генерировать новое исключение (Для обертки "страшных" сиключений в "нестрашные")
        /// </summary>
        Action<Exception> ExceptionHandlingExecuteCommand { get; set; }

        /// <summary>
        /// Метод, выполняемый перед выполнением <see cref="DbCommand"/>. Возвращаемое значение - объект кореляции вызовов (с <see cref="DataAccesBase.MonitorCommandAfterExecute"/>)
        /// </summary>
        /// <remarks>Метод выполняется обернутым в try cath.</remarks>
        Func<Object> MonitorCommandBeforeExecute { get; set; }
        /// <summary>
        /// Метод, выполняемый после выполнения <see cref="DbCommand"/>.
        /// <see cref="String"/> - текст команды,
        /// <see cref="Object"/> - объект кореляции, возвращяемый из <see cref="DataAccesBase.MonitorCommandBeforeExecute"/> (либо null, если <see cref="DataAccesBase.MonitorCommandBeforeExecute"/> == null),
        /// <see cref="DbParameter"/>[] - копия параметров, с которыми отработала команда <see cref="DbCommand"/>.
        /// </summary>
        /// <remarks>Метод выполняется в отдельном потоке, обернутый в try cath.</remarks>
        Action<String, Object, DbParameter[]> MonitorCommandAfterExecute { get; set; }
    }

    /// <summary>
    /// Интерфейс для тестирования слоя доступа к данным в нормированной выборкой
    /// </summary>
    public interface IDataAcces<Trow, TselectParams> : IDataAcces
        where Trow : class, new()
        where TselectParams : IAdapterParametrs
    {
        /// <summary>
        /// Получение данных из БД с записью в класс Trow, либо в его наследники
        /// </summary>
        /// <typeparam name="THeritorType">Указание типа для оторажения данных. Должен быть Trow или его наследником </typeparam>
        /// <param name="selectParams">Выражение на основе типа парамеров адаптера на выборку. Если null, то всем параметров присваивается DbNull</param>
        /// <returns>Коллекция объектов типа THeritorType</returns>
        IEnumerable<THeritorType> Get<THeritorType>(
                Expression<Action<TselectParams>> selectParams = null)
                where THeritorType : Trow, new();

        /// <summary>
        /// Получение данных из БД с записью в класс Trow
        /// </summary>
        /// <param name="selectParams">Выражение на основе типа парамеров адаптера на выборку. Если null, то всем параметров присваивается DbNull</param>
        /// <returns>Коллекция объектов типа Trow</returns>
        IEnumerable<Trow> Get(Expression<Action<TselectParams>> selectParams = null);
    }

    /// <summary>
    /// Интерфейс для тестирования слоя доступа к данным в нормированной выборкой
    /// </summary>
    /// <typeparam name="Trow">Класс, на который производится отражение данных из БД</typeparam>
    /// <typeparam name="TselectParams">Клас, типизирующий параметры адаптера на выборку</typeparam>
    /// <typeparam name="TupdateParams">Клас, типизирующий параметры адаптера на изменение</typeparam>
    /// <typeparam name="TdeleteParams">Клас, типизирующий параметры адаптера на удаление</typeparam>
    public interface IDataAcces<Trow, TselectParams, TupdateParams, TdeleteParams> : IDataAcces<Trow, TselectParams>
        where Trow : class, new()
        where TselectParams : IAdapterParametrs
        where TupdateParams : IAdapterParametrs
        where TdeleteParams : IAdapterParametrs
    {
        /// <summary>
        /// Добавить объект в БД
        /// </summary>
        /// <param name="newObj">Экземпляр объекта, который необходимо добавит в БД</param>
        void Add(Trow newObj);

        /// <summary>
        /// Удаление по предикату 
        /// </summary>
        /// <param name="deleteParams"></param>
        void Delete(Expression<Action<TdeleteParams>> deleteParams);

        /// <summary>
        /// Обновление данных
        /// </summary>
        /// <param name="updateParams"></param>
        void Update(Expression<Action<TupdateParams>> updateParams);
    }
}
