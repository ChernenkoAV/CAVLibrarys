using System.Data.Common;
using System.Linq.Expressions;

namespace Cav.DataAcces;

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
    Func<object?> MonitorCommandBeforeExecute { get; set; }
    /// <summary>
    /// Метод, выполняемый после выполнения <see cref="DbCommand"/>.
    /// <see cref="String"/> - текст команды,
    /// <see cref="Object"/> - объект кореляции, возвращяемый из <see cref="DataAccesBase.MonitorCommandBeforeExecute"/> (либо null, если <see cref="DataAccesBase.MonitorCommandBeforeExecute"/> == null),
    /// <see cref="DbParameter"/>[] - копия параметров, с которыми отработала команда <see cref="DbCommand"/>.
    /// </summary>
    /// <remarks>Метод выполняется в отдельном потоке, обернутый в try cath.</remarks>
    Action<string, object, DbParameter[]> MonitorCommandAfterExecute { get; set; }
}

/// <summary>
/// Интерфейс для тестирования слоя доступа к данным c нормированной выборкой
/// </summary>
public interface IDataAcces<TRow, TSelectParams> : IDataAcces
    where TRow : class, new()
    where TSelectParams : IAdapterParametrs
{
    /// <summary>
    /// Получение данных из БД с записью в класс Trow, либо в его наследники
    /// </summary>
    /// <typeparam name="THeritorType">Указание типа для оторажения данных. Должен быть Trow или его наследником </typeparam>
    /// <param name="selectParams">Выражение на основе типа парамеров адаптера на выборку. Если null, то всем параметров присваивается DbNull</param>
    /// <returns>Коллекция объектов типа THeritorType</returns>
    IEnumerable<THeritorType> Get<THeritorType>(
            Expression<Action<TSelectParams>>? selectParams = null)
            where THeritorType : TRow, new();

    /// <summary>
    /// Получение данных из БД с записью в класс Trow
    /// </summary>
    /// <param name="selectParams">Выражение на основе типа парамеров адаптера на выборку. Если null, то всем параметров присваивается DbNull</param>
    /// <returns>Коллекция объектов типа Trow</returns>
    IEnumerable<TRow> Get(Expression<Action<TSelectParams>>? selectParams = null);
}

/// <summary>
/// Интерфейс для тестирования слоя доступа к данным в нормированной выборкой
/// </summary>
/// <typeparam name="TRow">Класс, на который производится отражение данных из БД</typeparam>
/// <typeparam name="TSelectParams">Клас, типизирующий параметры адаптера на выборку</typeparam>
/// <typeparam name="TUpdateParams">Клас, типизирующий параметры адаптера на изменение</typeparam>
/// <typeparam name="TDeleteParams">Клас, типизирующий параметры адаптера на удаление</typeparam>
public interface IDataAcces<TRow, TSelectParams, TUpdateParams, TDeleteParams> : IDataAcces<TRow, TSelectParams>
    where TRow : class, new()
    where TSelectParams : IAdapterParametrs
    where TUpdateParams : IAdapterParametrs
    where TDeleteParams : IAdapterParametrs
{
    /// <summary>
    /// Добавить объект в БД
    /// </summary>
    /// <param name="newObj">Экземпляр объекта, который необходимо добавит в БД</param>
    void Add(TRow newObj);

    /// <summary>
    /// Удаление по предикату 
    /// </summary>
    /// <param name="deleteParams"></param>
    void Delete(Expression<Action<TDeleteParams>> deleteParams);

    /// <summary>
    /// Обновление данных
    /// </summary>
    /// <param name="updateParams"></param>
    void Update(Expression<Action<TUpdateParams>> updateParams);
}
