using Blazor.Redux.Core;

namespace Blazor.Redux.Interfaces;

/// <summary>
/// Provides access to the complete root state as observable snapshots
/// and the latest snapshot synchronously.
/// </summary>
public interface IRootStateStore
{
    /// <summary>
    /// Returns an observable that emits the complete root state snapshot
    /// immediately on subscription, then on every state change.
    /// </summary>
    /// <returns>Observable stream of root state snapshots.</returns>
    IObservable<RootStateSnapshot> ObserveState();

    /// <summary>
    /// Gets the current root state snapshot synchronously.
    /// </summary>
    RootStateSnapshot Current { get; }
}
