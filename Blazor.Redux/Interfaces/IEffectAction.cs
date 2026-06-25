namespace Blazor.Redux.Interfaces;

/// <summary>
/// Marker interface for an action produced by an effect that can be dispatched
/// back into the Redux pipeline. Implementations wrap the actual action along
/// with its target slice type for type-safe dispatch from effect handlers.
/// </summary>
public interface IEffectAction
{
    /// <summary>
    /// Dispatches the wrapped action through the given dispatcher.
    /// </summary>
    /// <param name="dispatcher">The dispatcher to use for dispatch.</param>
    void Dispatch(IDispatcher dispatcher);
}
