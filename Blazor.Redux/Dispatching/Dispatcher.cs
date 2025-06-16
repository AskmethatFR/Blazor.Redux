using Blazor.Redux.Core;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Dispatching;

public class Dispatcher : IDispatcher
{
    private readonly Store _store;
    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(Store store, IServiceProvider serviceProvider)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public void Dispatch<TSlice, TAction>(TAction action)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        ArgumentNullException.ThrowIfNull(action);

        var reducer =
            _serviceProvider.GetService(typeof(IReducer<TSlice, TAction>));

        if (reducer is IReducer<TSlice, TAction> typedReducer)
        {
            ApplyReducer(typedReducer, action);
        }
    }

    private void ApplyReducer<TSlice, TAction>(IReducer<TSlice, TAction> reducer, TAction action)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        var currentSlice = _store.GetSlice<TSlice>();

        if(currentSlice is null) throw new ArgumentNullException("currentSlice");

        var newSlice = reducer.Reduce(currentSlice, action);
        _store.UpdateSlice(newSlice);
    }
}