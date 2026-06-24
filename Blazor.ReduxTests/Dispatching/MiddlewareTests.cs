using System.Reflection;
using Blazor.Redux.Extensions;
using Blazor.Redux.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Blazor.ReduxTests.Dispatching;

public class MiddlewareTests
{
    [Fact]
    public void MiddlewareExecutesInRegisteredOrder()
    {
        MiddlewareProbe.Reset();
        var services = new ServiceCollection();
        services.AddBlazorRedux(new BlazorReduxOption
        {
            Slices = [new MiddlewareSlice { Value = 0 }],
            Assembly = Assembly.GetExecutingAssembly(),
            Middlewares = new List<Type> { typeof(LogMiddlewareA), typeof(LogMiddlewareB) }
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDispatcher>();
        var store = provider.GetRequiredService<Blazor.Redux.Core.Store>();

        dispatcher.Dispatch<MiddlewareSlice, MiddlewareAction>(new MiddlewareAction(5));

        Assert.Equal(["A-before", "B-before", "B-after", "A-after"], MiddlewareProbe.Calls);
        Assert.Equal(5, store.GetSlice<MiddlewareSlice>()?.Value);
    }

    [Fact]
    public void MiddlewareCanShortCircuit()
    {
        var services = new ServiceCollection();
        services.AddBlazorRedux(new BlazorReduxOption
        {
            Slices = [new MiddlewareSlice { Value = 0 }],
            Assembly = Assembly.GetExecutingAssembly(),
            Middlewares = new List<Type> { typeof(ShortCircuitMiddleware) }
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDispatcher>();
        var store = provider.GetRequiredService<Blazor.Redux.Core.Store>();

        dispatcher.Dispatch<MiddlewareSlice, MiddlewareAction>(new MiddlewareAction(5));

        Assert.Equal(0, store.GetSlice<MiddlewareSlice>()?.Value);
    }

    [Fact]
    public async Task MiddlewareSupportsAsync()
    {
        MiddlewareProbe.Reset();
        var services = new ServiceCollection();
        services.AddBlazorRedux(new BlazorReduxOption
        {
            Slices = [new MiddlewareSlice { Value = 0 }],
            Assembly = Assembly.GetExecutingAssembly(),
            Middlewares = new List<Type> { typeof(AsyncMiddleware) }
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IAsyncDispatcher>();
        var store = provider.GetRequiredService<Blazor.Redux.Core.Store>();

        await dispatcher.DispatchAsync<MiddlewareSlice, MiddlewareAction>(new MiddlewareAction(3));

        Assert.Equal(["Async-before", "Async-after"], MiddlewareProbe.Calls);
        Assert.Equal(3, store.GetSlice<MiddlewareSlice>()?.Value);
    }

    [Fact]
    public void SyncDispatcherRejectsAsyncMiddleware()
    {
        var services = new ServiceCollection();
        services.AddBlazorRedux(new BlazorReduxOption
        {
            Slices = [new MiddlewareSlice { Value = 0 }],
            Assembly = Assembly.GetExecutingAssembly(),
            Middlewares = new List<Type> { typeof(AsyncMiddleware) }
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDispatcher>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            dispatcher.Dispatch<MiddlewareSlice, MiddlewareAction>(new MiddlewareAction(3)));

        Assert.Contains("Use IAsyncDispatcher", exception.Message);
    }

    [Fact]
    public void MiddlewareExceptionBubblesUpAndStopsReducer()
    {
        var services = new ServiceCollection();
        services.AddBlazorRedux(new BlazorReduxOption
        {
            Slices = [new MiddlewareSlice { Value = 0 }],
            Assembly = Assembly.GetExecutingAssembly(),
            Middlewares = new List<Type> { typeof(ThrowingMiddleware) }
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDispatcher>();
        var store = provider.GetRequiredService<Blazor.Redux.Core.Store>();

        Assert.Throws<InvalidOperationException>(() =>
            dispatcher.Dispatch<MiddlewareSlice, MiddlewareAction>(new MiddlewareAction(1)));

        Assert.Equal(0, store.GetSlice<MiddlewareSlice>()?.Value);
    }
}

public record MiddlewareSlice : ISlice
{
    public int Value { get; init; }
}

public record MiddlewareAction(int Amount) : IAction;

public sealed class MiddlewareReducer : IReducer<MiddlewareSlice, MiddlewareAction>
{
    public MiddlewareSlice Reduce(MiddlewareSlice slice, MiddlewareAction action)
    {
        return slice with { Value = slice.Value + action.Amount };
    }
}

public static class MiddlewareProbe
{
    private static readonly List<string> _calls = new();
    public static IReadOnlyList<string> Calls => _calls;

    public static void Reset() => _calls.Clear();

    public static void Add(string value) => _calls.Add(value);
}

public sealed class LogMiddlewareA : IDispatchMiddleware
{
    public async Task InvokeAsync<TSlice, TAction>(TAction action, Func<Task> next)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        MiddlewareProbe.Add("A-before");
        await next();
        MiddlewareProbe.Add("A-after");
    }
}

public sealed class LogMiddlewareB : IDispatchMiddleware
{
    public async Task InvokeAsync<TSlice, TAction>(TAction action, Func<Task> next)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        MiddlewareProbe.Add("B-before");
        await next();
        MiddlewareProbe.Add("B-after");
    }
}

public sealed class ShortCircuitMiddleware : IDispatchMiddleware
{
    public Task InvokeAsync<TSlice, TAction>(TAction action, Func<Task> next)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        // Ne pas appeler next => les reducers ne s'exécutent pas
        return Task.CompletedTask;
    }
}

public sealed class AsyncMiddleware : IDispatchMiddleware
{
    public async Task InvokeAsync<TSlice, TAction>(TAction action, Func<Task> next)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        MiddlewareProbe.Add("Async-before");
        await Task.Delay(10);
        await next();
        MiddlewareProbe.Add("Async-after");
    }
}

public sealed class ThrowingMiddleware : IDispatchMiddleware
{
    public Task InvokeAsync<TSlice, TAction>(TAction action, Func<Task> next)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        throw new InvalidOperationException("boom");
    }
}
