using Blazor.Redux.Core;

namespace Blazor.Redux.Interfaces;

public interface IEffect
{
    IObservable<IEffectAction> Handle(
        IObservable<IAction> actions,
        IObservable<RootStateSnapshot> state);
}
