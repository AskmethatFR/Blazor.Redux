using System.Reflection;
using System.Reactive.Concurrency;
using Blazor.Redux.Core;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Extensions;

/// <summary>
/// Configuration options for registering Blazor.Redux with DI.
/// All properties are init-only for immutability after registration.
/// Use the <see cref="AddMiddleware{T}"/> fluent method or collection-initializer
/// syntax to configure middleware types during setup.
/// </summary>
public record BlazorReduxOption
{
    /// <summary>
    /// Initial state slices to register in the store.
    /// </summary>
    public ISlice[] Slices { get; init; } = [];

    /// <summary>
    /// Assembly to scan for reducers and effects (defaults to the calling assembly).
    /// </summary>
    public Assembly? Assembly { get; init; } = null;

    /// <summary>
    /// If true, the action stream replays the last dispatched action for late subscribers.
    /// </summary>
    public bool ReplayLastAction { get; init; } = false;

    /// <summary>
    /// Optional Rx scheduler for effect execution (e.g., to offload to a background thread).
    /// </summary>
    public IScheduler? EffectsScheduler { get; init; } = null;

    /// <summary>
    /// Strategy for creating state snapshots (<see cref="SnapshotStrategy.DeepCopy"/> by default).
    /// </summary>
    public SnapshotStrategy SnapshotStrategy { get; init; } = SnapshotStrategy.DeepCopy;

    /// <summary>
    /// Strategy for cancelling running effect operations.
    /// </summary>
    public EffectsCancellationStrategy EffectsCancellationStrategy { get; init; } = EffectsCancellationStrategy.None;

    /// <summary>
    /// Middleware types to register in the dispatch pipeline.
    /// Mutate this list during builder setup; it becomes immutable after registration.
    /// </summary>
    public IList<Type> Middlewares { get; init; } = new List<Type>();

    /// <summary>
    /// Adds a middleware type to the pipeline.
    /// </summary>
    /// <typeparam name="TMiddleware">Middleware type implementing <see cref="IDispatchMiddleware"/>.</typeparam>
    /// <returns>This instance for chaining.</returns>
    public BlazorReduxOption AddMiddleware<TMiddleware>() where TMiddleware : class, IDispatchMiddleware
    {
        Middlewares.Add(typeof(TMiddleware));
        return this;
    }
}
