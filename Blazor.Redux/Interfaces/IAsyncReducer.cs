namespace Blazor.Redux.Interfaces;

/// <summary>
/// Asynchronous reducer that produces new state from current state and an action.
/// Use for operations that require async work (e.g., fetching data) before producing state.
/// </summary>
/// <typeparam name="TS">The slice type this reducer operates on.</typeparam>
/// <typeparam name="TA">The action type this reducer handles.</typeparam>
public interface IAsyncReducer<TS, TA>
    where TS : class, ISlice
    where TA : class, IAction
{
    /// <summary>
    /// Reduces the given slice and action into a new slice state asynchronously.
    /// Maintains purity within the async boundary where practical.
    /// </summary>
    /// <param name="slice">Current state slice.</param>
    /// <param name="action">Action to apply.</param>
    /// <returns>Task resolving to the new slice state.</returns>
    public Task<TS> ReduceAsync(TS slice, TA action);
}
