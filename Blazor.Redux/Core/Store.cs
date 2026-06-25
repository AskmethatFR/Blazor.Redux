using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Core;

public sealed class Store : IObservableStore, IRootStateStore, IStateSnapshotApplier, IDisposable
{
    private readonly StoreStateManager _stateManager;
    private readonly SnapshotStrategy _snapshotStrategy;

    private Store(SnapshotStrategy snapshotStrategy, ISlice[] slices)
    {
        _snapshotStrategy = snapshotStrategy;
        _stateManager = new StoreStateManager(slices);
        _stateChanges = new BehaviorSubject<RootStateSnapshot>(CreateSnapshot());
    }

    public static Store Init(params ISlice[] slices)
    {
        return new Store(SnapshotStrategy.DeepCopy, slices);
    }

    public static Store Init(SnapshotStrategy snapshotStrategy, params ISlice[] slices)
    {
        return new Store(snapshotStrategy, slices);
    }

    private readonly Subject<(Type SliceType, ISlice Slice)> _sliceChanges = new();
    private readonly BehaviorSubject<RootStateSnapshot> _stateChanges;
    private readonly object _sliceChangesLock = new();

    public TSlice? GetSlice<TSlice>() where TSlice : class, ISlice =>
        _stateManager.GetSlice<TSlice>();

    public TSlice UpdateSlice<TSlice>(TSlice update) where TSlice : class, ISlice
    {
        var (updatedSlice, snapshot) = _stateManager.UpdateSliceAndSnapshot(update, _snapshotStrategy);

        lock (_sliceChangesLock)
        {
            _sliceChanges.OnNext((typeof(TSlice), updatedSlice));
        }
        _stateChanges.OnNext(snapshot);
        return updatedSlice;
    }

    public IObservable<TSlice> ObserveSlice<TSlice>() where TSlice : class, ISlice
    {
        return Observable.Create<TSlice>(observer =>
        {
            var currentSlice = GetSlice<TSlice>();

            IDisposable subscription;
            lock (_sliceChangesLock)
            {
                subscription = _sliceChanges
                    .Where(change => change.SliceType == typeof(TSlice))
                    .Select(change => (TSlice)change.Slice)
                    .Subscribe(observer);
            }

            if (currentSlice != null)
            {
                observer.OnNext(currentSlice);
            }

            return subscription;
        });
    }

    public IObservable<Unit> ObserveAnyChange()
    {
        return _sliceChanges.Select(_ => Unit.Default);
    }

    public IObservable<RootStateSnapshot> ObserveState()
    {
        return _stateChanges.AsObservable();
    }

    public RootStateSnapshot Current => _stateChanges.Value;

    public void ApplySnapshot(RootStateSnapshot snapshot, bool strictValidation = true)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var (updatedSnapshot, sliceUpdates) =
            _stateManager.ValidateAndReplaceAllSlices(snapshot, strictValidation, _snapshotStrategy);

        lock (_sliceChangesLock)
        {
            foreach (var update in sliceUpdates)
            {
                _sliceChanges.OnNext(update);
            }
        }

        _stateChanges.OnNext(updatedSnapshot);
    }

    public void Dispose()
    {
        _sliceChanges?.Dispose();
        _stateChanges?.Dispose();
    }

    private RootStateSnapshot CreateSnapshot()
    {
        return _stateManager.CreateSnapshot(_snapshotStrategy);
    }
}