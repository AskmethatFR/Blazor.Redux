using System.Linq;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Core;

internal sealed class StoreStateManager
{
    private AppState _state = AppState.InitialState();
    private readonly object _stateLock = new();

    public StoreStateManager(params ISlice[] slices)
    {
        foreach (var slice in slices)
        {
            _state = _state.AddSlice(slice);
        }
    }

    public T? GetSlice<T>() where T : class, ISlice
    {
        lock (_stateLock)
        {
            return _state.GetSlice<T>();
        }
    }

    public T UpdateSlice<T>(T value) where T : class, ISlice
    {
        lock (_stateLock)
        {
            return _state.UpdateSlice(value);
        }
    }

    public (T Updated, RootStateSnapshot Snapshot) UpdateSliceAndSnapshot<T>(T value, SnapshotStrategy strategy)
        where T : class, ISlice
    {
        lock (_stateLock)
        {
            var updated = _state.UpdateSlice(value);
            var snapshot = new RootStateSnapshot(_state.GetAllSlicesSnapshot(strategy));
            return (updated, snapshot);
        }
    }

    public RootStateSnapshot CreateSnapshot(SnapshotStrategy strategy)
    {
        lock (_stateLock)
        {
            return new RootStateSnapshot(_state.GetAllSlicesSnapshot(strategy));
        }
    }

    public (RootStateSnapshot Snapshot, IReadOnlyList<(Type SliceType, ISlice Slice)> Updates) ReplaceAllSlices(
        IReadOnlyDictionary<Type, ISlice> slices,
        SnapshotStrategy strategy)
    {
        lock (_stateLock)
        {
            var newState = AppState.InitialState();
            foreach (var slice in slices.Values)
            {
                newState = newState.AddSlice(slice);
            }

            _state = newState;

            var snapshot = new RootStateSnapshot(_state.GetAllSlicesSnapshot(strategy));
            var updates = slices
                .Select(entry => (entry.Key, entry.Value))
                .ToList();

            return (snapshot, updates);
        }
    }

    public (RootStateSnapshot Snapshot, IReadOnlyList<(Type SliceType, ISlice Slice)> Updates)
        ValidateAndReplaceAllSlices(RootStateSnapshot incoming, bool strictValidation, SnapshotStrategy strategy)
    {
        lock (_stateLock)
        {
            var currentSlices = _state.GetAllSlicesSnapshot(SnapshotStrategy.Reference);
            if (strictValidation)
            {
                var currentKeys = new HashSet<Type>(currentSlices.Keys);
                var incomingKeys = new HashSet<Type>(incoming.Slices.Keys);
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
                foreach (var type in incoming.Slices.Keys)
                {
                    if (!currentSlices.ContainsKey(type))
                    {
                        throw new InvalidOperationException(
                            $"Snapshot contains unknown slice type '{type.Name}'.");
                    }
                }
            }

            var newState = AppState.InitialState();
            foreach (var slice in incoming.Slices.Values)
            {
                newState = newState.AddSlice(slice);
            }

            _state = newState;

            var snapshot = new RootStateSnapshot(_state.GetAllSlicesSnapshot(strategy));
            var updates = incoming.Slices
                .Select(entry => (entry.Key, entry.Value))
                .ToList();

            return (snapshot, updates);
        }
    }

    public IReadOnlyDictionary<Type, ISlice> GetAllSlices(SnapshotStrategy strategy)
    {
        lock (_stateLock)
        {
            return _state.GetAllSlicesSnapshot(strategy);
        }
    }
}
