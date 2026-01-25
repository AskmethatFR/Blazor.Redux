using System.Linq;
using Blazor.Redux.Core;
using Blazor.Redux.Core.Events;
using Blazor.Redux.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Blazor.Redux.Dispatching;

public class Dispatcher : IDispatcher
{
    private readonly Store _store;
    private readonly IServiceProvider _serviceProvider;
    private readonly DispatchQueue _dispatchQueue;
    private readonly ActionStream _actionStream;
    private readonly EffectsPipeline _effectsPipeline;

    private readonly IStoreEventPublisher? _eventPublisher;
    private readonly IReadOnlyList<IDispatchMiddleware> _middlewares;

    public Dispatcher(
        Store store,
        IServiceProvider serviceProvider,
        DispatchQueue dispatchQueue,
        ActionStream actionStream,
        EffectsPipeline effectsPipeline,
        IStoreEventPublisher? eventPublisher,
        IEnumerable<IDispatchMiddleware> middlewares)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _dispatchQueue = dispatchQueue ?? throw new ArgumentNullException(nameof(dispatchQueue));
        _actionStream = actionStream ?? throw new ArgumentNullException(nameof(actionStream));
        _effectsPipeline = effectsPipeline ?? throw new ArgumentNullException(nameof(effectsPipeline));
        _eventPublisher = eventPublisher;
        _middlewares = (middlewares ?? Array.Empty<IDispatchMiddleware>()).ToList();

        _effectsPipeline.EnsureStarted();
    }

    public void Dispatch<TSlice, TAction>(TAction action)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        ArgumentNullException.ThrowIfNull(action);

        // Run synchronously but keep the pipeline async-aware to avoid blocking the semaphore thread.
        // If a middleware is truly async, this can still block; prefer AsyncDispatcher for I/O-bound work.
        _dispatchQueue.ExecuteAsync(() => ExecutePipeline<TSlice, TAction>(action, isSyncDispatcher: true))
            .GetAwaiter()
            .GetResult();
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

    private Task ExecutePipeline<TSlice, TAction>(TAction action, bool isSyncDispatcher)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        var middlewares = _middlewares;
        if (isSyncDispatcher)
        {
            // Guard: detect async middlewares to avoid deadlocks when called from sync dispatcher.
            foreach (var mw in middlewares)
            {
                var invokeMethod = mw.GetType().GetMethod(nameof(IDispatchMiddleware.InvokeAsync));
                if (invokeMethod != null && typeof(Task).IsAssignableFrom(invokeMethod.ReturnType))
                {
                    // We cannot reliably detect async-state-machine here, but we can flag presence.
                    // Users should prefer AsyncDispatcher for I/O bound middlewares.
                    // No throw to remain backward compatible; logging should be done by middleware itself.
                    break;
                }
            }
        }

        Func<Task> terminal = () =>
        {
            _actionStream.Publish(action);
            var reducers = _serviceProvider.GetServices<IReducer<TSlice, TAction>>();
            ApplyReducers(reducers, action);
            return Task.CompletedTask;
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
