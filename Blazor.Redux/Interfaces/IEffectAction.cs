namespace Blazor.Redux.Interfaces;

/// <summary>
/// Marker interface for an action produced by an effect that can be dispatched
/// back into the Redux pipeline. Implementations wrap the actual action along
/// with its target slice type for type-safe dispatch from effect handlers.
/// </summary>
public interface IEffectAction
{
    void Dispatch(IDispatcher dispatcher);

    Task DispatchAsync(IAsyncDispatcher dispatcher);
}
