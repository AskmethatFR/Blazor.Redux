using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Core;

/// <summary>
/// Typed effect action that wraps a concrete action with its target slice type
/// for type-safe dispatch from effect handlers.
/// </summary>
/// <typeparam name="TSlice">Target slice type for the wrapped action.</typeparam>
/// <typeparam name="TAction">Wrapped action type.</typeparam>
/// <param name="Action">The action to dispatch.</param>
public sealed record EffectAction<TSlice, TAction>(TAction Action) : IEffectAction
    where TSlice : class, ISlice
    where TAction : class, IAction
{
    /// <summary>
    /// Dispatches the wrapped action through the given dispatcher.
    /// </summary>
    /// <param name="dispatcher">The dispatcher to use.</param>
    public void Dispatch(IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(Action);

        dispatcher.Dispatch<TSlice, TAction>(Action);
    }
}
