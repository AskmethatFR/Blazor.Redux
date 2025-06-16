using Blazor.Redux.Core;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Dispatching;

public class AsyncDispatcher : IAsyncDispatcher
{
    private readonly Store _store;
    private readonly IDispatcher _dispatcher;
    private readonly IServiceProvider _serviceProvider;

    public AsyncDispatcher(Store store, IDispatcher dispatcher, IServiceProvider serviceProvider)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _dispatcher = dispatcher;
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task DispatchAsync<TSlice, TAction>(TAction action, CancellationToken cancellationToken = default)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();

        //appel le reducer synchrone avant
        _dispatcher.Dispatch<TSlice, TAction>(action);
        // Récupérer le reducer async depuis le DI (même pattern que Dispatcher synchrone)
        var asyncReducer = _serviceProvider.GetService(typeof(IAsyncReducer<TSlice, TAction>));

        if (asyncReducer is IAsyncReducer<TSlice, TAction> typedAsyncReducer)
        {
            await ApplyAsyncReducer(typedAsyncReducer, action);
        }
    }

    private async Task ApplyAsyncReducer<TSlice, TAction>(IAsyncReducer<TSlice, TAction> reducer, TAction action)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        var currentSlice = _store.GetSlice<TSlice>();
        if (currentSlice is null)
            throw new ArgumentNullException(nameof(currentSlice));

        // Le reducer fait le travail, pas le dispatcher !
        var newSlice = await reducer.ReduceAsync(currentSlice, action);

        // Le dispatcher se contente de mettre à jour le store
        _store.UpdateSlice(newSlice);
    }
}