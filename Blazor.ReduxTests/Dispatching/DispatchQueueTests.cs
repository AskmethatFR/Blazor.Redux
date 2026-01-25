using System.Reflection;
using Blazor.Redux.Extensions;
using Blazor.Redux.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Blazor.ReduxTests.Dispatching;

public class DispatchQueueTests
{
    [Fact]
    public async Task DispatchQueueSerializesConcurrentDispatches()
    {
        var services = new ServiceCollection();
        services.AddBlazorRedux(new BlazorReduxOption
        {
            Slices = [new ConcurrencySlice { Value = 0 }],
            Assembly = Assembly.GetExecutingAssembly()
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDispatcher>();

        ConcurrencyProbe.Reset();

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() =>
                dispatcher.Dispatch<ConcurrencySlice, ConcurrencyAction>(new ConcurrencyAction())))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, ConcurrencyProbe.MaxConcurrent);
    }

    [Fact]
    public async Task DispatchQueueIsThreadSafeForStateUpdates()
    {
        var services = new ServiceCollection();
        services.AddBlazorRedux(new BlazorReduxOption
        {
            Slices = [new DispatchQueueCounterSlice { Value = 0 }],
            Assembly = Assembly.GetExecutingAssembly()
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDispatcher>();
        var store = provider.GetRequiredService<Blazor.Redux.Core.Store>();

        var tasks = Enumerable.Range(0, 50)
            .Select(_ => Task.Run(() =>
                dispatcher.Dispatch<DispatchQueueCounterSlice, DispatchQueueIncrementAction>(new DispatchQueueIncrementAction())))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(50, store.GetSlice<DispatchQueueCounterSlice>()?.Value);
    }
}

public record ConcurrencySlice : ISlice
{
    public int Value { get; init; }
}

public record ConcurrencyAction : IAction;

public static class ConcurrencyProbe
{
    private static int _current;
    private static int _max;

    public static int MaxConcurrent => _max;

    public static void Reset()
    {
        _current = 0;
        _max = 0;
    }

    public static void Enter()
    {
        var current = Interlocked.Increment(ref _current);
        var initial = _max;
        while (current > initial)
        {
            var previous = Interlocked.CompareExchange(ref _max, current, initial);
            if (previous == initial)
            {
                break;
            }

            initial = previous;
        }
    }

    public static void Exit()
    {
        Interlocked.Decrement(ref _current);
    }
}

public sealed class ConcurrencyReducer : IReducer<ConcurrencySlice, ConcurrencyAction>
{
    public ConcurrencySlice Reduce(ConcurrencySlice slice, ConcurrencyAction action)
    {
        ConcurrencyProbe.Enter();
        try
        {
            Thread.Sleep(25);
            return slice with { Value = slice.Value + 1 };
        }
        finally
        {
            ConcurrencyProbe.Exit();
        }
    }
}

public record DispatchQueueCounterSlice : ISlice
{
    public int Value { get; init; }
    public bool IsLoading { get; init; }
    public string? Message { get; init; }
}

public record DispatchQueueIncrementAction : IAction;

public sealed class DispatchQueueIncrementReducer : IReducer<DispatchQueueCounterSlice, DispatchQueueIncrementAction>
{
    public DispatchQueueCounterSlice Reduce(DispatchQueueCounterSlice slice, DispatchQueueIncrementAction action)
    {
        return slice with { Value = slice.Value + 1 };
    }
}
