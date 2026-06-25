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
    private readonly ReducerPipelineRunner _reducerPipelineRunner;
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
        _middlewares = (middlewares ?? Array.Empty<IDispatchMiddleware>()).ToList();
        _reducerPipelineRunner = new ReducerPipelineRunner(_store, eventPublisher);
    }

    public async Task DispatchAsync<TSlice, TAction>(TAction action, CancellationToken cancellationToken = default)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();

        _effectsPipeline.EnsureStarted();

        await _dispatchQueue.ExecuteAsync(() =>
            ExecutePipelineAsync<TSlice, TAction>(action, cancellationToken)).ConfigureAwait(false);
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

            var syncReducers = _reducerRegistry.GetReducers<TSlice, TAction>();
            _reducerPipelineRunner.ApplyReducers(syncReducers, action);

            var asyncReducers = _reducerRegistry.GetAsyncReducers<TSlice, TAction>();
            await _reducerPipelineRunner.ApplyAsyncReducers(asyncReducers, action);
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