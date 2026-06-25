using Blazor.Redux.Core;
using Blazor.Redux.Core.Events;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Dispatching;

public class AsyncDispatcher : IAsyncDispatcher
{
    private readonly Store _store;
    private readonly IReducerRegistry _reducerRegistry;
    private readonly DispatchQueue _dispatchQueue;
    private readonly ActionStream _actionStream;
    private readonly EffectsPipeline _effectsPipeline;
    private readonly IStoreEventPublisher? _eventPublisher;
    private readonly IReadOnlyList<IDispatchMiddleware> _middlewares;

    public AsyncDispatcher(
        Store store,
        IReducerRegistry reducerRegistry,
        DispatchQueue dispatchQueue,
        ActionStream actionStream,
        EffectsPipeline effectsPipeline,
        IStoreEventPublisher? eventPublisher,
        IEnumerable<IDispatchMiddleware> middlewares)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _reducerRegistry = reducerRegistry ?? throw new ArgumentNullException(nameof(reducerRegistry));
        _dispatchQueue = dispatchQueue ?? throw new ArgumentNullException(nameof(dispatchQueue));
        _actionStream = actionStream ?? throw new ArgumentNullException(nameof(actionStream));
        _effectsPipeline = effectsPipeline ?? throw new ArgumentNullException(nameof(effectsPipeline));
        _eventPublisher = eventPublisher;
        _middlewares = (middlewares ?? Array.Empty<IDispatchMiddleware>()).ToList();

        _effectsPipeline.EnsureStarted();
    }

    public async Task DispatchAsync<TSlice, TAction>(TAction action, CancellationToken cancellationToken = default)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();

        await _dispatchQueue.ExecuteAsync(() =>
            ExecutePipelineAsync<TSlice, TAction>(action, cancellationToken)).ConfigureAwait(false);
    }

    private void ApplySyncReducerIfExists<TSlice, TAction>(TAction action)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        var reducers = _reducerRegistry.GetReducers<TSlice, TAction>();
        ApplyReducers(reducers, action);
    }

    private void ApplyReducers<TSlice, TAction>(IEnumerable<IReducer<TSlice, TAction>> reducers, TAction action)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        var reducerList = reducers as IList<IReducer<TSlice, TAction>> ?? reducers.ToList();
        if (reducerList.Count == 0)
        {
            return;
        }

        var currentSlice = _store.GetSlice<TSlice>();

        if (currentSlice is null) throw new InvalidOperationException($"Slice '{typeof(TSlice).Name}' is not registered in the store.");

        var newSlice = reducerList.Aggregate(currentSlice, (slice, reducer) => reducer.Reduce(slice, action));

        if (_eventPublisher != null)
        {
            _eventPublisher.PublishEvent(new StoreEvent(
                EventType: typeof(TAction).Name,
                SliceType: typeof(TSlice).Name,
                NewState: newSlice,
                PreviousState: currentSlice,
                Action: action
            ));
        }

        _store.UpdateSlice(newSlice);
    }

    private async Task ApplyAsyncReducers<TSlice, TAction>(
        IEnumerable<IAsyncReducer<TSlice, TAction>> reducers,
        TAction action)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        var reducerList = reducers as IList<IAsyncReducer<TSlice, TAction>> ?? reducers.ToList();
        if (reducerList.Count == 0)
        {
            return;
        }

        var currentSlice = _store.GetSlice<TSlice>();
        if (currentSlice is null)
            throw new ArgumentNullException(nameof(currentSlice));

        var newSlice = currentSlice;
        foreach (var reducer in reducerList)
        {
            newSlice = await reducer.ReduceAsync(newSlice, action);
        }

        if (_eventPublisher != null)
        {
            _eventPublisher.PublishEvent(new StoreEvent(
                EventType: typeof(TAction).Name,
                SliceType: typeof(TSlice).Name,
                NewState: newSlice,
                PreviousState: currentSlice,
                Action: action
            ));
        }
        _store.UpdateSlice(newSlice);
    }

    private Task ExecutePipelineAsync<TSlice, TAction>(TAction action, CancellationToken cancellationToken)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        var middlewares = _middlewares;
        Func<Task> terminal = async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            _actionStream.Publish(action);
            ApplySyncReducerIfExists<TSlice, TAction>(action);
            var asyncReducers = _reducerRegistry.GetAsyncReducers<TSlice, TAction>();
            await ApplyAsyncReducers(asyncReducers, action);
        };

        if (middlewares.Count == 0)
        {
            return terminal();
        }

        Func<Task> pipeline = terminal;
        for (var i = middlewares.Count - 1; i >= 0; i--)
        {
            var current = middlewares[i];
            var next = pipeline;
            pipeline = () => current.InvokeAsync<TSlice, TAction>(action, next);
        }

        return pipeline();
    }
}
