using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
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

        return DeepCopy<T>((first as T)!);
    }

    public AppState AddSlice(ISlice slice)
    {
        _slices.Add(slice.GetType(), slice);
        return this;
    }

    public T UpdateSlice<T>(T value) where T : class, ISlice
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (!_slices.ContainsKey(typeof(T)))
        {
            throw new InvalidOperationException($"Le slice de type {typeof(T).Name} n'existe pas dans l'état.");
        }

        var deepCopy = DeepCopy(value);
        _slices[typeof(T)] = deepCopy;
        return deepCopy;
    }

    private ConcurrentDictionary<Type, bool> _isRecordCache = new();
    private T DeepCopy<T>(T source) where T : class, ISlice
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var type = typeof(T);

        var isRecord = _isRecordCache.GetOrAdd(type, t =>
            t.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) != null ||
            t.BaseType?.Name.Contains("Record") == true);

        if (isRecord)
        {
            var cloneMethod = type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance);
            if (cloneMethod != null)
            {
                return (T)cloneMethod.Invoke(source, null)!;
            }
        }

        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(source))!;
    }


    public static AppState InitialState()
    {
        return new AppState();
    }
}