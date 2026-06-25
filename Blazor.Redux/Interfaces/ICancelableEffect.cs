using Blazor.Redux.Core;

namespace Blazor.Redux.Interfaces;

/// <summary>
/// Marker interface for an <see cref="IEffect"/> that supports cancellation
/// via the Rx Switch pattern. When effects are configured with
/// <see cref="EffectsCancellationStrategy.RxSwitch"/>, the pipeline
/// automatically subscribes using <c>Switch()</c>, which cancels the
/// previous inner subscription when a new action arrives.
/// </summary>
public interface ICancelableEffect : IEffect
{
    /// <summary>
    /// Handles incoming actions and state, returning a stream of streams.
    /// Each inner stream represents a cancellable effect cycle.
    /// The pipeline calls <c>Switch()</c> on the outer stream to cancel
    /// the previous cycle when a new action triggers the next inner stream.
    /// </summary>
    /// <param name="actions">Stream of dispatched actions.</param>
    /// <param name="state">Stream of root state snapshots.</param>
    /// <returns>Stream of streams, each producing effect actions for one cycle.</returns>
    IObservable<IObservable<IEffectAction>> HandleWithCancellation(
        IObservable<IAction> actions,
        IObservable<RootStateSnapshot> state);
}
