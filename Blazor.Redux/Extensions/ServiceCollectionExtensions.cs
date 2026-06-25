using System.Reflection;
using Blazor.Redux.Core;
using Blazor.Redux.Core.Events;
using Blazor.Redux.Dispatching;
using Blazor.Redux.Interfaces;
using Blazor.Redux.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Blazor.Redux.Extensions;

/// <summary>
/// Extension methods for registering Blazor.Redux services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Blazor.Redux services (store, dispatchers, reducers, effects, serializers)
    /// in the service collection using the provided options.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">Configuration options including slices, assembly, and middleware.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddBlazorRedux(this IServiceCollection services, BlazorReduxOption options)
    {
        var store = Store.Init(options.SnapshotStrategy, options.Slices);
        services.AddSingleton(store);
        services.AddSingleton<IObservableStore>(store);
        services.AddSingleton<IRootStateStore>(store);
        services.AddSingleton<IStateSnapshotApplier>(store);
        services.AddSingleton<IReduxSerializer>(provider =>
        {
            var assembly = options.Assembly ?? Assembly.GetCallingAssembly();
            return new ReduxJsonSerializer(searchAssemblies: [assembly]);
        });
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
        services.AddScoped<IReducerRegistry, ReducerRegistry>();
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
    /// Scans an assembly and registers all reducers (sync and async) in DI.
    /// A reducer cannot implement both <see cref="IReducer{TS,TA}"/> and
    /// <see cref="IAsyncReducer{TS,TA}"/> for the same (slice, action) pair.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="assembly">Assembly to scan (uses calling assembly if null).</param>
    /// <returns>Service collection for chaining.</returns>
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

            ValidateReducerInterfaces(reducerType, syncInterfaces, asyncInterfaces);

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

    private static void ValidateReducerInterfaces(
        Type reducerType,
        IEnumerable<Type> syncInterfaces,
        IEnumerable<Type> asyncInterfaces)
    {
        var syncKeys = syncInterfaces.Select(GetReducerKey).ToHashSet();
        var asyncKeys = asyncInterfaces.Select(GetReducerKey).ToHashSet();

        var duplicate = syncKeys.Intersect(asyncKeys).FirstOrDefault();
        if (duplicate == default)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Reducer '{reducerType.Name}' implements both IReducer and IAsyncReducer " +
            $"for slice '{duplicate.SliceType.Name}' and action '{duplicate.ActionType.Name}'. " +
            "Use only one interface per (slice, action) pair.");
    }

    private static (Type SliceType, Type ActionType) GetReducerKey(Type reducerInterface)
    {
        var arguments = reducerInterface.GetGenericArguments();
        return (arguments[0], arguments[1]);
    }

    /// <summary>
    /// Scans an assembly and registers all effects (<see cref="IEffect"/> implementations) in DI.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="assembly">Assembly to scan (uses calling assembly if null).</param>
    /// <returns>Service collection for chaining.</returns>
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
