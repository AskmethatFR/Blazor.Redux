namespace Blazor.Redux.Interfaces;

/// <summary>
/// Asynchronous dispatcher that sends actions through the Redux pipeline
/// and supports cancellation. Use for dispatching actions that trigger
/// async reducers or when cancellation is needed.
/// </summary>
public interface IAsyncDispatcher
{
    /// <summary>
    /// Dispatches an action asynchronously with optional cancellation support.
    /// </summary>
    /// <typeparam name="TSlice">Target slice type.</typeparam>
    /// <typeparam name="TAction">Action type.</typeparam>
    /// <param name="action">Action to dispatch.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Task representing the asynchronous dispatch operation.</returns>
    Task DispatchAsync<TSlice, TAction>(TAction action, CancellationToken cancellationToken = default)
        where TSlice : class, ISlice
        where TAction : class, IAction;
}
