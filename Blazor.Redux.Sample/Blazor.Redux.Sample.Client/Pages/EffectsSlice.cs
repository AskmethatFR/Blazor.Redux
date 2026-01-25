using System.Reactive.Linq;
using Blazor.Redux.Core;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Sample.Client.Pages;

public record EffectsSlice : ISlice
{
    public int Value { get; init; }
    public int Pending { get; init; }
    public string? Message { get; init; }
}

public record QueueIncrementAction(int Amount, int DelayMs) : IAction;

public record IncrementNowAction(int Amount) : IAction;

public sealed class QueueIncrementReducer : IReducer<EffectsSlice, QueueIncrementAction>
{
    public EffectsSlice Reduce(EffectsSlice state, QueueIncrementAction action)
    {
        return state with
        {
            Pending = state.Pending + 1,
            Message = $"Queued +{action.Amount} ({action.DelayMs}ms)"
        };
    }
}

public sealed class IncrementNowReducer : IReducer<EffectsSlice, IncrementNowAction>
{
    public EffectsSlice Reduce(EffectsSlice state, IncrementNowAction action)
    {
        var pending = state.Pending > 0 ? state.Pending - 1 : 0;
        return state with
        {
            Value = state.Value + action.Amount,
            Pending = pending,
            Message = $"Applied +{action.Amount}"
        };
    }
}

public sealed class QueueIncrementEffect : IEffect
{
    public IObservable<IEffectAction> Handle(
        IObservable<IAction> actions,
        IObservable<RootStateSnapshot> state)
    {
        return actions
            .OfType<QueueIncrementAction>()
            .SelectMany(action =>
                Observable.Timer(TimeSpan.FromMilliseconds(action.DelayMs))
                    .Select(_ =>
                        (IEffectAction)new EffectAction<EffectsSlice, IncrementNowAction>(
                            new IncrementNowAction(action.Amount))));
    }
}
