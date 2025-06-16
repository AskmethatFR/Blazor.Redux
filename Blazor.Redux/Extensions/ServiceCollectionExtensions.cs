using System.Reflection;
using Blazor.Redux.Core;
using Blazor.Redux.Dispatching;
using Blazor.Redux.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Blazor.Redux.Extensions;

public static class ServiceCollectionExtensions
{
    

    public static IServiceCollection AddBlazorRedux(this IServiceCollection services, BlazorReduxOption options)
    {
        var store = Store.Init(options.Slices);
        services.AddSingleton(store);
        services.AddSingleton<IObservableStore>(store);
        services.AddScoped<IDispatcher, Dispatcher>();
        services.AddScoped<IAsyncDispatcher, AsyncDispatcher>();

        AddReducers(services, options.Assembly);
        return services;
    }

    /// <summary>
    /// Scanne une assembly pour découvrir et enregistrer automatiquement tous les reducers synchrones et asynchrones
    /// </summary>
    /// <param name="services">Collection de services</param>
    /// <param name="assembly">Assembly à scanner (utilise l'assembly appelante si null)</param>
    /// <returns>Collection de services pour le chaînage</returns>
    public static IServiceCollection AddReducers(
        this IServiceCollection services,
        Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();

        var reducerTypes = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.GetInterfaces()
                .Any(i => i.IsGenericType && (
                    i.GetGenericTypeDefinition() == typeof(IReducer<,>) ||
                    i.GetGenericTypeDefinition() == typeof(IAsyncReducer<,>))))
            .ToList();

        foreach (var reducerType in reducerTypes)
        {
            // Enregistrer les interfaces IReducer<,> (synchrones)
            var syncInterfaces = reducerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReducer<,>));

            foreach (var interfaceType in syncInterfaces)
            {
                services.AddScoped(interfaceType, reducerType);
            }

            // Enregistrer les interfaces IAsyncReducer<,> (asynchrones)
            var asyncInterfaces = reducerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncReducer<,>));

            foreach (var interfaceType in asyncInterfaces)
            {
                services.AddScoped(interfaceType, reducerType);
            }
        }

        return services;
    }
}