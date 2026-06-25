using Blazor.Redux.Core;
using Blazor.Redux.Core.Events;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Dispatching;

/// <summary>
/// Shared reducer pipeline logic used by both Dispatcher and AsyncDispatcher.
///
/// Encapsulates:
/// 1. Sync reducer aggregation (via Aggregate)
/// 2. Async reducer sequential application (via foreach + await)
/// 3. Store event publication
/// 4. Store slice update
///
/// DispatchQueue provides serial dispatch ordering.
/// Subject.Synchronize in ActionStream provides thread-safe observer notification.
/// These are separate concerns: DispatchQueue prevents concurrent dispatch
/// re-entrance; Subject.Synchronize prevents concurrent OnNext calls to
/// reactive subscribers. Both are needed.
/// </summary>
internal sealed class ReducerPipelineRunner
{
    private readonly Store _store;
    private readonly IStoreEventPublisher? _eventPublisher;

    public ReducerPipelineRunner(Store store, IStoreEventPublisher? eventPublisher = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _eventPublisher = eventPublisher;
    }

    public void ApplyReducers<TSlice, TAction>(IEnumerable<IReducer<TSlice, TAction>> reducers, TAction action)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        var reducerList = reducers as IList<IReducer<TSlice, TAction>> ?? reducers.ToList();
        if (reducerList.Count == 0)
        {
            return;
        }

        var currentSlice = _store.GetSlice<TSlice>();
        if (currentSlice is null)
        {
            throw new InvalidOperationException($"Slice '{typeof(TSlice).Name}' is not registered in the store.");
        }

        var newSlice = reducerList.Aggregate(currentSlice, (slice, reducer) => reducer.Reduce(slice, action));

        PublishEvent(action, newSlice, currentSlice);
        _store.UpdateSlice(newSlice);
    }

    public async Task ApplyAsyncReducers<TSlice, TAction>(
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
        {
            throw new InvalidOperationException($"Slice '{typeof(TSlice).Name}' is not registered in the store.");
        }

        var newSlice = currentSlice;
        foreach (var reducer in reducerList)
        {
            newSlice = await reducer.ReduceAsync(newSlice, action).ConfigureAwait(false);
        }

        PublishEvent(action, newSlice, currentSlice);
        _store.UpdateSlice(newSlice);
    }

    private void PublishEvent<TSlice, TAction>(TAction action, TSlice newSlice, TSlice previousSlice)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        if (_eventPublisher != null)
        {
            _eventPublisher.PublishEvent(new StoreEvent(
                EventType: typeof(TAction).Name,
                SliceType: typeof(TSlice).Name,
                NewState: newSlice,
                PreviousState: previousSlice,
                Action: action
            ));
        }
    }
}
