using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

#pragma warning disable CS8604 // Возможно, аргумент-ссылка, допускающий значение NULL.
#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
#pragma warning disable CA2000 // Ликвидировать объекты перед потерей области

namespace Cav.DataAcces;

/// <summary>
/// Базовый клас для адаптера на получение данных
/// </summary>
/// <typeparam name="TRow">Класс, на который производится отражение данных из БД</typeparam>
/// <typeparam name="TSelectParams">Клас, типизирующий параметры адаптера на выборку</typeparam>
public class DataAccesBase<TRow, TSelectParams> : DataAccesBase, IDataAcces<TRow, TSelectParams>
    where TRow : class, new()
    where TSelectParams : IAdapterParametrs
{
    /// <summary>
    /// Получение данных из БД с записью в класс Trow
    /// </summary>
    /// <param name="selectParams">Выражение на основе типа парамеров адаптера на выборку. Если null, то всем параметров присваивается DbNull</param>
    /// <returns>Коллекция объектов типа Trow</returns>
    public IEnumerable<TRow> Get(Expression<Action<TSelectParams>>? selectParams = null) => Get<TRow>(selectParams);

    /// <summary>
    /// Получение данных из БД с записью в класс Trow, либо в его наследники
    /// </summary>
    /// <typeparam name="THeritorType">Указание типа для оторажения данных. Должен быть Trow или его наследником </typeparam>
    /// <param name="selectParams">Выражение на основе типа парамеров адаптера на выборку. Если null, то всем параметрам присваивается DbNull</param>
    /// <returns>Коллекция объектов типа THeritorType</returns>
    public IEnumerable<THeritorType> Get<THeritorType>(
            Expression<Action<TSelectParams>>? selectParams = null)
            where THeritorType : TRow, new()
    {
        Configured();
        var res = new List<THeritorType>();

        var execCom = addParamToCommand(CommandActionType.Select, selectParams);
        using (var table = FillTable(execCom))
            foreach (var dbRow in table.Rows.Cast<DataRow>())
            {
                var row = (THeritorType)Activator.CreateInstance(typeof(THeritorType))!;

                foreach (var ff in selectPropFieldMap)
                    ff.Value(row, dbRow);

                res.Add(row);
            }

        return res.ToArray();
    }

    /// <summary>
    /// Получение данных из БД с записью в класс Trow
    /// </summary>
    /// <param name="selectParams">Выражение на основе типа парамеров адаптера на выборку. Если null, то всем параметров присваивается DbNull</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Коллекция объектов типа Trow</returns>
    public async Task<IEnumerable<TRow>> GetAsync(
        Expression<Action<TSelectParams>>? selectParams = null,
        CancellationToken cancellationToken = default) =>
            await GetAsync<TRow>(selectParams, cancellationToken);

    /// <summary>
    /// Получение данных из БД с записью в класс Trow, либо в его наследники
    /// </summary>
    /// <typeparam name="THeritorType">Указание типа для оторажения данных. Должен быть Trow или его наследником </typeparam>
    /// <param name="selectParams">Выражение на основе типа парамеров адаптера на выборку. Если null, то всем параметрам присваивается DbNull</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Коллекция объектов типа THeritorType</returns>
    public async Task<IEnumerable<THeritorType>> GetAsync<THeritorType>(
        Expression<Action<TSelectParams>>? selectParams = null,
        CancellationToken cancellationToken = default)
            where THeritorType : TRow, new()
    {
        Configured();
        var res = new List<THeritorType>();

        var execCom = addParamToCommand(CommandActionType.Select, selectParams);
        using (var table = await FillTableAsync(execCom, cancellationToken))
            foreach (var dbRow in table.Rows.Cast<DataRow>())
            {
                var row = (THeritorType)Activator.CreateInstance(typeof(THeritorType))!;

                foreach (var ff in selectPropFieldMap)
                    ff.Value(row, dbRow);

                res.Add(row);
            }

        return res.ToArray();
    }

    /// <summary>
    /// Выполняет запрос и возвращает первый столбец первой строки результирующего набора, возвращаемого запросом. Все другие столбцы и строки игнорируются.
    /// По сути вызов <see cref="DbCommand.ExecuteScalar"/>
    /// </summary>
    /// <param name="selectParams">Выражение на основе типа парамеров адаптера на выборку. Если null, то всем параметрам присваивается DbNull</param>
    /// <returns>Первый столбец первой строки в результирующем наборе</returns>
    public object? GetScalar(
        Expression<Action<TSelectParams>>? selectParams = null)
    {
        Configured();

        var execCom = addParamToCommand(CommandActionType.Select, selectParams);
        return ExecuteScalar(execCom);
    }

    /// <summary>
    /// Выполняет запрос и возвращает первый столбец первой строки результирующего набора, возвращаемого запросом. Все другие столбцы и строки игнорируются.
    /// По сути вызов <see cref="DbCommand.ExecuteScalar"/>
    /// </summary>
    /// <param name="selectParams">Выражение на основе типа парамеров адаптера на выборку. Если null, то всем параметрам присваивается DbNull</param>
    /// <returns>Первый столбец первой строки в результирующем наборе</returns>
    public async Task<object?> GetScalarAsync(
        Expression<Action<TSelectParams>>? selectParams = null,
        CancellationToken cancellationToken = default)
    {
        Configured();

        var execCom = addParamToCommand(CommandActionType.Select, selectParams);
        return await ExecuteScalarAsync(execCom, cancellationToken);
    }

    internal DbCommand addParamToCommand(CommandActionType actionType, Expression? paramsExpr, TRow? obj = null)
    {
        if (!comands.TryGetValue(actionType, out var config))
            throw new NotImplementedException($"Команда для {actionType} не настроена");

        var command = createCommand(config);

        try
        {
            var key = actionType.ToString();

            var paramValues = parceParams(paramsExpr, obj);

            foreach (var item in commandParams.Where(x => x.Key.StartsWith(key)))
            {
                var prmCmd = createParametr(command, item.Value);
                var paramValKey = item.Key.Replace(key + " ", string.Empty);

                if (paramValues.TryGetValue(paramValKey, out var val))
                {
                    paramValues.Remove(paramValKey);

                    if (item.Value.ConvetProperty != null)
                        val = item.Value.ConvetProperty(val);
                }

                if (val != null)
                {
                    var valType = val.GetType();
                    valType = Nullable.GetUnderlyingType(valType) ?? valType;
                    if (valType == typeof(DateTime))
                        val = new DateTimeOffset((DateTime)val).LocalDateTime;

                    prmCmd.Value = val;
                }
            }

            if (paramValues.Count > 0)
            {
                var lost = paramValues.First();
                throw new ArgumentException(string.Format("Для свойства '{0}' не настроено сопоставление в операции {1}", lost.Key, key));
            }

            return command;
        }
        catch
        {
            command?.Dispose();
            throw;
        }
    }

    private Dictionary<string, object> parceParams(Expression? paramsExpr, TRow? obj)
    {
        var res = new Dictionary<string, object>();
        if (paramsExpr == null)
            return res;

        parceCall(res, (paramsExpr as LambdaExpression).Body as MethodCallExpression, obj);

        return res;
    }

    private void parceCall(Dictionary<string, object> d, MethodCallExpression mc, TRow? obj)
    {
        if (mc.Arguments[0] is MethodCallExpression nextExp)
            parceCall(d, nextExp, obj);

        var paramExp = ((mc.Arguments[1] as UnaryExpression).Operand as LambdaExpression).Body;

        var valueExp = mc.Arguments[2];

        if (paramExp.NodeType == ExpressionType.Convert)
            paramExp = (paramExp as UnaryExpression).Operand;
        var name = (paramExp as MemberExpression).Member.Name;

        if (d.ContainsKey(name))
            throw new ArgumentException($"Свойство {name} описано более одного раза");

        var val = obj == null
            ? Expression.Lambda<Func<object>>(valueExp).Compile()()
            : ((valueExp as UnaryExpression).Operand as LambdaExpression).Compile().DynamicInvoke(obj);

        d.Add(name, val!);
    }

    private bool flagConfigured;
    private object lockObj = new();
    internal void Configured()
    {
        lock (lockObj)
        {
            if (flagConfigured)
                return;
            ConfigAcces();
            flagConfigured = true;
        }
    }

    /// <summary>
    /// В производном классе - конфигурция адаптера. 
    /// </summary>
    protected virtual void ConfigAcces()
    {

    }

    /// <summary>
    /// Сопоставление свойства класса отражения с полем результирующего набора данных
    /// </summary>
    /// <param name="property">Свойство класса</param>
    /// <param name="fieldName">Имя поля в результурующем наборе</param>
    /// <param name="convertProperty">Дополнительная функция преобразования</param>
    protected void MapSelectField<T>(
        Expression<Func<TRow, T>> property,
        string fieldName,
        Expression<Func<object, T?>>? convertProperty = null) =>
        MapSelectFieldInDictionary(selectPropFieldMap, property, fieldName, convertProperty);

    internal void MapSelectFieldInDictionary<T>(
        ConcurrentDictionary<string, Action<TRow, DataRow>> selConvertHand,
        Expression<Func<TRow, T>> property,
        string fieldName,
        Expression<Func<object, T?>>? convertProperty = null)
    {
        if (fieldName.IsNullOrWhiteSpace())
            throw new InvalidOperationException("paramName. Имя параметра не может быть пустым, состоять из пробелов или быть null");

        if (property == null)
            throw new ArgumentException("выражение для свойства не может быть null", nameof(property));

        var propBody = property.Body;
        if (propBody.NodeType != ExpressionType.MemberAccess)
            throw new ArgumentException("В выражении property возможен только доступ к свойству или полю класса");

        var paramName = (propBody as MemberExpression).Member.Name;

        if (selConvertHand.ContainsKey(paramName))
            throw new ArgumentException($"Для свойства {paramName} уже определна связка");

        var typeT = typeof(T);
        var nullableType = Nullable.GetUnderlyingType(typeT);
        if (nullableType != null)
            typeT = nullableType;

        if (!HeplerDataAcces.IsCanMappedDbType(typeT))
        {
            if (convertProperty == null)
                throw new ArgumentException($"Для типа {typeT.FullName} свойства {paramName}, связываемого с полем {fieldName}, должен быть указан конвертор");
        }

        var p_rowObg = property.Parameters.First();
        var p_dbRow = Expression.Parameter(typeof(DataRow));

        Delegate? expConv = null;

        if (convertProperty != null)
            expConv = convertProperty.Compile();

        Expression fromfield =
            Expression.Call(
                typeof(HeplerDataAcces).GetMethod(nameof(HeplerDataAcces.FromField), BindingFlags.Static | BindingFlags.NonPublic),
                Expression.Constant(propBody.Type, typeof(Type)),
                p_dbRow,
                Expression.Constant(fieldName, typeof(string)),
                Expression.Constant(expConv, typeof(Delegate)));

        Expression assToProp = Expression.Assign(propBody, Expression.Convert(fromfield, propBody.Type));

        var readfomfield = Expression.Lambda<Action<TRow, DataRow>>(assToProp, p_rowObg, p_dbRow);

        selConvertHand.TryAdd(paramName, readfomfield.Compile());
    }

    /// <summary>
    /// Сопоставление свойств класса параметров адаптера с параметрами скрипта выборки
    /// </summary>
    /// <typeparam name="T">Тип свойства</typeparam> 
    /// <param name="property">Свойство</param>
    /// <param name="paramName">Имя параметра</param>
    /// <param name="typeParam">Тип параметра в БД</param>
    /// <param name="addedConvertFunct">Конвертер для пролучения значения для запроса</param>
    protected void MapSelectParam<T>(
        Expression<Func<TSelectParams, T>> property,
        string paramName,
        DbType? typeParam = null,
        Expression<Func<T, object?>>? addedConvertFunct = null) =>
        MapParam<T>(CommandActionType.Select, property, paramName, typeParam, addedConvertFunct);

    internal void MapParam<T>(CommandActionType actionType, Expression property, string paramName, DbType? typeParam = null, Expression? convertProperty = null)
    {
        var paramType = typeParam;

        if (paramName.IsNullOrWhiteSpace())
            throw new ArgumentException("имя параметра не может быть пустым, состоять из пробелов или быть null", nameof(paramName));

        if (property == null)
            throw new ArgumentException("выражение для свойства не может быть null", nameof(property));

        var typeT = typeof(T);

        if (!HeplerDataAcces.IsCanMappedDbType(typeT) && convertProperty == null)
            throw new ArgumentException($"Для типа {typeT.FullName} должен быть указан конвертор");

        if (convertProperty != null)
        {
            var body = (convertProperty as LambdaExpression).Body;
            if (body.NodeType == ExpressionType.Convert)
                body = (body as UnaryExpression).Operand;

            if (!paramType.HasValue)
                paramType = HeplerDataAcces.TypeMapDbType(body.Type);
        }

        var proprow = (property as LambdaExpression).Body;
        if (proprow.NodeType != ExpressionType.MemberAccess)
            throw new ArgumentException("В выражении property возможен только доступ к свойству или полю класса");

        var propName = (proprow as MemberExpression).Member.Name;
        var key = actionType.ToString() + " " + propName;
        if (commandParams.ContainsKey(key))
            throw new ArgumentException($"Для свойства {propName} уже указано сопоставление");

        var param = new DbParamSetting
        {
            ParamName = paramName
        };
        var typeVal = Nullable.GetUnderlyingType(proprow.Type) ?? proprow.Type;

        if (!paramType.HasValue)
            paramType = HeplerDataAcces.TypeMapDbType(typeVal);

        if (typeVal.IsEnum)
            param.ConvetProperty = x => x != null ? Convert.ChangeType(x, typeVal.GetEnumUnderlyingType()) : null;

        param.ParamType = paramType.Value;

        if (convertProperty != null)
        {
            var convFunct = (convertProperty as Expression<Func<T, object>>).Compile();
            param.ConvetProperty = x => convFunct((T?)x);
        }

        commandParams.TryAdd(key, param);
    }

    /// <summary>
    /// Конфигурация адаптера команды
    /// </summary>
    /// <param name="config">Объект с настройками адаптера</param>
    protected void ConfigCommand(AdapterConfig config)
    {
        if (config is null)
            throw new ArgumentNullException(nameof(config));

        if (comands.ContainsKey(config.ActionType))
            throw new ArgumentException($"в адаптере уже определена команда с типом {config.ActionType}");
        if (config.TextCommand.IsNullOrWhiteSpace())
            throw new ArgumentException("текст команды не может быть пустым");

        comands.TryAdd(config.ActionType, config);
    }

    private DbCommand createCommand(AdapterConfig config)
    {
        var cmmnd = DbContext.CreateCommand(ConnectionName);
#pragma warning disable CA2100 // Проверка запросов SQL на уязвимости безопасности
        cmmnd.CommandText = config.TextCommand;
#pragma warning restore CA2100 // Проверка запросов SQL на уязвимости безопасности
        cmmnd.CommandTimeout = config.TimeoutCommand;

        switch (config.TypeCommand)
        {
            case DataAccesCommandType.Text:
                cmmnd.CommandType = CommandType.Text;
                break;
            case DataAccesCommandType.StoredProcedure:
                cmmnd.CommandType = CommandType.StoredProcedure;
                break;
        }

        return cmmnd;
    }

    private DbParameter createParametr(DbCommand cmd, DbParamSetting paramSetting)
    {
        var res = cmd.CreateParameter();
        res.ParameterName = paramSetting.ParamName;
        res.DbType = paramSetting.ParamType;
        res.Value = DBNull.Value;
        cmd.Parameters.Add(res);
        return res;
    }

    private ConcurrentDictionary<string, Action<TRow, DataRow>> selectPropFieldMap = new();
    private ConcurrentDictionary<string, DbParamSetting> commandParams = new();
    private ConcurrentDictionary<CommandActionType, AdapterConfig> comands = new();
}

