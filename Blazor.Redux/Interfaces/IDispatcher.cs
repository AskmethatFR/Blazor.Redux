namespace Blazor.Redux.Interfaces;

/// <summary>
/// Synchronous dispatcher that sends actions through the Redux pipeline,
/// including middleware, reducers, and effects.
/// Dispatched actions are serialized through a dispatch queue to ensure ordering.
/// </summary>
public interface IDispatcher
{
    /// <summary>
    /// Dispatches an action synchronously for the given slice type.
    /// </summary>
    /// <typeparam name="TSlice">Target slice type.</typeparam>
    /// <typeparam name="TAction">Action type.</typeparam>
    /// <param name="action">Action to dispatch.</param>
    void Dispatch<TSlice, TAction>(TAction action)
        where TSlice : class, ISlice
        where TAction : class, IAction;
}
