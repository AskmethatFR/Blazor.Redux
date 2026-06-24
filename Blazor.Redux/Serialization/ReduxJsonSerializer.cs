using System.Reflection;
using System.Text.Json;
using Blazor.Redux.Core;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Serialization;

public sealed class ReduxJsonSerializer : IReduxSerializer
{
    private readonly JsonSerializerOptions _options;
    private readonly IDictionary<string, Type> _typeMap;
    private readonly Assembly[] _searchAssemblies;
    private readonly string _version;

    public ReduxJsonSerializer(
        JsonSerializerOptions? options = null,
        IDictionary<string, Type>? typeMap = null,
        Assembly[]? searchAssemblies = null,
        string version = "1")
    {
        _options = options ?? new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        _typeMap = typeMap ?? new Dictionary<string, Type>();
        _searchAssemblies = searchAssemblies ?? [];
        _version = version;
    }

    public SerializedAction SerializeAction(IAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var type = action.GetType();
        var data = JsonSerializer.SerializeToElement(action, type, _options);
        var typeName = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
        return new SerializedAction(typeName, data, _version);
    }

    public SerializedState SerializeState(RootStateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var slices = new List<SerializedSlice>(snapshot.Slices.Count);
        foreach (var entry in snapshot.Slices)
        {
            var type = entry.Key;
            var slice = entry.Value;
            var typeName = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
            var data = JsonSerializer.SerializeToElement(slice, type, _options);
            slices.Add(new SerializedSlice(typeName, data));
        }

        return new SerializedState(slices, _version);
    }

    public IAction DeserializeAction(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var serialized = JsonSerializer.Deserialize<SerializedAction>(json, _options);
        if (serialized is null)
        {
            throw new InvalidOperationException("Failed to deserialize action payload.");
        }

        var type = ResolveType(serialized.Type, typeof(IAction));
        var dataJson = serialized.Data.GetRawText();
        return (IAction)(JsonSerializer.Deserialize(dataJson, type, _options)
            ?? throw new InvalidOperationException("Failed to deserialize action data."));
    }

    public RootStateSnapshot DeserializeState(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var serialized = JsonSerializer.Deserialize<SerializedState>(json, _options);
        if (serialized is null)
        {
            throw new InvalidOperationException("Failed to deserialize state payload.");
        }

        var slices = new Dictionary<Type, ISlice>();
        foreach (var slice in serialized.Slices)
        {
            var type = ResolveType(slice.Type, typeof(ISlice));
            var dataJson = slice.Data.GetRawText();
            var value = (ISlice)(JsonSerializer.Deserialize(dataJson, type, _options)
                ?? throw new InvalidOperationException("Failed to deserialize slice data."));
            slices[type] = value;
        }

        return new RootStateSnapshot(slices);
    }

    private Type ResolveType(string typeName, Type requiredBase)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            throw new InvalidOperationException("Serialized type name is missing.");
        }

        if (_typeMap.TryGetValue(typeName, out var mapped))
        {
            if (!requiredBase.IsAssignableFrom(mapped))
            {
                throw new InvalidOperationException($"Type '{typeName}' does not implement {requiredBase.Name}.");
            }
            return mapped;
        }

        foreach (var assembly in _searchAssemblies)
        {
            var type = assembly.GetType(typeName);
            if (type == null)
            {
                // Try with just the type name (strip assembly suffix from AssemblyQualifiedName).
                var simpleName = typeName.Split(',')[0].Trim();
                type = assembly.GetType(simpleName);
            }
            if (type != null)
            {
                if (!requiredBase.IsAssignableFrom(type))
                {
                    throw new InvalidOperationException($"Type '{typeName}' does not implement {requiredBase.Name}.");
                }
                return type;
            }
        }

        if (_searchAssemblies.Length > 0)
        {
            var known = string.Join(", ", _searchAssemblies.Select(a => a.GetName().Name));
            throw new InvalidOperationException($"Unable to resolve type '{typeName}' from registered assemblies: {known}.");
        }

        // Fallback for backward compatibility when no assemblies are registered.
        var fallback = Type.GetType(typeName, throwOnError: false);
        if (fallback is null)
        {
            throw new InvalidOperationException($"Unable to resolve type '{typeName}'.");
        }

        if (!requiredBase.IsAssignableFrom(fallback))
        {
            throw new InvalidOperationException($"Type '{typeName}' does not implement {requiredBase.Name}.");
        }

        return fallback;
    }
}
