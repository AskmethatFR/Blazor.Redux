using System.Linq;
using System.Runtime.ExceptionServices;
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

        _dispatchQueue.Execute(() => ExecutePipeline<TSlice, TAction>(action));
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

    private void ExecutePipeline<TSlice, TAction>(TAction action)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        var middlewares = _middlewares;
        void Terminal()
        {
            _actionStream.Publish(action);
            var reducers = _serviceProvider.GetServices<IReducer<TSlice, TAction>>();
            ApplyReducers(reducers, action);
        }

        if (middlewares.Count == 0)
        {
            Terminal();
            return;
        }

        var index = -1;
        void Next()
        {
            index++;
            if (index == middlewares.Count)
            {
                Terminal();
                return;
            }

            var current = middlewares[index];
            var task = current.InvokeAsync<TSlice, TAction>(action, () =>
            {
                Next();
                return Task.CompletedTask;
            });

            EnsureMiddlewareCompletedSynchronously(task, current);
        }

        Next();
    }

    private static void EnsureMiddlewareCompletedSynchronously(Task task, IDispatchMiddleware middleware)
    {
        if (task.IsCompletedSuccessfully)
        {
            return;
        }

        if (task.IsFaulted && task.Exception?.InnerException is { } innerException)
        {
            ExceptionDispatchInfo.Capture(innerException).Throw();
        }

        if (task.IsCanceled)
        {
            throw new TaskCanceledException(task);
        }

        throw new InvalidOperationException(
            $"Middleware '{middleware.GetType().Name}' returned an incomplete task from sync dispatcher. " +
            "Use IAsyncDispatcher for asynchronous middleware.");
    }
}
