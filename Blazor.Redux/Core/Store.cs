using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Linq;
using Blazor.Redux.Core.Events;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Core;

public sealed class Store : IObservableStore, IRootStateStore, IStateSnapshotApplier, IDisposable
{
    private readonly SnapshotStrategy _snapshotStrategy;

    private Store(SnapshotStrategy snapshotStrategy, ISlice[] slices)
    {
        _snapshotStrategy = snapshotStrategy;
        foreach (var slice in slices)
        {
            _state = _state.AddSlice(slice);
        }

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

    private AppState _state = AppState.InitialState();
    private readonly Subject<(Type SliceType, ISlice Slice)> _sliceChanges = new();
    private readonly BehaviorSubject<RootStateSnapshot> _stateChanges;
    private readonly object _stateLock = new();
    private readonly object _sliceChangesLock = new();

    public TSlice? GetSlice<TSlice>() where TSlice : class, ISlice =>
        GetSliceInternal<TSlice>();

    private TSlice? GetSliceInternal<TSlice>() where TSlice : class, ISlice
    {
        lock (_stateLock)
        {
            return _state.GetSlice<TSlice>();
        }
    }

    public TSlice UpdateSlice<TSlice>(TSlice update) where TSlice : class, ISlice
    {
        RootStateSnapshot snapshot;
        TSlice updatedSlice;

        lock (_stateLock)
        {
            updatedSlice = _state.UpdateSlice(update);
            snapshot = CreateSnapshot();
        }

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

                if (currentSlice != null)
                {
                    observer.OnNext(currentSlice);
                }
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

        var currentSnapshot = _state.GetAllSlicesSnapshot(SnapshotStrategy.Reference);
        if (strictValidation)
        {
            var currentKeys = new HashSet<Type>(currentSnapshot.Keys);
            var incomingKeys = new HashSet<Type>(snapshot.Slices.Keys);
            if (!currentKeys.SetEquals(incomingKeys))
            {
                var missing = string.Join(", ", currentKeys.Except(incomingKeys).Select(t => t.Name));
                var extra = string.Join(", ", incomingKeys.Except(currentKeys).Select(t => t.Name));
                throw new InvalidOperationException(
                    $"Snapshot slice set mismatch. Missing: [{missing}] Extra: [{extra}].");
            }
        }
        else
        {
            foreach (var type in snapshot.Slices.Keys)
            {
                if (!currentSnapshot.ContainsKey(type))
                {
                    throw new InvalidOperationException($"Snapshot contains unknown slice type '{type.Name}'.");
                }
            }
        }

        RootStateSnapshot updatedSnapshot;
        IReadOnlyList<(Type SliceType, ISlice Slice)> sliceUpdates;

        lock (_stateLock)
        {
            var newState = AppState.InitialState();
            foreach (var slice in snapshot.Slices.Values)
            {
                newState = newState.AddSlice(slice);
            }

            _state = newState;
            updatedSnapshot = CreateSnapshot();

            sliceUpdates = snapshot.Slices
                .Select(entry => (entry.Key, entry.Value))
                .ToList();
        }

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
        return new RootStateSnapshot(_state.GetAllSlicesSnapshot(_snapshotStrategy));
    }
}
