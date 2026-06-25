using System.Text.Json;

namespace Blazor.Redux.Serialization;

/// <summary>
/// Portable representation of a single serialized slice within a state snapshot.
/// </summary>
/// <param name="Type">Assembly-qualified type name of the slice.</param>
/// <param name="Data">Serialized slice data as a JSON element.</param>
public sealed record SerializedSlice(string Type, JsonElement Data);

/// <summary>
/// Portable representation of a serialized root state snapshot.
/// </summary>
/// <param name="Slices">List of serialized slices.</param>
/// <param name="Version">Serializer version for forward compatibility.</param>
public sealed record SerializedState(List<SerializedSlice> Slices, string Version = "1");
