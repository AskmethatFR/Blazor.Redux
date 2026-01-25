namespace Blazor.Redux.Interfaces;

public interface IActionStream
{
    IObservable<IAction> Actions { get; }
}
