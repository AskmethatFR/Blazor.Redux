using System.ComponentModel;
using System.Reactive.Linq;
using System.Reflection;
using Blazor.Redux;
using Blazor.Redux.Core;
using Blazor.Redux.Extensions;
using Blazor.Redux.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Blazor.ReduxTests.Effects;

[Description("IDK why not working ( infinite )")]
public class EffectsPipelineTests
{
    [Fact]
    public void EffectsPipelineDispatchesActionsFromStream()
    {
        var services = new ServiceCollection();
        services.AddBlazorRedux(new BlazorReduxOption
        {
            Slices = [new EffectSlice { Value = 0 }],
            Assembly = Assembly.GetExecutingAssembly()
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDispatcher>();
        var store = provider.GetRequiredService<Store>();
        var actionStream = provider.GetRequiredService<IActionStream>();

        using var actionReceived = new ManualResetEventSlim();
        using var stateUpdated = new ManualResetEventSlim();

        using var actionSubscription = actionStream.Actions
            .OfType<IncrementAction>()
            .Take(1)
            .Subscribe(_ => actionReceived.Set());

        using var stateSubscription = store.ObserveSlice<EffectSlice>()
            .Skip(1)
            .Subscribe(slice =>
            {
                if (slice.Value == 3)
                {
                    stateUpdated.Set();
                }
            });

        dispatcher.Dispatch<EffectSlice, TriggerAction>(new TriggerAction(3));

        Assert.True(actionReceived.Wait(TimeSpan.FromSeconds(2)));
        Assert.True(stateUpdated.Wait(TimeSpan.FromSeconds(2)));
        Assert.Equal(3, store.GetSlice<EffectSlice>()?.Value);
    }

    [Fact]
    public void EffectsPipelineHandlesMultipleQueuedActions()
    {
        var services = new ServiceCollection();
        services.AddBlazorRedux(new BlazorReduxOption
        {
            Slices = [new EffectSlice { Value = 0 }],
            Assembly = Assembly.GetExecutingAssembly()
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDispatcher>();
        var store = provider.GetRequiredService<Store>();

        using var completed = new ManualResetEventSlim();

        using var sub = store.ObserveSlice<EffectSlice>()
            .Skip(2)
            .Take(1)
            .Subscribe(slice =>
            {
                if (slice.Value == 3)
                {
                    completed.Set();
                }
            });

        dispatcher.Dispatch<EffectSlice, TriggerAction>(new TriggerAction(1));
        dispatcher.Dispatch<EffectSlice, TriggerAction>(new TriggerAction(2));

        Assert.True(completed.Wait(TimeSpan.FromSeconds(2)), "Effects should process all queued actions");
        Assert.Equal(3, store.GetSlice<EffectSlice>()?.Value);
    }

    [Fact]
    public void EffectsPipelineSupportsDebounce()
    {
        var services = new ServiceCollection();
        services.AddBlazorRedux(new BlazorReduxOption
        {
            Slices = [new DebounceSlice { Value = 0 }],
            Assembly = Assembly.GetExecutingAssembly()
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDispatcher>();
        var store = provider.GetRequiredService<Store>();

        using var completed = new ManualResetEventSlim();

        using var subscription = store.ObserveSlice<DebounceSlice>()
            .Skip(1)
            .Take(1)
            .Subscribe(slice =>
            {
                if (slice.Value == 3)
                {
                    completed.Set();
                }
            });

        dispatcher.Dispatch<DebounceSlice, DebounceAction>(new DebounceAction(1));
        dispatcher.Dispatch<DebounceSlice, DebounceAction>(new DebounceAction(2));
        dispatcher.Dispatch<DebounceSlice, DebounceAction>(new DebounceAction(3));

        Assert.True(completed.Wait(TimeSpan.FromSeconds(2)));
        Assert.Equal(3, store.GetSlice<DebounceSlice>()?.Value);
    }

    [Fact]
    public void EffectsPipelineSupportsCancellationWithSwitch()
    {
        var services = new ServiceCollection();
        services.AddBlazorRedux(new BlazorReduxOption
        {
            Slices = [new CancelSlice { Value = 0 }],
            Assembly = Assembly.GetExecutingAssembly()
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDispatcher>();
        var store = provider.GetRequiredService<Store>();

        using var completed = new ManualResetEventSlim();

        using var subscription = store.ObserveSlice<CancelSlice>()
            .Skip(1)
            .Take(1)
            .Subscribe(slice =>
            {
                if (slice.Value == 2)
                {
                    completed.Set();
                }
            });

        dispatcher.Dispatch<CancelSlice, CancelAction>(new CancelAction(1, 200));
        dispatcher.Dispatch<CancelSlice, CancelAction>(new CancelAction(2, 50));

        Assert.True(completed.Wait(TimeSpan.FromSeconds(2)));
        Thread.Sleep(150);
        Assert.Equal(2, store.GetSlice<CancelSlice>()?.Value);
    }

    [Fact]
    public void EffectsPipelineUsesRxSwitchCancellationStrategy()
    {
        var services = new ServiceCollection();
        services.AddBlazorRedux(new BlazorReduxOption
        {
            Slices = [new SwitchSlice { Value = 0 }],
            Assembly = Assembly.GetExecutingAssembly(),
            EffectsCancellationStrategy = EffectsCancellationStrategy.RxSwitch
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDispatcher>();
        var store = provider.GetRequiredService<Store>();

        using var completed = new ManualResetEventSlim();

        using var subscription = store.ObserveSlice<SwitchSlice>()
            .Skip(1)
            .Take(1)
            .Subscribe(slice =>
            {
                if (slice.Value == 2)
                {
                    completed.Set();
                }
            });

        dispatcher.Dispatch<SwitchSlice, SwitchAction>(new SwitchAction(1, 200));
        dispatcher.Dispatch<SwitchSlice, SwitchAction>(new SwitchAction(2, 50));

        Assert.True(completed.Wait(TimeSpan.FromSeconds(2)));
        Thread.Sleep(150);
        Assert.Equal(2, store.GetSlice<SwitchSlice>()?.Value);
    }
}

public record EffectSlice : ISlice
{
    public int Value { get; init; }
}

public record TriggerAction(int Amount) : IAction;

public record IncrementAction(int Amount) : IAction;

public sealed class TriggerEffect : IEffect
{
    public IObservable<IEffectAction> Handle(
        IObservable<IAction> actions,
        IObservable<RootStateSnapshot> state)
    {
        return actions
            .OfType<TriggerAction>()
            .Select(action =>
                (IEffectAction)new EffectAction<EffectSlice, IncrementAction>(
                    new IncrementAction(action.Amount)));
    }
}

public sealed class IncrementReducer : IReducer<EffectSlice, IncrementAction>
{
    public EffectSlice Reduce(EffectSlice state, IncrementAction action)
    {
        return state with { Value = state.Value + action.Amount };
    }
}

public record DebounceSlice : ISlice
{
    public int Value { get; init; }
}

public record DebounceAction(int Value) : IAction;

public record ApplyDebounceAction(int Value) : IAction;

public sealed class DebounceEffect : IEffect
{
    public IObservable<IEffectAction> Handle(
        IObservable<IAction> actions,
        IObservable<RootStateSnapshot> state)
    {
        return actions
            .OfType<DebounceAction>()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .Select(action =>
                (IEffectAction)new EffectAction<DebounceSlice, ApplyDebounceAction>(
                    new ApplyDebounceAction(action.Value)));
    }
}

public sealed class ApplyDebounceReducer : IReducer<DebounceSlice, ApplyDebounceAction>
{
    public DebounceSlice Reduce(DebounceSlice state, ApplyDebounceAction action)
    {
        return state with { Value = action.Value };
    }
}

public record CancelSlice : ISlice
{
    public int Value { get; init; }
}

public record CancelAction(int Value, int DelayMs) : IAction;

public record ApplyCancelAction(int Value) : IAction;

public sealed class CancelEffect : IEffect
{
    public IObservable<IEffectAction> Handle(
        IObservable<IAction> actions,
        IObservable<RootStateSnapshot> state)
    {
        return actions
            .OfType<CancelAction>()
            .Select(action =>
                Observable.Timer(TimeSpan.FromMilliseconds(action.DelayMs))
                    .Select(_ =>
                        (IEffectAction)new EffectAction<CancelSlice, ApplyCancelAction>(
                            new ApplyCancelAction(action.Value))))
            .Switch();
    }
}

public sealed class ApplyCancelReducer : IReducer<CancelSlice, ApplyCancelAction>
{
    public CancelSlice Reduce(CancelSlice state, ApplyCancelAction action)
    {
        return state with { Value = action.Value };
    }
}

public record SwitchSlice : ISlice
{
    public int Value { get; init; }
}

public record SwitchAction(int Value, int DelayMs) : IAction;

public record ApplySwitchAction(int Value) : IAction;

public sealed class SwitchEffect : ICancelableEffect
{
    public IObservable<IEffectAction> Handle(
        IObservable<IAction> actions,
        IObservable<RootStateSnapshot> state)
    {
        return Observable.Empty<IEffectAction>();
    }

    public IObservable<IObservable<IEffectAction>> HandleWithCancellation(
        IObservable<IAction> actions,
        IObservable<RootStateSnapshot> state)
    {
        return actions
            .OfType<SwitchAction>()
            .Select(action =>
                Observable.Timer(TimeSpan.FromMilliseconds(action.DelayMs))
                    .Select(_ =>
                        (IEffectAction)new EffectAction<SwitchSlice, ApplySwitchAction>(
                            new ApplySwitchAction(action.Value))));
    }
}

public sealed class ApplySwitchReducer : IReducer<SwitchSlice, ApplySwitchAction>
{
    public SwitchSlice Reduce(SwitchSlice state, ApplySwitchAction action)
    {
        return state with { Value = action.Value };
    }
}
