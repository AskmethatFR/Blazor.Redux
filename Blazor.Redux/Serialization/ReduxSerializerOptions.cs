using System.Text.Json;

namespace Blazor.Redux.Serialization;

public sealed record ReduxSerializerOptions
{
    public JsonSerializerOptions JsonOptions { get; init; } = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Optional mapping from serialized type name to actual type (for renamed actions/slices).
    /// </summary>
    public IDictionary<string, Type> TypeMap { get; init; } = new Dictionary<string, Type>();

    /// <summary>
    /// Optional serializer version to embed in metadata (future-proof).
    /// </summary>
    public string Version { get; init; } = "1";
}
