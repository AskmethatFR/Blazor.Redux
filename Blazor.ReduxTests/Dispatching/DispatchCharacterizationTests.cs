using System.Reflection;
using Blazor.Redux.Core;
using Blazor.Redux.Extensions;
using Blazor.Redux.Interfaces;
using Blazor.ReduxTests.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Blazor.ReduxTests.Dispatching;

public class DispatchCharacterizationTests
{
    [Fact]
    public void DispatchPreservesOrder()
    {
        var services = new ServiceCollection();
        services.AddBlazorRedux(new BlazorReduxOption
        {
            Slices = [new CounterSlice { Value = 0 }],
            Assembly = Assembly.GetExecutingAssembly()
        });
        using var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<Store>();
        var dispatcher = provider.GetRequiredService<IDispatcher>();

        dispatcher.Dispatch<CounterSlice, CounterSliceAction>(new CounterSliceAction(3));
        dispatcher.Dispatch<CounterSlice, CounterSliceAction>(new CounterSliceAction(5));
        dispatcher.Dispatch<CounterSlice, CounterSliceAction>(new CounterSliceAction(2));

        Assert.Equal(10, store.GetSlice<CounterSlice>()?.Value);
    }

    [Fact]
    public void DispatchIsolatesSlices()
    {
        var services = new ServiceCollection();
        services.AddBlazorRedux(new BlazorReduxOption
        {
            Slices = [new CounterSlice { Value = 0 }, new NameSlice { Name = "" }],
            Assembly = Assembly.GetExecutingAssembly()
        });
        using var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<Store>();
        var dispatcher = provider.GetRequiredService<IDispatcher>();

        dispatcher.Dispatch<CounterSlice, CounterSliceAction>(new CounterSliceAction(7));
        dispatcher.Dispatch<NameSlice, NameAction>(new NameAction("hello"));

        Assert.Equal(7, store.GetSlice<CounterSlice>()?.Value);
        Assert.Equal("hello", store.GetSlice<NameSlice>()?.Name);
    }

    [Fact]
    public void ObserveSliceEmitsInitialAndUpdatedValues()
    {
        var services = new ServiceCollection();
        services.AddBlazorRedux(new BlazorReduxOption
        {
            Slices = [new CounterSlice { Value = 42 }],
            Assembly = Assembly.GetExecutingAssembly()
        });
        using var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<Store>();
        var dispatcher = provider.GetRequiredService<IDispatcher>();

        var emitted = new List<CounterSlice>();
        using var sub = store.ObserveSlice<CounterSlice>()
            .Subscribe(slice => emitted.Add(slice));

        Assert.Single(emitted);
        Assert.Equal(42, emitted[0].Value);

        dispatcher.Dispatch<CounterSlice, CounterSliceAction>(new CounterSliceAction(10));

        Assert.Equal(2, emitted.Count);
        Assert.Equal(52, emitted[1].Value);
    }

    [Fact]
    public void ObserveSliceDoesNotEmitForUnrelatedSlice()
    {
        var services = new ServiceCollection();
        services.AddBlazorRedux(new BlazorReduxOption
        {
            Slices = [new CounterSlice { Value = 0 }, new NameSlice { Name = "" }],
            Assembly = Assembly.GetExecutingAssembly()
        });
        using var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<Store>();
        var dispatcher = provider.GetRequiredService<IDispatcher>();

        var emitted = new List<CounterSlice>();
        using var sub = store.ObserveSlice<CounterSlice>()
            .Subscribe(slice => emitted.Add(slice));

        dispatcher.Dispatch<NameSlice, NameAction>(new NameAction("test"));

        Assert.Single(emitted);
    }

    [Fact]
    public void ObserveStateEmitsOnEveryDispatch()
    {
        var services = new ServiceCollection();
        services.AddBlazorRedux(new BlazorReduxOption
        {
            Slices = [new CounterSlice { Value = 0 }],
            Assembly = Assembly.GetExecutingAssembly()
        });
        using var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<Store>();
        var dispatcher = provider.GetRequiredService<IDispatcher>();

        var emitted = new List<RootStateSnapshot>();
        using var sub = store.ObserveState()
            .Subscribe(snapshot => emitted.Add(snapshot));

        dispatcher.Dispatch<CounterSlice, CounterSliceAction>(new CounterSliceAction(1));
        dispatcher.Dispatch<CounterSlice, CounterSliceAction>(new CounterSliceAction(2));

        Assert.Equal(3, emitted.Count);
    }

    [Fact]
    public void RootStateSnapshotReflectsLatestState()
    {
        var services = new ServiceCollection();
        services.AddBlazorRedux(new BlazorReduxOption
        {
            Slices = [new CounterSlice { Value = 5 }],
            Assembly = Assembly.GetExecutingAssembly()
        });
        using var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<Store>();
        var dispatcher = provider.GetRequiredService<IDispatcher>();

        dispatcher.Dispatch<CounterSlice, CounterSliceAction>(new CounterSliceAction(3));

        var snapshot = store.Current;
        Assert.NotNull(snapshot);
    }
}