/// <summary>
/// Класс инкапсуляции настроек адаптера
/// </summary>
public sealed class AdapterConfig
{
    /// <summary>
    /// Текст команды
    /// </summary>
    public string TextCommand { get; set; } = null!;
    /// <summary>
    /// Таймаут команды. По умолчанию - 15
    /// </summary>
    public int TimeoutCommand { get; set; } = 15;
    /// <summary>
    /// Тип команды
    /// </summary>
    public DataAccesCommandType TypeCommand { get; set; }
    /// <summary>
    /// Тип действия команды
    /// </summary>
    public CommandActionType ActionType { get; set; }
}
/// <summary>
/// Тип команды в адаптере
/// </summary>
public enum DataAccesCommandType
{
    /// <summary>
    /// Текстовая строка
    /// </summary>
    Text,
    /// <summary>
    /// Хранимая процедура
    /// </summary>
    StoredProcedure
}

/// <summary>
/// Тип действия адаптера
/// </summary>
public enum CommandActionType
{
    /// <summary>
    /// Выборка
    /// </summary>
    Select,
    /// <summary>
    /// Вставка
    /// </summary>
    Insert,
    /// <summary>
    /// Обновление
    /// </summary>
    Update,
    /// <summary>
    /// Удаление
    /// </summary>
    Delete
}

internal struct DbParamSetting
{
    public string ParamName { get; set; }
    public DbType ParamType { get; set; }
    public Func<object?, object?>? ConvetProperty { get; set; }
}

/// <summary>
/// Интерфейс для параметров адаптеров
/// </summary>
#pragma warning disable CA1040 // Не используйте пустые интерфейсы
public interface IAdapterParametrs { }
#pragma warning restore CA1040 // Не используйте пустые интерфейсы

