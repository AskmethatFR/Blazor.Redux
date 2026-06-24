using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Core;

internal record AppState
{
    private readonly IDictionary<Type, ISlice> _slices = new Dictionary<Type, ISlice>();
    private readonly ConcurrentDictionary<Type, bool> _isRecordCache = new();

    public T? GetSlice<T>() where T : class, ISlice
    {
        if (!_slices.TryGetValue(typeof(T), out ISlice first))
        {
            return null;
        }

        return DeepCopy((first as T)!);
    }

    public AppState AddSlice(ISlice slice)
    {
        ArgumentNullException.ThrowIfNull(slice);

        var sliceType = slice.GetType();
        _slices.Add(sliceType, DeepCopySlice(slice, sliceType));
        return this;
    }

    public T UpdateSlice<T>(T value) where T : class, ISlice
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!_slices.ContainsKey(typeof(T)))
        {
            throw new InvalidOperationException($"Slice type '{typeof(T).Name}' is not registered in the store.");
        }

        var deepCopy = DeepCopy(value);
        _slices[typeof(T)] = deepCopy;
        return deepCopy;
    }

    public IReadOnlyDictionary<Type, ISlice> GetAllSlicesCopy()
    {
        var result = new Dictionary<Type, ISlice>(_slices.Count);
        foreach (var slice in _slices)
        {
            result[slice.Key] = DeepCopySlice(slice.Value, slice.Key);
        }

        return result;
    }

    public IReadOnlyDictionary<Type, ISlice> GetAllSlicesSnapshot(SnapshotStrategy strategy)
    {
        return strategy == SnapshotStrategy.Reference
            ? new Dictionary<Type, ISlice>(_slices)
            : GetAllSlicesCopy();
    }

    private T DeepCopy<T>(T source) where T : class, ISlice
    {
        return (T)DeepCopySlice(source, typeof(T));
    }

    private ISlice DeepCopySlice(ISlice source, Type type)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        if (source is ICloneable cloneable)
        {
            var cloned = cloneable.Clone();
            if (cloned is ISlice sliceClone)
            {
                return sliceClone;
            }
        }

        var isRecord = IsRecord(type);

        if (isRecord)
        {
            var cloneMethod = type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance);
            if (cloneMethod != null)
            {
                return (ISlice)cloneMethod.Invoke(source, null)!;
            }
        }

        var json = JsonSerializer.Serialize(source, type);
        var deserialized = JsonSerializer.Deserialize(json, type);
        if (deserialized is ISlice slice)
        {
            return slice;
        }

        throw new InvalidOperationException($"Unable to deep copy slice of type {type.FullName}");
    }

    private bool IsRecord(Type type)
    {
        return _isRecordCache.GetOrAdd(type, t =>
            t.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) != null ||
            t.BaseType?.Name.Contains("Record") == true);
    }

    public static AppState InitialState()
    {
        return new AppState();
    }
}
