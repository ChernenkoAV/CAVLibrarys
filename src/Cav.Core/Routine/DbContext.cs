using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Reflection;

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
    private static ConcurrentDictionary<string, SettingConnection> dcsb = new();
    private struct SettingConnection
    {
        public string ConnectionString { get; set; }
        public DbProviderFactory ProviderFactory { get; set; }
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
        if (typeConnection == null)
            throw new ArgumentNullException(nameof(typeConnection));

        if (!typeConnection.IsSubclassOf(typeof(DbConnection)))
            throw new ArgumentException("typeConnection не является наследником DbConnection");

        if (connectionString.IsNullOrWhiteSpace())
            throw new ArgumentNullException(nameof(connectionString));

        if (connectionName.IsNullOrWhiteSpace())
            connectionName = defaultNameConnection;

        using var conn = (DbConnection)Activator.CreateInstance(typeConnection)!;
        conn.ConnectionString = connectionString;
        conn.Open();

        var pinfo = conn.GetType().GetProperty("DbProviderFactory", BindingFlags.NonPublic | BindingFlags.Instance);
        var setCon = new SettingConnection()
        {
            ConnectionString = connectionString,
            ProviderFactory = (pinfo!.GetValue(conn) as DbProviderFactory)!
        };

        dcsb.TryRemove(connectionName!, out _);
        dcsb.TryAdd(connectionName!, setCon);
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

        var connection = setCon.ProviderFactory.CreateConnection();
        connection!.ConnectionString = setCon.ConnectionString;
        connection.Open();
        return connection;
    }

    internal static DbProviderFactory DbProviderFactory(string? connectionName = null)
    {
        if (connectionName.IsNullOrWhiteSpace())
            connectionName = defaultNameConnection;

        return !dcsb.TryGetValue(connectionName!, out var setCon)
            ? throw new InvalidOperationException("Соединение с БД не настроено")
            : setCon.ProviderFactory;
    }

    /// <summary>
    /// Имена соединений, присутствующие в коллекции
    /// </summary>
    public static ReadOnlyCollection<string> NamesConnectionOnCollection => new(dcsb.Keys.ToArray());
}
