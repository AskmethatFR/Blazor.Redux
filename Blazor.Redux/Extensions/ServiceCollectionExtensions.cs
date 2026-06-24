using System.Reflection;
using Blazor.Redux.Core;
using Blazor.Redux.Core.Events;
using Blazor.Redux.Dispatching;
using Blazor.Redux.Interfaces;
using Blazor.Redux.Serialization;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Blazor.Redux.Extensions;

public static class ServiceCollectionExtensions
{
    

    public static IServiceCollection AddBlazorRedux(this IServiceCollection services, BlazorReduxOption options)
    {
        var store = Store.Init(options.SnapshotStrategy, options.Slices);
        services.AddSingleton(store);
        services.AddSingleton<IObservableStore>(store);
        services.AddSingleton<IRootStateStore>(store);
        services.AddSingleton<IStateSnapshotApplier>(store);
        services.AddSingleton<IReduxSerializer>(provider =>
            new ReduxJsonSerializer());
        services.AddSingleton<DispatchQueue>();
        services.AddSingleton(provider => new ActionStream(options.ReplayLastAction));
        services.AddSingleton<IActionStream>(provider => provider.GetRequiredService<ActionStream>());
        services.AddSingleton(provider => new EffectsPipeline(
            provider.GetRequiredService<IActionStream>(),
            provider.GetRequiredService<IRootStateStore>(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetServices<IEffect>(),
            options.EffectsScheduler,
            options.EffectsCancellationStrategy,
            provider.GetService<Microsoft.Extensions.Logging.ILogger<EffectsPipeline>>()));
        services.AddScoped<IDispatcher, Dispatcher>();
        services.AddScoped<IAsyncDispatcher, AsyncDispatcher>();
        services.AddScoped<IStoreEventPublisher, StoreEventPublisher>();
        RegisterMiddlewares(services, options.Middlewares);

        AddReducers(services, options.Assembly);
        AddEffects(services, options.Assembly);
        return services;
    }

    private static void RegisterMiddlewares(IServiceCollection services, IEnumerable<Type> middlewares)
    {
        var middlewareList = middlewares?.ToList() ?? new List<Type>();
        if (middlewareList.Count == 0)
        {
            return;
        }

        foreach (var middlewareType in middlewareList.Distinct())
        {
            if (!typeof(IDispatchMiddleware).IsAssignableFrom(middlewareType))
            {
                throw new InvalidOperationException(
                    $"Middleware type {middlewareType.Name} must implement IDispatchMiddleware.");
            }

            services.AddScoped(typeof(IDispatchMiddleware), middlewareType);
        }
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
            var syncInterfaces = reducerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReducer<,>))
                .ToList();

            var asyncInterfaces = reducerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncReducer<,>))
                .ToList();

            var syncKeys = syncInterfaces.Select(i => i.GetGenericArguments() switch { var a => (a[0], a[1]) }).ToHashSet();
            var asyncKeys = asyncInterfaces.Select(i => i.GetGenericArguments() switch { var a => (a[0], a[1]) }).ToHashSet();

            var duplicate = syncKeys.Intersect(asyncKeys).FirstOrDefault();
            if (duplicate != default)
            {
                throw new InvalidOperationException(
                    $"Reducer '{reducerType.Name}' implements both IReducer and IAsyncReducer " +
                    $"for slice '{duplicate.Item1.Name}' and action '{duplicate.Item2.Name}'. " +
                    "Use only one interface per (slice, action) pair.");
            }

            foreach (var interfaceType in syncInterfaces)
            {
                services.AddScoped(interfaceType, reducerType);
            }

            foreach (var interfaceType in asyncInterfaces)
            {
                services.AddScoped(interfaceType, reducerType);
            }
        }

        return services;
    }

    /// <summary>
    /// Scanne une assembly pour découvrir et enregistrer automatiquement tous les effects
    /// </summary>
    /// <param name="services">Collection de services</param>
    /// <param name="assembly">Assembly à scanner (utilise l'assembly appelante si null)</param>
    /// <returns>Collection de services pour le chaînage</returns>
    public static IServiceCollection AddEffects(
        this IServiceCollection services,
        Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();

        var effectTypes = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => typeof(IEffect).IsAssignableFrom(type))
            .ToList();

        foreach (var effectType in effectTypes)
        {
            services.AddSingleton(typeof(IEffect), effectType);
        }

        return services;
    }
}
