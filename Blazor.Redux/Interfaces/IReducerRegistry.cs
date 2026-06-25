namespace Blazor.Redux.Interfaces;

public interface IReducerRegistry
{
    IEnumerable<IReducer<TSlice, TAction>> GetReducers<TSlice, TAction>()
        where TSlice : class, ISlice
        where TAction : class, IAction;

    IEnumerable<IAsyncReducer<TSlice, TAction>> GetAsyncReducers<TSlice, TAction>()
        where TSlice : class, ISlice
        where TAction : class, IAction;
}
