using System.Reflection;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Core;

internal record AppState
{
    private readonly IDictionary<Type, ISlice> _slices = new Dictionary<Type, ISlice>();

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

    /// <summary>
    /// Creates a deep copy of a slice instance.
    /// First tries the compiler-generated record clone method ("&lt;Clone&gt;$") —
    /// this is a C# compiler implementation detail that generates a protected
    /// member on all record types. Falls back to <see cref="ICloneable"/> if
    /// the record method is not found.
    /// Slices must be records or implement <see cref="ICloneable"/> for deep copy.
    /// </summary>
    /// <param name="source">Source slice to copy.</param>
    /// <param name="type">Runtime type of the slice.</param>
    /// <returns>Deep copy of the slice.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if neither record clone method nor <see cref="ICloneable"/> is available.
    /// </exception>
    private ISlice DeepCopySlice(ISlice source, Type type)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        // Detect the C#-compiler-generated record clone method "<Clone>$".
        // This is emitted for all record types and performs a memberwise copy
        // (the object equivalent of `with { }`). It relies on a compiler
        // implementation detail that is stable across .NET 6+ but is not
        // part of any ECMA/ISO specification.
        var cloneMethod = type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance);
        if (cloneMethod != null)
        {
            return (ISlice)cloneMethod.Invoke(source, null)!;
        }

        if (source is ICloneable cloneable)
        {
            var cloned = cloneable.Clone();
            if (cloned is ISlice sliceClone)
            {
                return sliceClone;
            }
        }

        throw new InvalidOperationException(
            $"Slice type '{type.FullName}' must be a record or implement ICloneable for deep copy.");
    }

    public static AppState InitialState()
    {
        return new AppState();
    }
}
