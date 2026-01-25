using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Core;

public sealed record EffectAction<TSlice, TAction>(TAction Action) : IEffectAction
    where TSlice : class, ISlice
    where TAction : class, IAction
{
    public void Dispatch(IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(Action);

        dispatcher.Dispatch<TSlice, TAction>(Action);
    }
}
