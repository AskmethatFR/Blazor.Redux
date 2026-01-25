using System.Reactive.Linq;
using System.Reactive.Subjects;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Core;

public sealed class ActionStream : IActionStream, IDisposable
{
    private readonly ISubject<IAction> _actions;
    private readonly IDisposable? _actionsDisposable;

    public ActionStream(bool replayLastAction)
    {
        ISubject<IAction> subject = replayLastAction
            ? new ReplaySubject<IAction>(1)
            : new Subject<IAction>();
        _actions = Subject.Synchronize(subject);
        _actionsDisposable = _actions as IDisposable;
    }

    public IObservable<IAction> Actions => _actions.AsObservable();

    internal void Publish(IAction action)
    {
        _actions.OnNext(action);
    }

    public void Dispose()
    {
        _actionsDisposable?.Dispose();
    }
}
