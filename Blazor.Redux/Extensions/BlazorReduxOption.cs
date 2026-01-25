using System.Reflection;
using System.Reactive.Concurrency;
using Blazor.Redux.Core;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Extensions;

public record BlazorReduxOption
{
    public ISlice[] Slices { get; set; } = [];
    public Assembly? Assembly { get; set; } = null;
    public bool ReplayLastAction { get; set; } = false;
    public IScheduler? EffectsScheduler { get; set; } = null;
    public SnapshotStrategy SnapshotStrategy { get; set; } = SnapshotStrategy.DeepCopy;
    public EffectsCancellationStrategy EffectsCancellationStrategy { get; set; } = EffectsCancellationStrategy.None;
    public IList<Type> Middlewares { get; set; } = new List<Type>();

    public BlazorReduxOption AddMiddleware<TMiddleware>() where TMiddleware : class, IDispatchMiddleware
    {
        Middlewares.Add(typeof(TMiddleware));
        return this;
    }
}
