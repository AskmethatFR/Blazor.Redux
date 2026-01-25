using Blazor.Redux.Core;

namespace Blazor.Redux.Interfaces;

public interface ICancelableEffect : IEffect
{
    IObservable<IObservable<IEffectAction>> HandleWithCancellation(
        IObservable<IAction> actions,
        IObservable<RootStateSnapshot> state);
}
