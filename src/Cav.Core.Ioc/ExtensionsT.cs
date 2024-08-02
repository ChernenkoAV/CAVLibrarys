using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Cav.Service;

/// <summary>
/// Расширения для коллекции сервисов
/// </summary>
public static class ServiceCollectionsExtensions
{
    #region Extensions

    /// <summary>
    /// Регистрация пемеченных элементов в указанных сборках
    /// </summary>
    /// <param name="services"></param>
    /// <param name="assemblies"></param>
    /// <returns></returns>
    public static IServiceCollection AddServices([NotNull] this IServiceCollection services, params Assembly[] assemblies)
    {
        var servicesToRegister = assemblies
            .SelectMany(x => x.GetTypes())
            .Where(t => typeof(IService).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        foreach (var serviceToRegister in servicesToRegister)
        {
            var (serviceType, implementationType) = getTypes(serviceToRegister);

            var lifetime = getLifetime(serviceToRegister);

            if (typeof(IHasImplementationFactory).IsAssignableFrom(serviceToRegister))
            {
                registerWithImplementationFactory(services, implementationType, lifetime);
            }
            else
            {
                registerWithTypes(services, serviceType, implementationType, lifetime);
            }
        }

        return services;
    }

    /// <summary>
    /// Регистрация всех помеченных типов в "вызываемой" сборке.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddServicesForCallingAssembly(this IServiceCollection services)
    {
        var callingAssembly = Assembly.GetCallingAssembly();

        return AddServices(services, callingAssembly);
    }

    #endregion

    #region Registration

    private static void registerWithTypes(IServiceCollection services, Type serviceType, Type implementationType, ServiceLifetime lifetime)
    {
        var descriptor = new ServiceDescriptor(serviceType, implementationType, lifetime);

        services.Add(descriptor);
    }

    private static void registerWithImplementationFactory(IServiceCollection services, Type implementationType, ServiceLifetime lifetime)
    {
        var factory = ((IHasImplementationFactory)Activator.CreateInstance(implementationType)!).GetFactory();
        var descriptor = new ServiceDescriptor(implementationType, factory, lifetime);

        services.Add(descriptor);
    }

    #endregion Registration

    #region Helpers

    private static (Type serviceType, Type implementationType) getTypes(Type serviceToRegister)
    {
        var genericInterface = serviceToRegister
            .GetInterfaces()
            .FirstOrDefault(x => x.IsGenericType && typeof(IService).IsAssignableFrom(x));

        return (genericInterface != null
            ? genericInterface.GetGenericArguments()[0]
            : serviceToRegister, serviceToRegister);
    }

    private static ServiceLifetime getLifetime(Type serviceToRegister)
    {
        var lifetime = ServiceLifetime.Transient;

        if (typeof(IScoped).IsAssignableFrom(serviceToRegister))
        {
            lifetime = ServiceLifetime.Scoped;
        }
        else if (typeof(ISingleton).IsAssignableFrom(serviceToRegister))
        {
            lifetime = ServiceLifetime.Singleton;
        }

        return lifetime;
    }

    #endregion Helpers
}
