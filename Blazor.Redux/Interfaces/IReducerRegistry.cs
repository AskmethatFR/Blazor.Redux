namespace Blazor.Redux.Interfaces;

/// <summary>
/// Registry that resolves all <see cref="IReducer{TS,TA}"/> and
/// <see cref="IAsyncReducer{TS,TA}"/> implementations for a given
/// slice/action pair. Implementations are resolved from the DI container.
/// </summary>
public interface IReducerRegistry
{
    /// <summary>
    /// Gets all synchronous reducers for the given slice and action types.
    /// </summary>
    /// <typeparam name="TSlice">Slice type.</typeparam>
    /// <typeparam name="TAction">Action type.</typeparam>
    /// <returns>Collection of matching synchronous reducers.</returns>
    IEnumerable<IReducer<TSlice, TAction>> GetReducers<TSlice, TAction>()
        where TSlice : class, ISlice
        where TAction : class, IAction;

    /// <summary>
    /// Gets all asynchronous reducers for the given slice and action types.
    /// </summary>
    /// <typeparam name="TSlice">Slice type.</typeparam>
    /// <typeparam name="TAction">Action type.</typeparam>
    /// <returns>Collection of matching asynchronous reducers.</returns>
    IEnumerable<IAsyncReducer<TSlice, TAction>> GetAsyncReducers<TSlice, TAction>()
        where TSlice : class, ISlice
        where TAction : class, IAction;
}
