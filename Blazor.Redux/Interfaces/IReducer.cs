namespace Blazor.Redux.Interfaces;

/// <summary>
/// Synchronous reducer that produces new state from current state and an action.
/// </summary>
/// <typeparam name="TS">The slice type this reducer operates on.</typeparam>
/// <typeparam name="TA">The action type this reducer handles.</typeparam>
public interface IReducer<TS, TA>
    where TS : class, ISlice
    where TA : class, IAction
{
    /// <summary>
    /// Reduces the given slice and action into a new slice state.
    /// Must be a pure function: no side effects, same input always produces same output.
    /// </summary>
    /// <param name="slice">Current state slice.</param>
    /// <param name="action">Action to apply.</param>
    /// <returns>New slice state after applying the action.</returns>
    public TS Reduce(TS slice, TA action);
}
