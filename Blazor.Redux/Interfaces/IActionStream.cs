namespace Blazor.Redux.Interfaces;

/// <summary>
/// Observable stream of all dispatched actions.
/// Effects subscribe to this stream to react to actions as they occur.
/// The stream can optionally replay the last action for late subscribers.
/// </summary>
public interface IActionStream
{
    /// <summary>
    /// Gets the observable sequence of dispatched actions.
    /// </summary>
    IObservable<IAction> Actions { get; }
}
