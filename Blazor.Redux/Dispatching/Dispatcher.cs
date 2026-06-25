using System.Runtime.ExceptionServices;
using Blazor.Redux.Core;
using Blazor.Redux.Core.Events;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Dispatching;

/// <summary>
/// Synchronous dispatcher that sends actions through the Redux pipeline.
/// Actions are serialized through a <see cref="DispatchQueue"/>, run through
/// registered middlewares, published to the action stream, and then applied
/// to synchronous reducers. Middleware tasks must complete synchronously.
/// </summary>
public class Dispatcher : IDispatcher
{
    private readonly Store _store;
    private readonly IReducerRegistry _reducerRegistry;
    private readonly DispatchQueue _dispatchQueue;
    private readonly ActionStream _actionStream;
    private readonly EffectsPipeline _effectsPipeline;
    private readonly ReducerPipelineRunner _reducerPipelineRunner;

    private readonly IReadOnlyList<IDispatchMiddleware> _middlewares;

    /// <summary>
    /// Initializes the synchronous dispatcher.
    /// </summary>
    public Dispatcher(
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
        _middlewares = (middlewares ?? Array.Empty<IDispatchMiddleware>()).ToList();
        _reducerPipelineRunner = new ReducerPipelineRunner(_store, eventPublisher);

        _effectsPipeline.EnsureStarted();
    }

    /// <summary>
    /// Dispatches an action synchronously for the given slice type.
    /// </summary>
    public void Dispatch<TSlice, TAction>(TAction action)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        ArgumentNullException.ThrowIfNull(action);

        _dispatchQueue.Execute(() => ExecutePipeline<TSlice, TAction>(action));
    }

    private void ExecutePipeline<TSlice, TAction>(TAction action)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        var middlewares = _middlewares;
        void Terminal()
        {
            _actionStream.Publish(action);
            var reducers = _reducerRegistry.GetReducers<TSlice, TAction>();
            _reducerPipelineRunner.ApplyReducers(reducers, action);
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
