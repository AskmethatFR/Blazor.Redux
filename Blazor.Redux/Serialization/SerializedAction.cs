using System.Text.Json;

namespace Blazor.Redux.Serialization;

public sealed record SerializedAction(string Type, JsonElement Data, string Version = "1");
