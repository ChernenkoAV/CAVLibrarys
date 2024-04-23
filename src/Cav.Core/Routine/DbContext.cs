using System.Collections.ObjectModel;
using System.Data.Common;

namespace Cav;

/// <summary>
/// Контекст работы с БД
/// </summary>
public static class DbContext
{
    internal const string defaultNameConnection = "DefaultNameConnectionForConnectionCollectionOnDomainContext";

    /// <summary>
    /// Коллекция настроек соединения с БД
    /// </summary>
    private static Dictionary<string, SettingConnection> dcsb = [];
    private struct SettingConnection
    {
        public string ConnectionString { get; set; }
        public Type ConnectionType { get; set; }
        public Type CommandType { get; set; }
    }

    /// <summary>
    /// Инициализация нового соединения с БД указанного типа. Проверка соединения с сервером.
    /// </summary>
    /// <typeparam name="TConnection">Тип - наследник DbConnection</typeparam>
    /// <param name="connectionString">Строка соединения</param>
    /// <param name="connectionName">Имя подключения</param>
    public static void InitConnection<TConnection>(
        string connectionString,
        string? connectionName = null)
        where TConnection : DbConnection =>
        InitConnection(typeof(TConnection), connectionString, connectionName);

    /// <summary>
    /// Инициализация нового соединения с БД указанного типа. Проверка соединения с сервером.
    /// </summary>
    /// <param name="typeConnection">Тип - наследник DbConnection</param>
    /// <param name="connectionString">Строка соединения</param>
    /// <param name="connectionName">Имя подключения</param>
    public static void InitConnection(
        Type typeConnection,
        string connectionString,
        string? connectionName = null)
    {
        if (connectionString.IsNullOrWhiteSpace())
            throw new ArgumentNullException(nameof(connectionString));

        if (connectionName.IsNullOrWhiteSpace())
            connectionName = defaultNameConnection;

        if (typeConnection == null)
            throw new ArgumentNullException(nameof(typeConnection));

        if (!typeConnection.IsSubclassOf(typeof(DbConnection)))
            throw new ArgumentException("typeConnection не является наследником DbConnection");

        using var conn = (DbConnection)Activator.CreateInstance(typeConnection)!;
        conn.ConnectionString = connectionString;
        conn.Open();

        using var cmd = conn.CreateCommand();
        var setCon = new SettingConnection()
        {
            ConnectionString = connectionString,
            ConnectionType = typeConnection,
            CommandType = cmd.GetType()
        };

        dcsb[connectionName!] = setCon;
    }

    /// <summary>
    /// Получение экземпляра открытого соединения с БД
    /// </summary>
    /// <param name="connectionName">Имя соединения в коллекции</param>
    /// <returns></returns>
    public static DbConnection Connection(string? connectionName = null)
    {
        if (connectionName.IsNullOrWhiteSpace())
            connectionName = defaultNameConnection;

        if (!dcsb.TryGetValue(connectionName!, out var setCon))
            throw new InvalidOperationException("Соединение с БД не настроено");

        var connection = (DbConnection)Activator.CreateInstance(setCon.ConnectionType)!;
        connection.ConnectionString = setCon.ConnectionString;
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Получение экземпляра открытого соединения с БД
    /// </summary>
    /// <param name="connectionName">Имя соединения в коллекции</param>
    /// <returns></returns>
    public static DbCommand CreateCommand(string? connectionName = null)
    {
        if (connectionName.IsNullOrWhiteSpace())
            connectionName = defaultNameConnection;

        return !dcsb.TryGetValue(connectionName!, out var setCon)
            ? throw new InvalidOperationException("Соединение с БД не настроено")
            : (DbCommand)Activator.CreateInstance(setCon.CommandType)!;
    }

    /// <summary>
    /// Имена соединений, присутствующие в коллекции
    /// </summary>
    public static ReadOnlyCollection<string> NamesConnectionOnCollection => new(dcsb.Keys.ToArray());
}
