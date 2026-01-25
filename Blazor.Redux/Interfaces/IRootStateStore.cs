using Blazor.Redux.Core;

namespace Blazor.Redux.Interfaces;

public interface IRootStateStore
{
    IObservable<RootStateSnapshot> ObserveState();
    RootStateSnapshot Current { get; }
}
