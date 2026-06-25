using System.Reactive;

namespace Blazor.Redux.Interfaces;

/// <summary>
/// Observable store that enables reactive subscriptions to state changes.
/// Provides streams for individual slices and a general change-notification stream.
/// </summary>
public interface IObservableStore
{
    /// <summary>
    /// Returns an observable that emits the current state of the specified slice
    /// immediately on subscription, then on every subsequent change.
    /// </summary>
    /// <typeparam name="TSlice">Slice type to observe.</typeparam>
    /// <returns>Observable stream of slice states.</returns>
    IObservable<TSlice> ObserveSlice<TSlice>() where TSlice : class, ISlice;

    /// <summary>
    /// Returns an observable that emits a notification on every state change,
    /// regardless of which slice changed.
    /// </summary>
    /// <returns>Observable that emits <see cref="Unit.Default"/> on each change.</returns>
    IObservable<Unit> ObserveAnyChange();
}
