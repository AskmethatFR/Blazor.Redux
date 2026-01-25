using System.Reactive.Linq;
using System.Reflection;
using Blazor.Redux.Core;
using Blazor.Redux.Extensions;
using Blazor.Redux.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Blazor.ReduxTests.Core;

public class ActionStreamTests
{
    [Fact]
    public void ActionStreamDoesNotReplayLastActionByDefault()
    {
        var services = new ServiceCollection();
        services.AddBlazorRedux(new BlazorReduxOption
        {
            Slices = [new StreamSlice()],
            Assembly = Assembly.GetExecutingAssembly()
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDispatcher>();
        var actionStream = provider.GetRequiredService<IActionStream>();

        dispatcher.Dispatch<StreamSlice, StreamAction>(new StreamAction());

        using var received = new ManualResetEventSlim();
        using var subscription = actionStream.Actions
            .OfType<StreamAction>()
            .Take(1)
            .Subscribe(_ => received.Set());

        Assert.False(received.Wait(TimeSpan.FromMilliseconds(200)));
    }

    [Fact]
    public void ActionStreamReplaysLastActionWhenEnabled()
    {
        var services = new ServiceCollection();
        services.AddBlazorRedux(new BlazorReduxOption
        {
            Slices = [new StreamSlice()],
            ReplayLastAction = true,
            Assembly = Assembly.GetExecutingAssembly()
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDispatcher>();
        var actionStream = provider.GetRequiredService<IActionStream>();

        dispatcher.Dispatch<StreamSlice, StreamAction>(new StreamAction());

        using var received = new ManualResetEventSlim();
        using var subscription = actionStream.Actions
            .OfType<StreamAction>()
            .Take(1)
            .Subscribe(_ => received.Set());

        Assert.True(received.Wait(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task ActionStreamPublishesActionsFromAsyncDispatch()
    {
        var services = new ServiceCollection();
        services.AddBlazorRedux(new BlazorReduxOption
        {
            Slices = [new AsyncStreamSlice { Value = 0 }],
            Assembly = Assembly.GetExecutingAssembly()
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IAsyncDispatcher>();
        var actionStream = provider.GetRequiredService<IActionStream>();

        using var received = new ManualResetEventSlim();
        using var subscription = actionStream.Actions
            .OfType<AsyncStreamAction>()
            .Take(1)
            .Subscribe(_ => received.Set());

        await dispatcher.DispatchAsync<AsyncStreamSlice, AsyncStreamAction>(new AsyncStreamAction(3));

        Assert.True(received.Wait(TimeSpan.FromSeconds(1)));
    }

    //[Fact]
    //Same, this test run undefinitly
    public void ActionStreamPublishesEffectGeneratedActions()
    {
        var services = new ServiceCollection();
        services.AddBlazorRedux(new BlazorReduxOption
        {
            Slices = [new EffectStreamSlice { Value = 0 }],
            Assembly = Assembly.GetExecutingAssembly()
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDispatcher>();
        var actionStream = provider.GetRequiredService<IActionStream>();

        using var triggerReceived = new ManualResetEventSlim();
        using var effectReceived = new ManualResetEventSlim();

        using var subscription = actionStream.Actions.Subscribe(action =>
        {
            switch (action)
            {
                case TriggerStreamAction:
                    triggerReceived.Set();
                    break;
                case EffectStreamAction:
                    effectReceived.Set();
                    break;
            }
        });

        dispatcher.Dispatch<EffectStreamSlice, TriggerStreamAction>(new TriggerStreamAction(5));

        Assert.True(triggerReceived.Wait(TimeSpan.FromSeconds(1)));
        Assert.True(effectReceived.Wait(TimeSpan.FromSeconds(2)));
    }
}

public record StreamSlice : ISlice;

public record StreamAction : IAction;

public record AsyncStreamSlice : ISlice
{
    public int Value { get; init; }
}

public record AsyncStreamAction(int Amount) : IAction;

public sealed class AsyncStreamReducer : IAsyncReducer<AsyncStreamSlice, AsyncStreamAction>
{
    public Task<AsyncStreamSlice> ReduceAsync(AsyncStreamSlice slice, AsyncStreamAction action)
    {
        return Task.FromResult(slice with { Value = slice.Value + action.Amount });
    }
}

public record EffectStreamSlice : ISlice
{
    public int Value { get; init; }
}

public record TriggerStreamAction(int Amount) : IAction;

public record EffectStreamAction(int Amount) : IAction;

public sealed class TriggerStreamEffect : IEffect
{
    public IObservable<IEffectAction> Handle(
        IObservable<IAction> actions,
        IObservable<Blazor.Redux.Core.RootStateSnapshot> state)
    {
        return actions
            .OfType<TriggerStreamAction>()
            .Select(action =>
                (IEffectAction)new EffectAction<EffectStreamSlice, EffectStreamAction>(
                    new EffectStreamAction(action.Amount)));
    }
}

public sealed class EffectStreamReducer : IReducer<EffectStreamSlice, EffectStreamAction>
{
    public EffectStreamSlice Reduce(EffectStreamSlice state, EffectStreamAction action)
    {
        return state with { Value = state.Value + action.Amount };
    }
}
