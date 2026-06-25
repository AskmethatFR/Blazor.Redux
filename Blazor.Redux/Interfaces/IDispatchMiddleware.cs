using Blazor.Redux.Core;

namespace Blazor.Redux.Interfaces;

/// <summary>
/// Middleware that intercepts dispatched actions before they reach reducers.
/// Middleware can modify, log, block, or transform actions in the pipeline.
/// Call <c>next</c> to pass control to the next middleware or terminal reducer.
/// </summary>
public interface IDispatchMiddleware
{
    /// <summary>
    /// Invokes the middleware for the given action.
    /// </summary>
    /// <typeparam name="TSlice">Target slice type.</typeparam>
    /// <typeparam name="TAction">Action type.</typeparam>
    /// <param name="action">Action being dispatched.</param>
    /// <param name="next">Delegate to invoke the next middleware or terminal.</param>
    /// <returns>Task representing the middleware execution.</returns>
    Task InvokeAsync<TSlice, TAction>(TAction action, Func<Task> next)
        where TSlice : class, ISlice
        where TAction : class, IAction;
}
