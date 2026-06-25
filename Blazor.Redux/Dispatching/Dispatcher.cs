using System.Runtime.ExceptionServices;
using Blazor.Redux.Core;
using Blazor.Redux.Core.Events;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Dispatching;

public class Dispatcher : IDispatcher
{
    private readonly Store _store;
    private readonly IReducerRegistry _reducerRegistry;
    private readonly DispatchQueue _dispatchQueue;
    private readonly ActionStream _actionStream;
    private readonly EffectsPipeline _effectsPipeline;
    private readonly ReducerPipelineRunner _reducerPipelineRunner;

    private readonly IReadOnlyList<IDispatchMiddleware> _middlewares;

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
    }

    public void Dispatch<TSlice, TAction>(TAction action)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        ArgumentNullException.ThrowIfNull(action);

        _effectsPipeline.EnsureStarted();

        _dispatchQueue.Execute(() => ExecutePipeline<TSlice, TAction>(action));
    }

    private void ExecutePipeline<TSlice, TAction>(TAction action)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        var middlewares = _middlewares;

        if (middlewares.Count == 0)
        {
            Terminal<TSlice, TAction>(action);
            return;
        }

        ExecuteMiddlewareChain<TSlice, TAction>(action, middlewares, 0);
    }

    private void ExecuteMiddlewareChain<TSlice, TAction>(
        TAction action,
        IReadOnlyList<IDispatchMiddleware> middlewares,
        int index)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        if (index >= middlewares.Count)
        {
            Terminal<TSlice, TAction>(action);
            return;
        }

        var current = middlewares[index];
        var task = current.InvokeAsync<TSlice, TAction>(action, () =>
        {
            ExecuteMiddlewareChain<TSlice, TAction>(action, middlewares, index + 1);
            return Task.CompletedTask;
        });

        EnsureMiddlewareCompletedSynchronously(task, current);
    }

    private void Terminal<TSlice, TAction>(TAction action)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        _actionStream.Publish(action);
        var reducers = _reducerRegistry.GetReducers<TSlice, TAction>();
        _reducerPipelineRunner.ApplyReducers(reducers, action);
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