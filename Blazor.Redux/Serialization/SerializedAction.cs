using System.Text.Json;

namespace Blazor.Redux.Serialization;

/// <summary>
/// Portable representation of a serialized Redux action.
/// </summary>
/// <param name="Type">Assembly-qualified type name of the action.</param>
/// <param name="Data">Serialized action data as a JSON element.</param>
/// <param name="Version">Serializer version for forward compatibility.</param>
public sealed record SerializedAction(string Type, JsonElement Data, string Version = "1");
