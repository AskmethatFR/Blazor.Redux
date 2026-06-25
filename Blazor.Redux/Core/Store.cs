using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Core;

/// <summary>
/// Central Redux store holding all application state.
/// Implements observable patterns for reactive subscriptions,
/// snapshot creation for serialization, and state application for hydration.
/// Thread-safe via <see cref="StoreStateManager"/>.
/// </summary>
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

    /// <summary>
    /// Creates a store with the given slices using <see cref="SnapshotStrategy.DeepCopy"/>.
    /// </summary>
    /// <param name="slices">Initial state slices.</param>
    /// <returns>Initialized store instance.</returns>
    public static Store Init(params ISlice[] slices)
    {
        return new Store(SnapshotStrategy.DeepCopy, slices);
    }

    /// <summary>
    /// Creates a store with the given slices and snapshot strategy.
    /// </summary>
    /// <param name="snapshotStrategy">Strategy for creating state snapshots.</param>
    /// <param name="slices">Initial state slices.</param>
    /// <returns>Initialized store instance.</returns>
    public static Store Init(SnapshotStrategy snapshotStrategy, params ISlice[] slices)
    {
        return new Store(snapshotStrategy, slices);
    }

    private readonly Subject<(Type SliceType, ISlice Slice)> _sliceChanges = new();
    private readonly BehaviorSubject<RootStateSnapshot> _stateChanges;
    private readonly object _sliceChangesLock = new();

    /// <summary>
    /// Gets the current state of a specific slice.
    /// Returns null if the slice type is not registered.
    /// </summary>
    public TSlice? GetSlice<TSlice>() where TSlice : class, ISlice =>
        _stateManager.GetSlice<TSlice>();

    /// <summary>
    /// Updates a slice in the store, notifying all observers.
    /// Returns a deep copy (or reference, depending on strategy) of the updated slice.
    /// </summary>
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

    /// <summary>
    /// Returns an observable that emits the current slice state on subscription,
    /// then on every subsequent change.
    /// </summary>
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

    /// <summary>
    /// Returns an observable that emits <see cref="Unit.Default"/> on every state change.
    /// </summary>
    public IObservable<Unit> ObserveAnyChange()
    {
        return _sliceChanges.Select(_ => Unit.Default);
    }

    /// <summary>
    /// Returns an observable that emits the complete root state snapshot on subscription,
    /// then on every state change.
    /// </summary>
    public IObservable<RootStateSnapshot> ObserveState()
    {
        return _stateChanges.AsObservable();
    }

    /// <summary>
    /// Gets the current root state snapshot synchronously.
    /// </summary>
    public RootStateSnapshot Current => _stateChanges.Value;

    /// <summary>
    /// Applies a saved snapshot, replacing all slice state atomically.
    /// See <see cref="IStateSnapshotApplier.ApplySnapshot"/> for validation rules.
    /// </summary>
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
