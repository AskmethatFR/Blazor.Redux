using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Core;

public record Store : IObservableStore, IDisposable
{
    private Store(ISlice[] slices)
    {
        foreach (var slice in slices)
        {
            _state = _state.AddSlice(slice);
        }
    }

    public static Store Init(params ISlice[] slices)
    {
        return new Store(slices);
    }

    private AppState _state = AppState.InitialState();
    private readonly Subject<(Type SliceType, ISlice Slice)> _sliceChanges = new();

    public TSlice? GetSlice<TSlice>() where TSlice : class, ISlice =>
        _state.GetSlice<TSlice>();

    public TSlice UpdateSlice<TSlice>(TSlice update) where TSlice : class, ISlice
    {
        var updatedSlice = _state.UpdateSlice(update);
        
        _sliceChanges.OnNext((typeof(TSlice), updatedSlice));
        
        return updatedSlice;
    }

    public IObservable<TSlice> ObserveSlice<TSlice>() where TSlice : class, ISlice
    {
        var currentSlice = GetSlice<TSlice>();
        
        var observable = _sliceChanges
            .Where(change => change.SliceType == typeof(TSlice))
            .Select(change => (TSlice)change.Slice);

        // StartWith seulement si le slice existe déjà
        return currentSlice != null 
            ? observable.StartWith(currentSlice)
            : observable;
    }

    public IObservable<Unit> ObserveAnyChange()
    {
        return _sliceChanges.Select(_ => Unit.Default);
    }

    public void Dispose()
    {
        _sliceChanges?.Dispose();
    }
}