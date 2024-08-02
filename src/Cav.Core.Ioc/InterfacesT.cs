namespace Cav;

/// <summary>
/// Use this interface when you have different registration strategy, for example, you want to register concrete instance of an object
/// NOTE: Provide parameterless constructor for your class
/// </summary>
public interface IHasImplementationFactory
{
    /// <summary>
    /// Implementation factory
    /// </summary>
    /// <returns></returns>
    Func<IServiceProvider, object> GetFactory();
}

/// <summary>
/// Базовый интерфейс для сервисов
/// </summary>
public interface IService { }
/// <summary>
/// Время жизни - область
/// </summary>
public interface IScoped : IService { }
/// <summary>
/// Время жизни - область. Типизированный
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IScoped<T> : IScoped { }

/// <summary>
/// Время жизни - синглтон
/// </summary>
public interface ISingleton : IService { }
/// <summary>
/// Время жизни - синглтон. Типизированный
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ISingleton<T> : ISingleton { }
/// <summary>
/// Время жизни - временный
/// </summary>
public interface ITransient : IService { }
/// <summary>
/// Время жизни - временный. Типизированный
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ITransient<T> : ITransient { }
