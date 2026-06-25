using System.Reactive.Linq;
using System.Reactive.Subjects;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Core;

/// <summary>
/// In-memory action stream based on Rx subjects.
/// Can optionally replay the last action on subscription.
/// Implements <see cref="IActionStream"/> for external access
/// and exposes <see cref="Publish"/> for internal dispatch.
/// </summary>
public sealed class ActionStream : IActionStream, IDisposable
{
    private readonly ISubject<IAction> _actions;
    private readonly IDisposable? _actionsDisposable;

    /// <summary>
    /// Initializes the action stream.
    /// </summary>
    /// <param name="replayLastAction">
    /// If true, uses <see cref="ReplaySubject{T}"/> with buffer size 1
    /// so late subscribers receive the last action.
    /// </param>
    public ActionStream(bool replayLastAction)
    {
        ISubject<IAction> subject = replayLastAction
            ? new ReplaySubject<IAction>(1)
            : new Subject<IAction>();
        _actions = Subject.Synchronize(subject);
        _actionsDisposable = _actions as IDisposable;
    }

    /// <summary>
    /// Observable sequence of dispatched actions.
    /// </summary>
    public IObservable<IAction> Actions => _actions.AsObservable();

    /// <summary>
    /// Publishes an action into the stream.
    /// Called internally by dispatchers after middleware and reducers.
    /// </summary>
    internal void Publish(IAction action)
    {
        _actions.OnNext(action);
    }

    public void Dispose()
    {
        _actionsDisposable?.Dispose();
    }
}
