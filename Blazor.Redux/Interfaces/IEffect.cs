using Blazor.Redux.Core;

namespace Blazor.Redux.Interfaces;

/// <summary>
/// Side-effect handler that observes dispatched actions and current state,
/// and returns new actions to be re-dispatched into the pipeline.
/// Effects are subscribed to the action stream and run reactively.
/// </summary>
public interface IEffect
{
    /// <summary>
    /// Handles incoming actions and state, producing outgoing effect actions.
    /// </summary>
    /// <param name="actions">Stream of dispatched actions.</param>
    /// <param name="state">Stream of root state snapshots.</param>
    /// <returns>Stream of effect actions to re-dispatch.</returns>
    IObservable<IEffectAction> Handle(
        IObservable<IAction> actions,
        IObservable<RootStateSnapshot> state);
}
