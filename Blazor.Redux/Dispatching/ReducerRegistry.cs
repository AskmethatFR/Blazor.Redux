using Blazor.Redux.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Blazor.Redux.Dispatching;

internal sealed class ReducerRegistry : IReducerRegistry
{
    private readonly IServiceProvider _serviceProvider;

    public ReducerRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public IEnumerable<IReducer<TSlice, TAction>> GetReducers<TSlice, TAction>()
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        return _serviceProvider.GetServices<IReducer<TSlice, TAction>>();
    }

    public IEnumerable<IAsyncReducer<TSlice, TAction>> GetAsyncReducers<TSlice, TAction>()
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        return _serviceProvider.GetServices<IAsyncReducer<TSlice, TAction>>();
    }
}
