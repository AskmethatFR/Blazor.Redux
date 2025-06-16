using System.Reactive;

namespace Blazor.Redux.Interfaces;

public interface IObservableStore
{
    IObservable<TSlice> ObserveSlice<TSlice>() where TSlice : class, ISlice;
    IObservable<Unit> ObserveAnyChange();
}